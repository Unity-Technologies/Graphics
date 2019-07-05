using System;
using UnityEngine;
using IDataProvider = UnityEngine.Rendering.Experimental.LookDev.IDataProvider;

namespace UnityEditor.Rendering.Experimental.LookDev
{
    enum ShadowCompositionPass
    {
        WithSun,
        WithoutSun,
        ShadowMask
    }

    enum CompositionFinal
    {
        First,
        Second
    }

    class RenderTextureCache
    {
        RenderTexture[] m_RTs = new RenderTexture[8];

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
            => (int)index * 3 + (int)(passIndex);
        int computeIndex(CompositionFinal index)
            => 6 + (int)(index);

        public static RenderTexture UpdateSize(RenderTexture renderTexture, Rect rect, bool pixelPerfect, Camera renderingCamera, string renderDocName = "LookDevRT")
        {
            int width = (int)rect.width;
            int height = (int)rect.height;
            if ((renderTexture == null
                || width != renderTexture.width
                || height != renderTexture.height)
                && !rect.IsNullOrInverted())
            {
                if (renderTexture != null)
                {
                    if (renderingCamera != null && !renderingCamera.Equals(null) && renderingCamera.targetTexture == renderTexture)
                        renderingCamera.targetTexture = null;
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                }

                // Do not use GetTemporary to manage render textures. Temporary RTs are only
                // garbage collected each N frames, and in the editor we might be wildly resizing
                // the inspector, thus using up tons of memory.
                renderTexture = new RenderTexture(
                    width, height, 0,
                    RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default);
                renderTexture.hideFlags = HideFlags.HideAndDontSave;
                renderTexture.name = renderDocName;
                renderTexture.Create();

                if (renderingCamera != null)
                    renderingCamera.targetTexture = renderTexture;
            }
            return renderTexture;
        }

        public void UpdateSize(Rect rect, ViewIndex index, bool pixelPerfect, Camera renderingCamera)
        {
            this[index, ShadowCompositionPass.WithSun] = UpdateSize(this[index, ShadowCompositionPass.WithSun], rect, pixelPerfect, renderingCamera, $"LookDevRT-{index}-Sky");
            this[index, ShadowCompositionPass.WithoutSun] = UpdateSize(this[index, ShadowCompositionPass.WithoutSun], rect, pixelPerfect, renderingCamera, $"LookDevRT-{index}-Shadow");
            this[index, ShadowCompositionPass.ShadowMask] = UpdateSize(this[index, ShadowCompositionPass.ShadowMask], rect, pixelPerfect, renderingCamera, $"LookDevRT-{index}-ShadowMask");
        }


        public void UpdateSize(Rect rect, CompositionFinal index, bool pixelPerfect, Camera renderingCamera)
            => this[index] = UpdateSize(this[index], rect, pixelPerfect, renderingCamera, $"LookDevRT-Final-{index}");
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

        IDataProvider m_DataProvider;
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
            m_DataProvider = dataProvider;
            m_Displayer = displayer;
            m_Contexts = contexts;

            m_RenderDataCache = new RenderingData[2]
            {
                new RenderingData() { stage = stages[ViewIndex.First], updater = contexts.GetViewContent(ViewIndex.First).camera },
                new RenderingData() { stage = stages[ViewIndex.Second], updater = contexts.GetViewContent(ViewIndex.Second).camera }
            };

            m_Displayer.OnRenderDocAcquisitionTriggered += RenderDocAcquisitionRequested;
            EditorApplication.update += Render;
        }

        void RenderDocAcquisitionRequested()
            => m_RenderDocAcquisitionRequested = true;

