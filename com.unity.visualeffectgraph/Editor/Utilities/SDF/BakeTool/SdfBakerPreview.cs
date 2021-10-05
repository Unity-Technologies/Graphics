using System;
using UnityEngine;
using Transform = UnityEngine.Transform;
using UObject = UnityEngine.Object;
using UnityEditor;

namespace UnityEditor.VFX.SDF
{
    class SdfBakerPreview
    {
        internal static class Styles
        {
            internal static readonly GUIContent wireframeToggle = EditorGUIUtility.TrTextContent("Wireframe", "Show wireframe");
            internal static readonly GUIContent orthographicToggle = EditorGUIUtility.TrTextContent("Orthographic view");
            internal static readonly GUIContent showActualBox = EditorGUIUtility.TrTextContent("Show Actual Box");
            internal static readonly GUIContent showDesiredBox = EditorGUIUtility.TrTextContent("Show Desired Box");

            internal static GUIStyle preSlider = "preSlider";
        }

        internal class Settings
        {
            public bool drawWire = true;
            public bool showActualBox = true;
            public bool showDesiredBox = true;

            public Vector3 orthoPosition = new Vector3(0.0f, 0.0f, 0.0f);
            public Vector2 previewDir = new Vector2(0, 0);
            public Vector2 lightDir = new Vector2(0, 0);
            public Vector3 pivotPositionOffset = Vector3.zero;
            public float zoomFactor = 1.0f;

            public Material shadedPreviewMaterial;
            public Material activeMaterial;
            public Material wireMaterial;

            public Settings()
            {
                shadedPreviewMaterial = new Material(Shader.Find("Standard"));
                shadedPreviewMaterial.hideFlags = HideFlags.DontSave;
                wireMaterial = CreateWireframeMaterial();

                activeMaterial = shadedPreviewMaterial;

                orthoPosition = new Vector3(0.5f, 0.5f, -1);
                previewDir = new Vector2(130, 0);
                lightDir = new Vector2(-40, -40);
                zoomFactor = 1.0f;
            }

            public void Dispose()
            {
                if (shadedPreviewMaterial != null)
                    UObject.DestroyImmediate(shadedPreviewMaterial);
                if (wireMaterial != null)
                    UObject.DestroyImmediate(wireMaterial);
            }
        }


        Mesh m_Target;

        internal Mesh mesh
        {
            get => m_Target;
            set => m_Target = value;
        }

        private Vector3 m_SizeBoxReference = Vector3.one;
        private Vector3 m_ActualSizeBox = Vector3.one;
        private Vector3 m_CenterBox = Vector3.zero;
        private Color m_ActualBoxColor = new Color(0, 255.0f / 255, 70.0f / 255);

        internal Vector3 sizeBoxReference
        {
            get => m_SizeBoxReference;
            set => m_SizeBoxReference = value;
        }

        internal Vector3 actualSizeBox
        {
            get => m_ActualSizeBox;
            set => m_ActualSizeBox = value;
        }
        internal Vector3 centerBox
        {
            get => m_CenterBox;
            set => m_CenterBox = value;
        }


        private bool m_Orthographic = false;

        internal bool orthographic
        {
            get => m_Orthographic;
            set => m_Orthographic = value;
        }


        PreviewRenderUtility m_PreviewUtility;
        Settings m_Settings;


        internal SdfBakerPreview(Mesh target)
        {
            m_Target = target;

            m_PreviewUtility = new PreviewRenderUtility();
            m_PreviewUtility.camera.fieldOfView = 30.0f;
            m_PreviewUtility.camera.transform.position = new Vector3(5, 5, 0);

            m_Settings = new Settings();
        }

        internal void Dispose()
        {
            m_PreviewUtility.Cleanup();
            m_Settings.Dispose();
        }

        static Material CreateWireframeMaterial()
        {
            var shader = Shader.Find("Hidden/Internal-Colored");
            if (!shader)
            {
                Debug.LogWarning("Could not find the built-in Internal-Colored shader");
                return null;
            }
            var mat = new Material(shader);
            mat.hideFlags = HideFlags.HideAndDontSave;
            mat.SetColor("_Color", new Color(0, 0, 0, 0.3f));
            mat.SetFloat("_ZWrite", 0.0f);
            mat.SetFloat("_ZBias", -1.0f);
            return mat;
        }

        void ResetView()
        {
            m_Settings.zoomFactor = 1.0f;
            m_Settings.orthoPosition = new Vector3(0.5f, 0.5f, -1);
            m_Settings.pivotPositionOffset = Vector3.zero;
        }

