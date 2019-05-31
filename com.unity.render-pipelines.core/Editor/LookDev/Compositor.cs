//#define TEMPORARY_RENDERDOC_INTEGRATION //require specific c++

using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LookDev;
using UnityEngine.SceneManagement;
using IDataProvider = UnityEngine.Rendering.LookDev.IDataProvider;

namespace UnityEditor.Rendering.LookDev
{
    class RenderTextureCache
    {
        RenderTexture[] m_RTs = new RenderTexture[3];

        public RenderTexture this[ViewCompositionIndex index]
        {
            get => m_RTs[(int)index];
            set => m_RTs[(int)index] = value;
        }

        public static RenderTexture UpdateSize(RenderTexture renderTexture, Rect rect, bool pixelPerfect, Camera renderingCamera)
        {
            float scaleFactor = GetScaleFactor(rect.width, rect.height, pixelPerfect);
            int width = (int)(rect.width * scaleFactor);
            int height = (int)(rect.height * scaleFactor);
            if (renderTexture == null
                || width != renderTexture.width
                || height != renderTexture.height)
            {
                if (renderTexture != null)
                    UnityEngine.Object.DestroyImmediate(renderTexture);

                // Do not use GetTemporary to manage render textures. Temporary RTs are only
                // garbage collected each N frames, and in the editor we might be wildly resizing
                // the inspector, thus using up tons of memory.
                //GraphicsFormat format = camera.allowHDR ? GraphicsFormat.R16G16B16A16_SFloat : GraphicsFormat.R8G8B8A8_UNorm;
                //m_RenderTexture = new RenderTexture(rtWidth, rtHeight, 16, format);
                //m_RenderTexture.hideFlags = HideFlags.HideAndDontSave;
                //TODO: check format
                renderTexture = new RenderTexture(
                    width, height, 0,
                    RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default);
                renderTexture.hideFlags = HideFlags.HideAndDontSave;
                renderTexture.name = "LookDevTexture";
                renderTexture.Create();

                if (renderingCamera != null)
                    renderingCamera.targetTexture = renderTexture;
            }
            return renderTexture;
        }

        public void UpdateSize(Rect rect, ViewCompositionIndex index, bool pixelPerfect, Camera renderingCamera)
            => this[index] = UpdateSize(this[index], rect, pixelPerfect, renderingCamera);

        static float GetScaleFactor(float width, float height, bool pixelPerfect)
        {
            float scaleFacX = Mathf.Max(Mathf.Min(width * 2, 1024), width) / width;
            float scaleFacY = Mathf.Max(Mathf.Min(height * 2, 1024), height) / height;
            float result = Mathf.Min(scaleFacX, scaleFacY) * EditorGUIUtility.pixelsPerPoint;
            if (pixelPerfect)
                result = Mathf.Max(Mathf.Round(result), 1f);
            return result;
        }
    }

    class Compositer : IDisposable
    {
        public static readonly Color firstViewGizmoColor = new Color32(0, 154, 154, 255);
        public static readonly Color secondViewGizmoColor = new Color32(255, 37, 4, 255);
        static Material s_MaterialCompositer = new Material(Shader.Find("Hidden/LookDev/Compositor"));

        IViewDisplayer m_Displayer;
        Context m_Contexts;
        RenderTextureCache m_RenderTextures = new RenderTextureCache();
        StageCache m_Stages;

        Renderer m_Renderer = new Renderer();
        RenderingData[] m_RenderDataCache;

        bool m_pixelPerfect;

        public bool pixelPerfect
        {
            get => m_pixelPerfect;
            set => m_Renderer.pixelPerfect = m_pixelPerfect = value;
        }

        Color m_AmbientColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);

#if TEMPORARY_RENDERDOC_INTEGRATION
        bool m_RenderDocAcquisitionRequested;
#endif

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
                new RenderingData() { stage = stages[ViewIndex.First] },
                new RenderingData() { stage = stages[ViewIndex.Second] }
            };

            m_Displayer.OnRenderDocAcquisitionTriggered += RenderDocAcquisitionRequested;
            EditorApplication.update += Render;
        }

        void RenderDocAcquisitionRequested()
#if TEMPORARY_RENDERDOC_INTEGRATION
            => m_RenderDocAcquisitionRequested = true;
#else
        {
            UnityEngine.Debug.LogWarning("RenderDoc integration flag not set. Full RenderDoc is not possible.");
        }
