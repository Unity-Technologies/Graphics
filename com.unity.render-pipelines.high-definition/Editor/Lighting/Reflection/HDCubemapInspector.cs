using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditorForRenderPipeline(typeof(Cubemap), typeof(HDRenderPipelineAsset))]
    class HDCubemapInspector : Editor
    {
        private enum NavMode
        {
            None = 0,
            Zooming = 1,
            Rotating = 2
        }

        static GUIContent s_MipMapLow, s_MipMapHigh, s_ExposureLow;
        static GUIStyle s_PreLabel;
        static Mesh s_SphereMesh;

        static Mesh sphereMesh
        {
            get { return s_SphereMesh ?? (s_SphereMesh = Resources.GetBuiltinResource(typeof(Mesh), "New-Sphere.fbx") as Mesh); }
        }

        Material m_ReflectiveMaterial = null;
        PreviewRenderUtility m_PreviewUtility;
        float m_CameraPhi = 0.75f;
        float m_CameraTheta = 0.5f;
        float m_CameraDistance = 2.0f;
        Vector2 m_PreviousMousePosition = Vector2.zero;

        Texture targetTexture => target as Texture;

        public float previewExposure = 0f;
        public float mipLevelPreview = 0f;

        void InitMaterialIfNeeded()
        {
            if(m_ReflectiveMaterial == null)
            {
                var shader = Shader.Find("Debug/ReflectionProbePreview");
                if(shader != null)
                {
                    m_ReflectiveMaterial = new Material(Shader.Find("Debug/ReflectionProbePreview"))
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                }
            }
        }

        void OnEnable()
        {
            if (m_PreviewUtility == null)
                InitPreview();
        }

        void OnDisable()
        {
            if (m_PreviewUtility != null)
                m_PreviewUtility.Cleanup();
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (m_ReflectiveMaterial != null)
            {
                m_ReflectiveMaterial.SetFloat("_Exposure", previewExposure);
                m_ReflectiveMaterial.SetFloat("_MipLevel", mipLevelPreview);
            }

            if (m_PreviewUtility == null)
                InitPreview();

            // We init material just before using it as the inspector might have been enabled/awaked before during import.
            InitMaterialIfNeeded();

            UpdateCamera();

            m_ReflectiveMaterial.SetTexture("_Cubemap", target as Texture);

            m_PreviewUtility.BeginPreview(r, GUIStyle.none);
            m_PreviewUtility.DrawMesh(sphereMesh, Matrix4x4.identity, m_ReflectiveMaterial, 0);
            m_PreviewUtility.camera.Render();
            m_PreviewUtility.EndAndDrawPreview(r);

            if (Event.current.type != EventType.Repaint)
            {
                if (HandleMouse(r))
                    Repaint();
            }
        }

        public override void OnPreviewSettings()
        {
            if (s_MipMapLow == null)
                InitIcons();

            var mipmapCount = 0;
            var rt = target as RenderTexture;
            if (targetTexture != null)
                mipmapCount = targetTexture.mipmapCount;
            if (rt != null)
                mipmapCount = rt.useMipMap
                    ? (int)(Mathf.Log(Mathf.Max(rt.width, rt.height)) / Mathf.Log(2))
                    : 1;

            // If the cubemap texture does not have any mipmaps, then we hide the knob
            if (mipmapCount == 1)
                mipmapCount = 0;

            GUI.enabled = true;

            GUILayout.Box(s_ExposureLow, s_PreLabel, GUILayout.MaxWidth(20));
            previewExposure = GUILayout.HorizontalSlider(previewExposure, -20f, 20f, GUILayout.MaxWidth(80));
            GUILayout.Space(5);
            GUILayout.Box(s_MipMapHigh, s_PreLabel, GUILayout.MaxWidth(20));
            mipLevelPreview = GUILayout.HorizontalSlider(mipLevelPreview, 0, mipmapCount, GUILayout.MaxWidth(80));
            GUILayout.Box(s_MipMapLow, s_PreLabel, GUILayout.MaxWidth(20));
        }

        public override string GetInfoString() => $"{targetTexture.width}x{targetTexture.height} {GraphicsFormatUtility.GetFormatString(targetTexture.graphicsFormat)}";

        void InitPreview()
        {
            if (m_PreviewUtility != null)
                m_PreviewUtility.Cleanup();
            m_PreviewUtility = new PreviewRenderUtility(false, true);
            m_PreviewUtility.cameraFieldOfView = 50.0f;
            m_PreviewUtility.camera.nearClipPlane = 0.01f;
            m_PreviewUtility.camera.farClipPlane = 20.0f;
            m_PreviewUtility.camera.transform.position = new Vector3(0, 0, 2);
            m_PreviewUtility.camera.transform.LookAt(Vector3.zero);
        }

        static int sliderHash = "Slider".GetHashCode();

        bool HandleMouse(Rect Viewport)
        {
            bool needRepaint = false;
            int id = GUIUtility.GetControlID(sliderHash, FocusType.Passive);
            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (Viewport.Contains(evt.mousePosition) && Viewport.width > 50)
                    {
                        if (evt.button == 0)
                        {
                            GUIUtility.hotControl = id;
                            EditorGUIUtility.SetWantsMouseJumping(1);
                        }
                        evt.Use();
                    }
                    break;
                case EventType.ScrollWheel:
                    if (Viewport.Contains(evt.mousePosition) && Viewport.width > 50)
                    {
                        m_CameraDistance = Mathf.Clamp(evt.delta.y * 0.01f + m_CameraDistance, 1, 10);
                        needRepaint = true;
                        evt.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        m_CameraTheta = (m_CameraTheta - evt.delta.x * 0.003f) % (Mathf.PI * 2);
                        m_CameraPhi = Mathf.Clamp(m_CameraPhi - evt.delta.y * 0.003f, 0.2f, Mathf.PI - 0.2f);
                        evt.Use();
                        GUI.changed = true;
                        needRepaint = true;
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                        GUIUtility.hotControl = 0;
                    EditorGUIUtility.SetWantsMouseJumping(0);
                    break;
            }

            return needRepaint;
        }

        void UpdateCamera()
        {
            var pos = new Vector3(Mathf.Sin(m_CameraPhi) * Mathf.Cos(m_CameraTheta), Mathf.Cos(m_CameraPhi), Mathf.Sin(m_CameraPhi) * Mathf.Sin(m_CameraTheta)) * m_CameraDistance;
            m_PreviewUtility.camera.transform.position = pos;
            m_PreviewUtility.camera.transform.LookAt(Vector3.zero);
        }

        static void InitIcons()
        {
            s_MipMapLow = EditorGUIUtility.IconContent("PreTextureMipMapLow");
            s_MipMapHigh = EditorGUIUtility.IconContent("PreTextureMipMapHigh");
            s_ExposureLow = EditorGUIUtility.IconContent("SceneViewLighting");
            s_PreLabel = "preLabel";
        }
        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            m_CameraDistance = 1.25f;
            m_CameraPhi = Mathf.PI * 0.33f;
            m_CameraTheta = Mathf.PI;

            InitPreview();

            UpdateCamera();

            // Force loading the needed preview shader
            var previewShader = EditorGUIUtility.LoadRequired("Previews/PreviewCubemap.shader") as Shader;
            var previewMaterial = new Material(previewShader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

            // We need to force it to go through legacy
            bool assetUsedFromQuality = false;
            var currentPipelineAsset = HDUtils.SwitchToBuiltinRenderPipeline(out assetUsedFromQuality);

            previewMaterial.SetVector("_CameraWorldPosition", m_PreviewUtility.camera.transform.position);
            previewMaterial.SetFloat("_Mip", 0.0f);
            previewMaterial.SetFloat("_Alpha", 0.0f);
            previewMaterial.SetFloat("_Intensity", 1.0f);
            previewMaterial.mainTexture = (target as Texture);

            m_PreviewUtility.ambientColor = Color.black;
            m_PreviewUtility.BeginStaticPreview(new Rect(0, 0, width, height));
            m_PreviewUtility.DrawMesh(sphereMesh, Matrix4x4.identity, previewMaterial, 0);
            // TODO: For now the following line is D3D11 + Metal only as it cause out of memory on both DX12 and Vulkan API.
            // We will need to invest time to understand what is happening
            // For now priority is to enable Yamato platform automation
            // This mean that cubemap icon will render incorrectly on anything but D3D11
            if(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
                m_PreviewUtility.camera.Render();

            var outTexture = m_PreviewUtility.EndStaticPreview();

            // Reset back to whatever asset was used before the rendering
            HDUtils.RestoreRenderPipelineAsset(assetUsedFromQuality, currentPipelineAsset);

            // Dummy empty render call to reset the pipeline in RenderPipelineManager
            m_PreviewUtility.camera.Render();

            return outTexture;

        }
    }
}