        internal void RenderMeshPreview(
            Mesh mesh,
            PreviewRenderUtility previewUtility,
            Settings settings,
            int meshSubset)
        {
            if (mesh == null || previewUtility == null)
                return;

            Bounds bounds = mesh.bounds;

            UnityEngine.Transform renderCamTransform = previewUtility.camera.GetComponent<UnityEngine.Transform>();
            if (m_Orthographic)
            {
                previewUtility.camera.nearClipPlane = 1;
                previewUtility.camera.farClipPlane = 1 + sizeBoxReference.magnitude;
                previewUtility.camera.orthographicSize = mesh.bounds.extents.y * 1.1f;
            }
            else
            {
                previewUtility.camera.nearClipPlane = 0.0001f;
                previewUtility.camera.farClipPlane = 1000f;
            }

            float halfSize = bounds.extents.magnitude;
            float distance = 4.0f * halfSize;

            previewUtility.camera.orthographic = m_Orthographic;
            Quaternion camRotation = Quaternion.identity;
            Vector3 camPosition = camRotation * Vector3.forward * (-distance * settings.zoomFactor) + settings.pivotPositionOffset;

            renderCamTransform.position = camPosition;
            renderCamTransform.rotation = camRotation;

            previewUtility.lights[0].intensity = 1.1f;
            previewUtility.lights[0].transform.rotation = Quaternion.Euler(-settings.lightDir.y, -settings.lightDir.x, 0);
            previewUtility.lights[1].intensity = 1.1f;
            previewUtility.lights[1].transform.rotation = Quaternion.Euler(settings.lightDir.y, settings.lightDir.x, 0);

            previewUtility.ambientColor = new Color(.1f, .1f, .1f, 0);

            RenderMeshPreviewSkipCameraAndLighting(mesh, bounds, previewUtility, settings, null, meshSubset);
        }

        internal static Color GetSubMeshTint(int index)
        {
            // color palette generator based on "golden ratio" idea, like in
            // https://martin.ankerl.com/2009/12/09/how-to-create-random-colors-programmatically/
            var hue = Mathf.Repeat(index * 0.618f, 1);
            var sat = index == 0 ? 0f : 0.3f;
            var val = 1f;
            return Color.HSVToRGB(hue, sat, val);
        }

        internal void RenderMeshPreviewSkipCameraAndLighting(
            Mesh mesh,
            Bounds bounds,
            PreviewRenderUtility previewUtility,
            Settings settings,
            MaterialPropertyBlock customProperties,
            int meshSubset) // -1 for whole mesh
        {
            if (mesh == null || previewUtility == null)
                return;

            Quaternion rot = Quaternion.Euler(settings.previewDir.y, 0, 0) * Quaternion.Euler(0, settings.previewDir.x, 0);
            Vector3 pos = rot * (-bounds.center);

            bool oldFog = RenderSettings.fog;
            Unsupported.SetRenderSettingsUseFogNoDirty(false);

            int submeshes = mesh.subMeshCount;
            var tintSubmeshes = false;
            var colorPropID = 0;
            if (submeshes > 1 && customProperties == null && meshSubset == -1)
            {
                tintSubmeshes = true;
                customProperties = new MaterialPropertyBlock();
                colorPropID = Shader.PropertyToID("_Color");
            }

            if (settings.activeMaterial != null)
            {
                previewUtility.camera.clearFlags = CameraClearFlags.Nothing;
                if (meshSubset < 0 || meshSubset >= submeshes)
                {
                    for (int i = 0; i < submeshes; ++i)
                    {
                        if (tintSubmeshes)
                            customProperties.SetColor(colorPropID, GetSubMeshTint(i));
                        previewUtility.DrawMesh(mesh, pos, rot, settings.activeMaterial, i, customProperties);
                    }
                }
                else
                    previewUtility.DrawMesh(mesh, pos, rot, settings.activeMaterial, meshSubset, customProperties);
                previewUtility.Render(false, false);
            }

            if (settings.wireMaterial != null && settings.drawWire)
            {
                previewUtility.camera.clearFlags = CameraClearFlags.Nothing;
                GL.wireframe = true;
                if (tintSubmeshes)
                    customProperties.SetColor(colorPropID, settings.wireMaterial.color);
                if (meshSubset < 0 || meshSubset >= submeshes)
                {
                    for (int i = 0; i < submeshes; ++i)
                    {
                        // lines/points already are wire-like; it does not make sense to overdraw
                        // them again with dark wireframe color
                        var topology = mesh.GetTopology(i);
                        if (topology == MeshTopology.Lines || topology == MeshTopology.LineStrip || topology == MeshTopology.Points)
                            continue;
                        previewUtility.DrawMesh(mesh, pos, rot, settings.wireMaterial, i, customProperties);
                    }
                }
                else
                    previewUtility.DrawMesh(mesh, pos, rot, settings.wireMaterial, meshSubset, customProperties);
                previewUtility.Render(false, false);

                GL.wireframe = false;
            }

            Unsupported.SetRenderSettingsUseFogNoDirty(oldFog);
        }

        void DoRenderPreview()
        {
            RenderMeshPreview(mesh, m_PreviewUtility, m_Settings, -1);
        }

        internal void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            var evt = Event.current;

            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                if (evt.type == EventType.Repaint)
                    EditorGUI.DropShadowLabel(new Rect(rect.x, rect.y, rect.width, 40),
                        "Mesh preview requires\nrender texture support");
                return;
            }