        void CleanUp()
        {
            m_Displayer.OnRenderDocAcquisitionTriggered -= RenderDocAcquisitionRequested;
            EditorApplication.update -= Render;
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

            using (new UnityEngine.Rendering.VolumeIsolationScope(true))
            {
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
                    case Layout.CustomCircular:
                        RenderCompositeAndOutput();
                        break;
                }
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
            Environment env = m_Contexts.GetViewContent(index).environment;

            m_RenderTextures.UpdateSize(renderingData.viewPort, index, m_Renderer.pixelPerfect, renderingData.stage.camera);

            renderingData.output = m_RenderTextures[index, ShadowCompositionPass.WithSun];
            m_Renderer.Acquire(renderingData, RenderingPass.First);
            if (renderingData.resized)
            {
                m_RenderTextures[index, ShadowCompositionPass.WithSun] = renderingData.output;
                renderingData.resized = false;
            }

            //get shadowmask betwen first and last pass to still be isolated
            RenderTexture tmp = m_RenderTextures[index, ShadowCompositionPass.ShadowMask];
            env?.UpdateSunPosition(renderingData.stage.sunLight);
            renderingData.stage.sunLight.intensity = 1f;
            m_DataProvider.GetShadowMask(ref tmp, renderingData.stage.runtimeInterface);
            renderingData.stage.sunLight.intensity = 0f;
            m_RenderTextures[index, ShadowCompositionPass.ShadowMask] = tmp;

            if (env != null)
                m_DataProvider.UpdateSky(renderingData.stage.camera, env.shadowSky, renderingData.stage.runtimeInterface);
            renderingData.output = m_RenderTextures[index, ShadowCompositionPass.WithoutSun];
            m_Renderer.Acquire(renderingData, RenderingPass.Last);
            if (renderingData.resized)
            {
                m_RenderTextures[index, ShadowCompositionPass.WithoutSun] = renderingData.output;
                renderingData.resized = false;
            }
            if (env != null)
                m_DataProvider.UpdateSky(renderingData.stage.camera, env.sky, renderingData.stage.runtimeInterface);

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

        void Compositing(Rect rect, int pass, CompositionFinal finalBuffer)
        {
            if (rect.IsNullOrInverted()
                || (m_Contexts.layout.viewLayout != Layout.FullSecondView
                    && (m_RenderTextures[ViewIndex.First, ShadowCompositionPass.WithSun] == null
                        || m_RenderTextures[ViewIndex.First, ShadowCompositionPass.WithoutSun] == null
                        || m_RenderTextures[ViewIndex.First, ShadowCompositionPass.ShadowMask] == null))
                || (m_Contexts.layout.viewLayout != Layout.FullFirstView
                    && (m_RenderTextures[ViewIndex.Second, ShadowCompositionPass.WithSun] == null
                        || m_RenderTextures[ViewIndex.Second, ShadowCompositionPass.WithoutSun] == null
                        || m_RenderTextures[ViewIndex.Second, ShadowCompositionPass.ShadowMask] == null)))
            {
                m_RenderTextures[finalBuffer] = null;
                return;
            }

            m_RenderTextures.UpdateSize(rect, finalBuffer, m_pixelPerfect, null);

            ComparisonGizmoState gizmo = m_Contexts.layout.gizmoState;

            Vector4 gizmoPosition = new Vector4(gizmo.center.x, gizmo.center.y, 0.0f, 0.0f);
            Vector4 gizmoZoneCenter = new Vector4(gizmo.point2.x, gizmo.point2.y, 0.0f, 0.0f);
            Vector4 gizmoThickness = new Vector4(ComparisonGizmoState.thickness, ComparisonGizmoState.thicknessSelected, 0.0f, 0.0f);
            Vector4 gizmoCircleRadius = new Vector4(ComparisonGizmoState.circleRadius, ComparisonGizmoState.circleRadiusSelected, 0.0f, 0.0f);

            Environment env0 = m_Contexts.GetViewContent(ViewIndex.First).environment;
            Environment env1 = m_Contexts.GetViewContent(ViewIndex.Second).environment;

            float exposureValue0 = env0?.sky.exposure ?? 0f;
            float exposureValue1 = env1?.sky.exposure ?? 0f;
            float dualViewBlendFactor = gizmo.blendFactor;
            float isCurrentlyLeftEditting = 1f; //1f true, -1f false
            float dragAndDropContext = 0f; //1f left, -1f right, 0f neither
            float toneMapEnabled = -1f; //1f true, -1f false
            float shadowMultiplier0 = env0?.shadowIntensity ?? 0f;
            float shadowMultiplier1 = env1?.shadowIntensity ?? 0f;
            Color shadowColor0 = env0?.shadow.color ?? Color.white;
            Color shadowColor1 = env1?.shadow.color ?? Color.white;

            //TODO: handle shadow not at compositing step but in rendering
            Texture texWithSun0 = m_RenderTextures[ViewIndex.First, ShadowCompositionPass.WithSun];
            Texture texWithoutSun0 = m_RenderTextures[ViewIndex.First, ShadowCompositionPass.WithoutSun];
            Texture texShadowsMask0 = m_RenderTextures[ViewIndex.First, ShadowCompositionPass.ShadowMask];

            Texture texWithSun1 = m_RenderTextures[ViewIndex.Second, ShadowCompositionPass.WithSun];
            Texture texWithoutSun1 = m_RenderTextures[ViewIndex.Second, ShadowCompositionPass.WithoutSun];
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
            RenderTexture.active = m_RenderTextures[finalBuffer];
            material.SetTexture("_Tex0WithSun", texWithSun0);
            material.SetTexture("_Tex0WithoutSun", texWithoutSun0);
            material.SetTexture("_Tex0Shadows", texShadowsMask0);
            material.SetColor("_ShadowColor0", shadowColor0);
            material.SetTexture("_Tex1WithSun", texWithSun1);
            material.SetTexture("_Tex1WithoutSun", texWithoutSun1);
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