#endif

        void CleanUp()
        {
            m_Displayer.OnRenderDocAcquisitionTriggered -= RenderDocAcquisitionRequested;
            EditorApplication.update -= Render;
        }
        public void Dispose()
        {
            CleanUp();
            GC.SuppressFinalize(this);
        }
        ~Compositer() => CleanUp();

        public void Render()
        {
#if TEMPORARY_RENDERDOC_INTEGRATION
            //TODO: make integration EditorWindow agnostic!
            if (RenderDoc.IsLoaded() && RenderDoc.IsSupported() && m_RenderDocAcquisitionRequested)
                RenderDoc.BeginCaptureRenderDoc(m_Displayer as EditorWindow);
#endif

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

#if TEMPORARY_RENDERDOC_INTEGRATION
            //TODO: make integration EditorWindow agnostic!
            if (RenderDoc.IsLoaded() && RenderDoc.IsSupported() && m_RenderDocAcquisitionRequested)
                RenderDoc.EndCaptureRenderDoc(m_Displayer as EditorWindow);

            //stating that RenderDoc do not need to acquire anymore should
            //allows to gather both view and composition in render doc at once
            //TODO: check this
            m_RenderDocAcquisitionRequested = false;
#endif
        }

        void RenderSingleAndOutput(ViewIndex index)
        {
            var renderingData = m_RenderDataCache[(int)index];
            renderingData.viewPort = m_Displayer.GetRect((ViewCompositionIndex)index);
            m_Renderer.Acquire(renderingData);
            if (renderingData.resized)
            {
                m_RenderTextures[(ViewCompositionIndex)index] = renderingData.output;
                renderingData.resized = false;
            }
            m_Displayer.SetTexture((ViewCompositionIndex)index, renderingData.output);
        }

        void RenderCompositeAndOutput()
        {
            Rect rect = m_Displayer.GetRect(ViewCompositionIndex.Composite);

            var renderingData = m_RenderDataCache[0];
            renderingData.viewPort = rect;
            m_Renderer.Acquire(renderingData);
            if (renderingData.resized)
            {
                m_RenderTextures[ViewCompositionIndex.First] = renderingData.output;
                renderingData.resized = false;
            }
            renderingData = m_RenderDataCache[1];
            renderingData.viewPort = rect;
            if (renderingData.resized)
            {
                m_RenderTextures[ViewCompositionIndex.Second] = renderingData.output;
                renderingData.resized = false;
            }
            m_Renderer.Acquire(renderingData);

            Compositing(rect);
            m_Displayer.SetTexture(ViewCompositionIndex.Composite, m_RenderTextures[ViewCompositionIndex.Composite]);
        }

        void Compositing(Rect rect)
        {
            if (rect.IsNullOrInverted()
                || m_RenderTextures[ViewCompositionIndex.First] == null
                || m_RenderTextures[ViewCompositionIndex.Second] == null)
            {
                m_RenderTextures[ViewCompositionIndex.Composite] = null;
                return;
            }

            m_RenderTextures.UpdateSize(rect, ViewCompositionIndex.Composite, m_pixelPerfect, null);

            ComparisonGizmoState gizmo = m_Contexts.layout.gizmoState;

            Vector4 gizmoPosition = new Vector4(gizmo.center.x, gizmo.center.y, 0.0f, 0.0f);
            Vector4 gizmoZoneCenter = new Vector4(gizmo.point2.x, gizmo.point2.y, 0.0f, 0.0f);
            Vector4 gizmoThickness = new Vector4(ComparisonGizmoState.thickness, ComparisonGizmoState.thicknessSelected, 0.0f, 0.0f);
            Vector4 gizmoCircleRadius = new Vector4(ComparisonGizmoState.circleRadius, ComparisonGizmoState.circleRadiusSelected, 0.0f, 0.0f);

            float exposureValue0 = 0.0f;
            float exposureValue1 = 0.0f;
            float dualViewBlendFactor = gizmo.blendFactor;
            float isCurrentlyLeftEditting = 1.0f; //1f true, -1f false
            float dragAndDropContext = 0f; //1f left, -1f right, 0f neither
            float toneMapEnabled = -1f; //1f true, -1f false
            float shadowMultiplier0 = 0f;
            float shadowMultiplier1 = 0f;
            Color shadowColor0 = Color.white;
            Color shadowColor1 = Color.white;

            //TODO: handle shadow not at compositing step but in rendering
            Texture texWithSun0 = m_RenderTextures[ViewCompositionIndex.First];
            Texture texWithoutSun0 = texWithSun0;
            Texture texShadowsMask0 = Texture2D.whiteTexture;

            Texture texWithSun1 = m_RenderTextures[ViewCompositionIndex.Second];
            Texture texWithoutSun1 = texWithSun0;
            Texture texShadowsMask1 = Texture2D.whiteTexture;

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
            RenderTexture.active = m_RenderTextures[ViewCompositionIndex.Composite];
            s_MaterialCompositer.SetTexture("_Tex0Normal", texWithSun0);
            s_MaterialCompositer.SetTexture("_Tex0WithoutSun", texWithoutSun0);
            s_MaterialCompositer.SetTexture("_Tex0Shadows", texShadowsMask0);
            s_MaterialCompositer.SetColor("_ShadowColor0", shadowColor0);
            s_MaterialCompositer.SetTexture("_Tex1Normal", texWithSun1);
            s_MaterialCompositer.SetTexture("_Tex1WithoutSun", texWithoutSun1);
            s_MaterialCompositer.SetTexture("_Tex1Shadows", texShadowsMask1);
            s_MaterialCompositer.SetColor("_ShadowColor1", shadowColor1);
            s_MaterialCompositer.SetVector("_CompositingParams", compositingParams);
            s_MaterialCompositer.SetVector("_CompositingParams2", compositingParams2);
            s_MaterialCompositer.SetColor("_FirstViewColor", firstViewGizmoColor);
            s_MaterialCompositer.SetColor("_SecondViewColor", secondViewGizmoColor);
            s_MaterialCompositer.SetVector("_GizmoPosition", gizmoPosition);
            s_MaterialCompositer.SetVector("_GizmoZoneCenter", gizmoZoneCenter);
            s_MaterialCompositer.SetVector("_GizmoSplitPlane", gizmo.plane);
            s_MaterialCompositer.SetVector("_GizmoSplitPlaneOrtho", gizmo.planeOrtho);
            s_MaterialCompositer.SetFloat("_GizmoLength", gizmo.length);
            s_MaterialCompositer.SetVector("_GizmoThickness", gizmoThickness);
            s_MaterialCompositer.SetVector("_GizmoCircleRadius", gizmoCircleRadius);
            s_MaterialCompositer.SetFloat("_BlendFactorCircleRadius", ComparisonGizmoState.blendFactorCircleRadius);
            s_MaterialCompositer.SetFloat("_GetBlendFactorMaxGizmoDistance", gizmo.blendFactorMaxGizmoDistance);
            s_MaterialCompositer.SetFloat("_GizmoRenderMode", k_GizmoRenderMode);
            s_MaterialCompositer.SetVector("_ScreenRatio", screenRatio);
            s_MaterialCompositer.SetVector("_ToneMapCoeffs1", tonemapCoeff1);
            s_MaterialCompositer.SetVector("_ToneMapCoeffs2", tonemapCoeff2);
            s_MaterialCompositer.SetPass((int)m_Contexts.layout.viewLayout); //missing horizontal pass

            Renderer.DrawFullScreenQuad(new Rect(0, 0, rect.width, rect.height));

            RenderTexture.active = oldActive;
            //GUI.DrawTexture(rect, m_RenderTextures[ViewCompositionIndex.Composite], ScaleMode.StretchToFill, false);
        }


        public ViewIndex GetViewFromComposition(Vector2 localCoordinate)
        {
            Rect compositionRect = m_Displayer.GetRect(ViewCompositionIndex.Composite);
            Vector2 normalizedLocalCoordinate = ComparisonGizmo.GetNormalizedCoordinates(localCoordinate, compositionRect);
            switch (m_Contexts.layout.viewLayout)
            {
                case Layout.CustomSplit:
                    return Vector3.Dot(new Vector3(normalizedLocalCoordinate.x, normalizedLocalCoordinate.y, 1), m_Contexts.layout.gizmoState.plane) >= 0
                        ? ViewIndex.First
                        : ViewIndex.Second;
                case Layout.CustomCircular:
                    //[TODO]
                    return default;
                default:
                    throw new Exception("GetViewFromComposition call when not inside a Composition");
            }
        }
    }
}
