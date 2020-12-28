using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using IDataProvider = UnityEngine.Rendering.LookDev.IDataProvider;

namespace UnityEditor.Rendering.LookDev
{
    enum ShadowCompositionPass
    {
        MainView,
        ShadowMask
    }

    enum CompositionFinal
    {
        First,
        Second
    }

    class RenderTextureCache : IDisposable
    {
        const int k_PassPerViewCount = 3;
        const int k_ViewCount = 2;
        const int k_TextureCacheSize = k_PassPerViewCount * k_ViewCount;
        //RenderTextures are packed this way:
        //0: ViewIndex.First, ShadowCompositionPass.MainView
        //1: ViewIndex.First, ShadowCompositionPass.ShadowMask
        //2: CompositionFinal.First
        //3: ViewIndex.Second, ShadowCompositionPass.MainView
        //4: ViewIndex.Second, ShadowCompositionPass.ShadowMask
        //5: CompositionFinal.Second
        RenderTexture[] m_RTs = new RenderTexture[k_TextureCacheSize];

        public RenderTexture this[ViewIndex index, ShadowCompositionPass passIndex]
        {
            get => m_RTs[computeIndex(index, passIndex)];
            set => m_RTs[computeIndex(index, passIndex)] = value;
        }

        public RenderTexture this[CompositionFinal index]
        {
            get => m_RTs[computeIndex(index)];
            set => m_RTs[computeIndex(index)] = value;
        }

        int computeIndex(ViewIndex index, ShadowCompositionPass passIndex)
            => (int)index * k_PassPerViewCount + (int)(passIndex);
        int computeIndex(CompositionFinal index)
            => (k_PassPerViewCount-1) + (int)(index) * k_PassPerViewCount;

        void UpdateSize(int index, Rect rect, bool pixelPerfect, Camera renderingCamera, string renderDocName = "LookDevRT")
        {
            bool nullRect = rect.IsNullOrInverted();

            GraphicsFormat format = SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.Render)
                ? GraphicsFormat.R16G16B16A16_SFloat
                : SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
            if (m_RTs[index] != null && (nullRect || m_RTs[index].graphicsFormat != format))
            {
                m_RTs[index].Release();
                UnityEngine.Object.DestroyImmediate(m_RTs[index]);
                m_RTs[index] = null;
            }
            if (nullRect)
                return;

            int width = (int)rect.width;
            int height = (int)rect.height;

            if (m_RTs[index] == null)
            {
                m_RTs[index] = new RenderTexture(0, 0, 24, format);
                m_RTs[index].name = renderDocName;
                m_RTs[index].antiAliasing = 1;
                m_RTs[index].hideFlags = HideFlags.HideAndDontSave;
            }

            if (m_RTs[index].width != width || m_RTs[index].height != height)
            {
                m_RTs[index].Release();
                m_RTs[index].width = width;
                m_RTs[index].height = height;
                m_RTs[index].Create();
            }

            if (renderingCamera != null)
                renderingCamera.targetTexture = m_RTs[index];
        }

        public void UpdateSize(Rect rect, ViewIndex index, bool pixelPerfect, Camera renderingCamera)
        {
            UpdateSize(computeIndex(index, ShadowCompositionPass.MainView), rect, pixelPerfect, renderingCamera, $"LookDevRT-{index}-MainView");
            UpdateSize(computeIndex(index, ShadowCompositionPass.ShadowMask), rect, pixelPerfect, renderingCamera, $"LookDevRT-{index}-ShadowMask");
        }


        public void UpdateSize(Rect rect, CompositionFinal index, bool pixelPerfect, Camera renderingCamera)
            => UpdateSize(computeIndex(index), rect, pixelPerfect, renderingCamera, $"LookDevRT-Final-{index}");

        bool m_Disposed = false;
        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;