            if (evt.button <= 0)
                m_Settings.previewDir = PreviewGUI.Drag2D(m_Settings.previewDir, rect);

            if (evt.button == 1)
                m_Settings.lightDir = PreviewGUI.Drag2D(m_Settings.lightDir, rect);

            if (evt.type == EventType.ScrollWheel && rect.Contains(evt.mousePosition))
                MeshPreviewZoom(rect, evt);

            if (evt.type == EventType.MouseDrag && evt.button == 2 && rect.Contains(evt.mousePosition))
                MeshPreviewPan(rect, evt);

            if (evt.type != EventType.Repaint)
                return;

            m_PreviewUtility.BeginPreview(rect, background);

            DoRenderPreview();

            Handles.EndGUI();
            Handles.SetCamera(m_PreviewUtility.camera);
            Quaternion rot = Quaternion.Euler(m_Settings.previewDir.y, 0, 0) * Quaternion.Euler(0, m_Settings.previewDir.x, 0);
            Vector3 pos = Vector3.zero;
            Handles.matrix = Matrix4x4.TRS(pos, rot, Vector3.one);
            if (m_Settings.showDesiredBox)
            {
                Handles.DrawWireCube(m_CenterBox - mesh.bounds.center, m_SizeBoxReference);
            }
            if (m_Settings.showActualBox)
            {
                Color prevColor = Handles.color;
                Handles.color = m_ActualBoxColor;
                Handles.DrawWireCube(m_CenterBox - mesh.bounds.center, m_ActualSizeBox);
                Handles.color = prevColor;
            }
            m_PreviewUtility.EndAndDrawPreview(rect);

            EditorGUI.DropShadowLabel(rect, GetInfoString(mesh));
        }

        internal void OnPreviewSettings()
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
                return;

            GUI.enabled = true;
            using (new EditorGUI.DisabledScope(false))
            {
                m_Settings.showActualBox = GUILayout.Toggle(m_Settings.showActualBox, Styles.showActualBox,
                    EditorStyles.toolbarButton, GUILayout.MinWidth(100));
                m_Settings.showDesiredBox = GUILayout.Toggle(m_Settings.showDesiredBox, Styles.showDesiredBox,
                    EditorStyles.toolbarButton, GUILayout.MinWidth(105));
                m_Settings.drawWire = GUILayout.Toggle(m_Settings.drawWire, Styles.wireframeToggle, EditorStyles.toolbarButton, GUILayout.MaxWidth(70));
            }
        }

        void MeshPreviewZoom(Rect rect, Event evt)
        {
            float zoomDelta = -(HandleUtility.niceMouseDeltaZoom * 0.5f) * 0.05f;
            var newZoom = m_Settings.zoomFactor + m_Settings.zoomFactor * zoomDelta;
            newZoom = Mathf.Clamp(newZoom, 0.1f, 10.0f);

            // we want to zoom around current mouse position
            var mouseViewPos = new Vector2(
                evt.mousePosition.x / rect.width,
                1 - evt.mousePosition.y / rect.height);
            var mouseWorldPos = m_PreviewUtility.camera.ViewportToWorldPoint(mouseViewPos);
            var mouseToCamPos = m_Settings.orthoPosition - mouseWorldPos;
            var newCamPos = mouseWorldPos + mouseToCamPos * (newZoom / m_Settings.zoomFactor);


            m_Settings.orthoPosition.x = newCamPos.x;
            m_Settings.orthoPosition.y = newCamPos.y;


            m_Settings.zoomFactor = newZoom;
            evt.Use();
        }

        void MeshPreviewPan(Rect rect, Event evt)
        {
            var cam = m_PreviewUtility.camera;

            // event delta is in "screen" units of the preview rect, but the
            // preview camera is rendering into a render target that could
            // be different size; have to adjust drag position to match
            var delta = new Vector3(
                -evt.delta.x * cam.pixelWidth / rect.width,
                evt.delta.y * cam.pixelHeight / rect.height,
                0);

            Vector3 screenPos;
            Vector3 worldPos;

            screenPos = cam.WorldToScreenPoint(m_Settings.pivotPositionOffset);
            screenPos += delta;
            worldPos = cam.ScreenToWorldPoint(screenPos) - m_Settings.pivotPositionOffset;
            m_Settings.pivotPositionOffset += worldPos;


            evt.Use();
        }

        static string GetInfoString(Mesh mesh)
        {
            if (mesh == null)
                return "";

            string info = $"{mesh.vertexCount} Vertices, {InternalMeshUtil.GetPrimitiveCount(mesh)} Triangles";

            int submeshes = mesh.subMeshCount;
            if (submeshes > 1)
                info += $", {submeshes} Sub Meshes";

            int blendShapeCount = mesh.blendShapeCount;
            if (blendShapeCount > 0)
                info += $", {blendShapeCount} Blend Shapes";

            info += " | " + InternalMeshUtil.GetVertexFormat(mesh);
            return info;
        }
    }
}