            for (int index = 0; index < k_TextureCacheSize; ++index)
            {
                if (m_RTs[index] == null || m_RTs[index].Equals(null))
                    continue;

                UnityEngine.Object.DestroyImmediate(m_RTs[index]);
                m_RTs[index] = null;
            }
        }
    }

    class Compositer : IDisposable
    {
        public static readonly Color firstViewGizmoColor = new Color32(0, 154, 154, 255);
        public static readonly Color secondViewGizmoColor = new Color32(255, 37, 4, 255);
        static Material s_Material;
        static Material material
        {
            get
            {
                if (s_Material == null || s_Material.Equals(null))
                    s_Material = new Material(Shader.Find("Hidden/LookDev/Compositor"));
                return s_Material;
            }
        }

        IViewDisplayer m_Displayer;
        Context m_Contexts;
        RenderTextureCache m_RenderTextures = new RenderTextureCache();
        Renderer m_Renderer = new Renderer();
        RenderingData[] m_RenderDataCache;

        bool m_pixelPerfect;
        bool m_Disposed;

        public bool pixelPerfect
        {
            get => m_pixelPerfect;
            set => m_Renderer.pixelPerfect = m_pixelPerfect = value;
        }

        Color m_AmbientColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);

        bool m_RenderDocAcquisitionRequested;

        public Compositer(
            IViewDisplayer displayer,
            Context contexts,
            IDataProvider dataProvider,
            StageCache stages)
        {
            m_Displayer = displayer;
            m_Contexts = contexts;

            m_RenderDataCache = new RenderingData[2]
            {
                new RenderingData() { stage = stages[ViewIndex.First], updater = contexts.GetViewContent(ViewIndex.First).camera },
                new RenderingData() { stage = stages[ViewIndex.Second], updater = contexts.GetViewContent(ViewIndex.Second).camera }
            };

            m_Displayer.OnRenderDocAcquisitionTriggered += RenderDocAcquisitionRequested;
            m_Displayer.OnUpdateRequested += Render;
        }

        void RenderDocAcquisitionRequested()
            => m_RenderDocAcquisitionRequested = true;

        void CleanUp()
        {
            for (int index = 0; index < 2; ++index)
            {
                m_RenderDataCache[index]?.Dispose();
                m_RenderDataCache[index] = null;
            }

            m_RenderTextures.Dispose();

            m_Displayer.OnRenderDocAcquisitionTriggered -= RenderDocAcquisitionRequested;
            m_Displayer.OnUpdateRequested -= Render;
        }
        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            CleanUp();
            GC.SuppressFinalize(this);
        }
        ~Compositer() => CleanUp();

        public void Render()
        {
            //TODO: make integration EditorWindow agnostic!
            if (UnityEditorInternal.RenderDoc.IsLoaded() && UnityEditorInternal.RenderDoc.IsSupported() && m_RenderDocAcquisitionRequested)
                UnityEditorInternal.RenderDoc.BeginCaptureRenderDoc(m_Displayer as EditorWindow);

            switch (m_Contexts.layout.viewLayout)
            {
                case Layout.FullFirstView:
                    RenderSingleAndOutput(ViewIndex.First);
                    break;
                case Layout.FullSecondView:
                    RenderSingleAndOutput(ViewIndex.Second);
                    break;
                case Layout.HorizontalSplit:
                case Layout.VerticalSplit:
                    RenderSingleAndOutput(ViewIndex.First);
                    RenderSingleAndOutput(ViewIndex.Second);
                    break;
                case Layout.CustomSplit:
                    RenderCompositeAndOutput();
                    break;
            }

            //TODO: make integration EditorWindow agnostic!
            if (UnityEditorInternal.RenderDoc.IsLoaded() && UnityEditorInternal.RenderDoc.IsSupported() && m_RenderDocAcquisitionRequested)
                UnityEditorInternal.RenderDoc.EndCaptureRenderDoc(m_Displayer as EditorWindow);

            //stating that RenderDoc do not need to acquire anymore should
            //allows to gather both view and composition in render doc at once
            m_RenderDocAcquisitionRequested = false;
        }

        void AcquireDataForView(ViewIndex index, Rect viewport)
        {
            var renderingData = m_RenderDataCache[(int)index];
            renderingData.viewPort = viewport;
            ViewContext view = m_Contexts.GetViewContent(index);

            m_RenderTextures.UpdateSize(renderingData.viewPort, index, m_Renderer.pixelPerfect, renderingData.stage.camera);

            int debugMode = m_Contexts.GetViewContent(index).debug.viewMode;
            if (debugMode != -1)
                LookDev.dataProvider.UpdateDebugMode(debugMode);

            renderingData.output = m_RenderTextures[index, ShadowCompositionPass.MainView];
            m_Renderer.BeginRendering(renderingData, LookDev.dataProvider);
            m_Renderer.Acquire(renderingData);

            if (view.debug.shadow)
            {
                RenderTexture tmp = m_RenderTextures[index, ShadowCompositionPass.ShadowMask];
                view.environment?.UpdateSunPosition(renderingData.stage.sunLight);
                renderingData.stage.sunLight.intensity = 1f;
                LookDev.dataProvider.GetShadowMask(ref tmp, renderingData.stage.runtimeInterface);
                renderingData.stage.sunLight.intensity = 0f;
                m_RenderTextures[index, ShadowCompositionPass.ShadowMask] = tmp;
            }

            m_Renderer.EndRendering(renderingData, LookDev.dataProvider);

            if (debugMode != -1)
                LookDev.dataProvider.UpdateDebugMode(-1);
        }

        void RenderSingleAndOutput(ViewIndex index)
        {
            Rect viewport = m_Displayer.GetRect((ViewCompositionIndex)index);
            AcquireDataForView(index, viewport);
            Compositing(viewport, (int)index, (CompositionFinal)index);
            m_Displayer.SetTexture((ViewCompositionIndex)index, m_RenderTextures[(CompositionFinal)index]);
        }

        void RenderCompositeAndOutput()
        {
            Rect viewport = m_Displayer.GetRect(ViewCompositionIndex.Composite);
            AcquireDataForView(ViewIndex.First, viewport);
            AcquireDataForView(ViewIndex.Second, viewport);
            Compositing(viewport, 2 /*split*/, CompositionFinal.First);
            m_Displayer.SetTexture(ViewCompositionIndex.Composite, m_RenderTextures[CompositionFinal.First]);
        }

        void Compositing(Rect rect, int pass, CompositionFinal finalBufferIndex)
        {
            bool skipShadowComposition0 = !m_Contexts.GetViewContent(ViewIndex.First).debug.shadow;
            bool skipShadowComposition1 = !m_Contexts.GetViewContent(ViewIndex.Second).debug.shadow;

            if (rect.IsNullOrInverted()
                || (m_Contexts.layout.viewLayout != Layout.FullSecondView
                    && (m_RenderTextures[ViewIndex.First, ShadowCompositionPass.MainView] == null
                        || (!skipShadowComposition0
                            && m_RenderTextures[ViewIndex.First, ShadowCompositionPass.ShadowMask] == null)))
                || (m_Contexts.layout.viewLayout != Layout.FullFirstView
                    && (m_RenderTextures[ViewIndex.Second, ShadowCompositionPass.MainView] == null
                        || (!skipShadowComposition1
                            && m_RenderTextures[ViewIndex.Second, ShadowCompositionPass.ShadowMask] == null))))
            {
                m_RenderTextures[finalBufferIndex] = null;
                return;
            }

            m_RenderTextures.UpdateSize(rect, finalBufferIndex, m_pixelPerfect, null);

            ComparisonGizmoState gizmo = m_Contexts.layout.gizmoState;

            Vector4 gizmoPosition = new Vector4(gizmo.center.x, gizmo.center.y, 0.0f, 0.0f);
            Vector4 gizmoZoneCenter = new Vector4(gizmo.point2.x, gizmo.point2.y, 0.0f, 0.0f);
            Vector4 gizmoThickness = new Vector4(ComparisonGizmoState.thickness, ComparisonGizmoState.thicknessSelected, 0.0f, 0.0f);
            Vector4 gizmoCircleRadius = new Vector4(ComparisonGizmoState.circleRadius, ComparisonGizmoState.circleRadiusSelected, 0.0f, 0.0f);

            Environment env0 = m_Contexts.GetViewContent(ViewIndex.First).environment;
            Environment env1 = m_Contexts.GetViewContent(ViewIndex.Second).environment;

            float exposureValue0 = env0?.exposure ?? 0f;
            float exposureValue1 = env1?.exposure ?? 0f;
            float dualViewBlendFactor = gizmo.blendFactor;
            float isCurrentlyLeftEditting = m_Contexts.layout.lastFocusedView == ViewIndex.First ? 1f : -1f;
            float dragAndDropContext = 0f; //1f left, -1f right, 0f neither
            float toneMapEnabled = -1f; //1f true, -1f false
            float shadowMultiplier0 = skipShadowComposition0 ? -1f : 1.0f;
            float shadowMultiplier1 = skipShadowComposition1 ? -1f : 1.0f;
            Color shadowColor0 = env0?.shadowColor ?? Color.white;
            Color shadowColor1 = env1?.shadowColor ?? Color.white;

            //TODO: handle shadow not at compositing step but in rendering
            Texture texMainView0 = m_RenderTextures[ViewIndex.First, ShadowCompositionPass.MainView];
            Texture texShadowsMask0 = m_RenderTextures[ViewIndex.First, ShadowCompositionPass.ShadowMask];

            Texture texMainView1 = m_RenderTextures[ViewIndex.Second, ShadowCompositionPass.MainView];
            Texture texShadowsMask1 = m_RenderTextures[ViewIndex.Second, ShadowCompositionPass.ShadowMask];

            Vector4 compositingParams = new Vector4(dualViewBlendFactor, exposureValue0, exposureValue1, isCurrentlyLeftEditting);
            Vector4 compositingParams2 = new Vector4(dragAndDropContext, toneMapEnabled, shadowMultiplier0, shadowMultiplier1);

            // Those could be tweakable for the neutral tonemapper, but in the case of the LookDev we don't need that
            const float k_BlackIn = 0.02f;
            const float k_WhiteIn = 10.0f;
            const float k_BlackOut = 0.0f;
            const float k_WhiteOut = 10.0f;
            const float k_WhiteLevel = 5.3f;
            const float k_WhiteClip = 10.0f;
            const float k_DialUnits = 20.0f;
            const float k_HalfDialUnits = k_DialUnits * 0.5f;
            const float k_GizmoRenderMode = 4f; //display all

            // converting from artist dial units to easy shader-lerps (0-1)
            //TODO: to compute one time only
            Vector4 tonemapCoeff1 = new Vector4((k_BlackIn * k_DialUnits) + 1.0f, (k_BlackOut * k_HalfDialUnits) + 1.0f, (k_WhiteIn / k_DialUnits), (1.0f - (k_WhiteOut / k_DialUnits)));
            Vector4 tonemapCoeff2 = new Vector4(0.0f, 0.0f, k_WhiteLevel, k_WhiteClip / k_HalfDialUnits);

            const float k_ReferenceScale = 1080.0f;
            Vector4 screenRatio = new Vector4(rect.width / k_ReferenceScale, rect.height / k_ReferenceScale, rect.width, rect.height);

            RenderTexture oldActive = RenderTexture.active;
            RenderTexture.active = m_RenderTextures[finalBufferIndex];
            material.SetTexture("_Tex0MainView", texMainView0);
            material.SetTexture("_Tex0Shadows", texShadowsMask0);
            material.SetColor("_ShadowColor0", shadowColor0);
            material.SetTexture("_Tex1MainView", texMainView1);
            material.SetTexture("_Tex1Shadows", texShadowsMask1);
            material.SetColor("_ShadowColor1", shadowColor1);
            material.SetVector("_CompositingParams", compositingParams);
            material.SetVector("_CompositingParams2", compositingParams2);
            material.SetColor("_FirstViewColor", firstViewGizmoColor);
            material.SetColor("_SecondViewColor", secondViewGizmoColor);
            material.SetVector("_GizmoPosition", gizmoPosition);
            material.SetVector("_GizmoZoneCenter", gizmoZoneCenter);
            material.SetVector("_GizmoSplitPlane", gizmo.plane);
            material.SetVector("_GizmoSplitPlaneOrtho", gizmo.planeOrtho);
            material.SetFloat("_GizmoLength", gizmo.length);
            material.SetVector("_GizmoThickness", gizmoThickness);
            material.SetVector("_GizmoCircleRadius", gizmoCircleRadius);
            material.SetFloat("_BlendFactorCircleRadius", ComparisonGizmoState.blendFactorCircleRadius);
            material.SetFloat("_GetBlendFactorMaxGizmoDistance", gizmo.blendFactorMaxGizmoDistance);
            material.SetFloat("_GizmoRenderMode", k_GizmoRenderMode);
            material.SetVector("_ScreenRatio", screenRatio);
            material.SetVector("_ToneMapCoeffs1", tonemapCoeff1);
            material.SetVector("_ToneMapCoeffs2", tonemapCoeff2);
            material.SetPass(pass);

            Renderer.DrawFullScreenQuad(new Rect(0, 0, rect.width, rect.height));

            RenderTexture.active = oldActive;
        }

        public ViewIndex GetViewFromComposition(Vector2 localCoordinate)
        {
            Rect compositionRect = m_Displayer.GetRect(ViewCompositionIndex.Composite);
            Vector2 normalizedLocalCoordinate = ComparisonGizmoController.GetNormalizedCoordinates(localCoordinate, compositionRect);
            switch (m_Contexts.layout.viewLayout)
            {
                case Layout.CustomSplit:
                    return Vector3.Dot(new Vector3(normalizedLocalCoordinate.x, normalizedLocalCoordinate.y, 1), m_Contexts.layout.gizmoState.plane) >= 0
                        ? ViewIndex.First
                        : ViewIndex.Second;
                default:
                    throw new Exception("GetViewFromComposition call when not inside a Composition");
            }
        }
    }
}
