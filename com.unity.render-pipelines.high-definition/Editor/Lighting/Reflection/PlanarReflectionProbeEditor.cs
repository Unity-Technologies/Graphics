using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditorForRenderPipeline(typeof(PlanarReflectionProbe), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    sealed class PlanarReflectionProbeEditor : HDProbeEditor<PlanarReflectionProbeUISettingsProvider, SerializedPlanarReflectionProbe>
    {
        public static Material GUITextureBlit2SRGBMaterial
                => HDRenderPipeline.defaultAsset.renderPipelineEditorResources.materials.GUITextureBlit2SRGB;

        const float k_PreviewHeight = 128;

        static Mesh k_QuadMesh;
        static Material k_PreviewMaterial;
        static Material k_PreviewOutlineMaterial;

        static GUIContent s_MipMapLow, s_MipMapHigh, s_ExposureLow;
        static GUIStyle s_PreLabel;

        public float previewExposure = 0f;
        public float mipLevelPreview = 0f;

        static Material _previewMaterial;
        static Material previewMaterial
        {
            get
            {
                if (_previewMaterial == null)
                    _previewMaterial = new Material(HDRenderPipeline.defaultAsset.renderPipelineEditorResources.materials.GUITextureBlit2SRGB);
                return _previewMaterial;
            }
        }

        bool firstDraw = true;

        List<Texture> m_PreviewedTextures = new List<Texture>();

        public override bool HasPreviewGUI()
        {
            foreach (var p in m_TypedTargets)
            {
                if (p.texture != null)
                    return true;
            }
            return false;
        }

        public override GUIContent GetPreviewTitle() => EditorGUIUtility.TrTextContent("Planar Reflection");

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            m_PreviewedTextures.Clear();
            foreach (var p in m_TypedTargets)
                m_PreviewedTextures.Add(p.texture);

            var space = Vector2.one;
            var rowSize = Mathf.CeilToInt(Mathf.Sqrt(m_PreviewedTextures.Count));
            var size = r.size / rowSize - space * (rowSize - 1);

            previewMaterial.SetFloat("_ExposureBias", previewExposure);
            previewMaterial.SetFloat("_MipLevel", mipLevelPreview);
            // We don't have the Exposure texture in the inspector so we bind white instead.
            previewMaterial.SetTexture("_Exposure", Texture2D.whiteTexture);

            for (var i = 0; i < m_PreviewedTextures.Count; i++)
            {
                var row = i / rowSize;
                var col = i % rowSize;
                var itemRect = new Rect(
                        r.x + size.x * row + ((row > 0) ? (row - 1) * space.x : 0),
                        r.y + size.y * col + ((col > 0) ? (col - 1) * space.y : 0),
                        size.x,
                        size.y);

                if (m_PreviewedTextures[i] != null)
                    EditorGUI.DrawPreviewTexture(itemRect, m_PreviewedTextures[i], previewMaterial, ScaleMode.ScaleToFit, 0, 1);
                else
                    EditorGUI.LabelField(itemRect, EditorGUIUtility.TrTextContent("Not Available"));
            }
        }

        public override void OnPreviewSettings()
        {
            if (s_MipMapLow == null)
                InitIcons();

            GUILayout.Box(s_ExposureLow, s_PreLabel, GUILayout.MaxWidth(20));
            previewExposure = GUILayout.HorizontalSlider(previewExposure, -20f, 20f, GUILayout.MaxWidth(80));
            GUILayout.Space(5);

            // For now we don't display the mip level slider because they are black. The convolution of the probe
            // texture is made in the atlas and so is not available in the texture we have here.
#if false
            int mipmapCount = m_PreviewedTextures.Count > 0 ? m_PreviewedTextures[0].mipmapCount : 1;

            GUILayout.Box(s_MipMapHigh, s_PreLabel, GUILayout.MaxWidth(20));
            mipLevelPreview = GUILayout.HorizontalSlider(mipLevelPreview, 0, mipmapCount, GUILayout.MaxWidth(80));
            GUILayout.Box(s_MipMapLow, s_PreLabel, GUILayout.MaxWidth(20));
#endif
        }

        protected override SerializedPlanarReflectionProbe NewSerializedObject(SerializedObject so)
            => new SerializedPlanarReflectionProbe(so);
        internal override HDProbe GetTarget(Object editorTarget) => editorTarget as HDProbe;

        protected override void DrawAdditionalCaptureSettings(
            SerializedPlanarReflectionProbe serialized, Editor owner
        )
        {
            var isReferencePositionRelevant = serialized.probeSettings.mode.intValue != (int)ProbeSettings.Mode.Realtime;
            if (!isReferencePositionRelevant)
                return;

            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(serialized.localReferencePosition, EditorGUIUtility.TrTextContent("Reference Local Position"));
            --EditorGUI.indentLevel;
        }

        protected override void DrawHandles(SerializedPlanarReflectionProbe serialized, Editor owner)
        {
            base.DrawHandles(serialized, owner);

            SceneViewOverlay_Window(EditorGUIUtility.TrTextContent(target.name), OnOverlayGUI, -100, target);

            if (serialized.probeSettings.mode.intValue != (int)ProbeSettings.Mode.Realtime)
            {
                using (new Handles.DrawingScope(Matrix4x4.TRS(serialized.target.transform.position, serialized.target.transform.rotation, Vector3.one)))
                {
                    var referencePosition = serialized.localReferencePosition.vector3Value;
                    EditorGUI.BeginChangeCheck();
                    referencePosition = Handles.PositionHandle(referencePosition, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck())
                        serialized.localReferencePosition.vector3Value = referencePosition;
                }
            }
        }

        void OnOverlayGUI(Object target, SceneView sceneView)
        {
            // Draw a preview of the captured texture from the planar reflection

            // Get the exposure texture used in this scene view
            if (!(RenderPipelineManager.currentPipeline is HDRenderPipeline hdrp))
                return;
            var hdCamera = HDCamera.GetOrCreate(sceneView.camera);
            var exposureTex = hdrp.GetExposureTexture(hdCamera);

            var index = Array.IndexOf(m_TypedTargets, target);
            if (index == -1)
                return;
            var p = m_TypedTargets[index];
            if (p.texture == null)
                return;

            var previewWidth = k_PreviewHeight;
            var previewSize = new Rect(previewWidth, k_PreviewHeight + EditorGUIUtility.singleLineHeight + 2, 0, 0);

            if (Event.current.type == EventType.Layout
                || !firstDraw && Event.current.type == EventType.Repaint)
            {
                // Get and reserve rect
                //this can cause the following issue if calls on a repaint before a layout:
                //ArgumentException: Getting control 0's position in a group with only 0 controls when doing repaint
                var cameraRect = GUILayoutUtility.GetRect(previewSize.x, previewSize.y);
                firstDraw = false;

                // The aspect ratio of the capture texture may not be the aspect of the texture
                // So we need to stretch back the texture to the aspect used during the capture
                // to give users a non distorded preview of the capture.
                // Here we compute a centered rect that has the correct aspect for the texture preview.
                var c = new Rect(cameraRect);
                c.y += EditorGUIUtility.singleLineHeight + 2;
                if (p.renderData.aspect > 1)
                {
                    c.width = k_PreviewHeight;
                    c.height = k_PreviewHeight / p.renderData.aspect;
                    c.y += (k_PreviewHeight - c.height) * 0.5f;
                }
                else
                {
                    c.width = k_PreviewHeight * p.renderData.aspect;
                    c.height = k_PreviewHeight;
                    c.x += (k_PreviewHeight - c.width) * 0.5f;
                }

                // Setup the material to draw the quad with the exposure texture
                var material = GUITextureBlit2SRGBMaterial;
                material.SetTexture("_Exposure", exposureTex);
                Graphics.DrawTexture(c, p.texture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, material, -1);

                // We now display the FoV and aspect used during the capture of the planar reflection
                var fovRect = new Rect(cameraRect);
                fovRect.x += 5;
                fovRect.y += 2;
                fovRect.width -= 10;
                fovRect.height = EditorGUIUtility.singleLineHeight;
                var width = fovRect.width;
                fovRect.width = width * 0.5f;
                GUI.TextField(fovRect, $"F: {p.renderData.fieldOfView:F2}°");
                fovRect.x += width * 0.5f;
                fovRect.width = width * 0.5f;
                GUI.TextField(fovRect, $"A: {p.renderData.aspect:F2}");
            }
        }

        static Type k_SceneViewOverlay_WindowFunction = Type.GetType("UnityEditor.SceneViewOverlay+WindowFunction,UnityEditor");
        static Type k_SceneViewOverlay_WindowDisplayOption = Type.GetType("UnityEditor.SceneViewOverlay+WindowDisplayOption,UnityEditor");
        static MethodInfo k_SceneViewOverlay_Window = Type.GetType("UnityEditor.SceneViewOverlay,UnityEditor")
            .GetMethod(
                "Window",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                CallingConventions.Any,
                new[] { typeof(GUIContent), k_SceneViewOverlay_WindowFunction, typeof(int), typeof(Object), k_SceneViewOverlay_WindowDisplayOption, typeof(EditorWindow) },
                null);
        static void SceneViewOverlay_Window(GUIContent title, Action<Object, SceneView> sceneViewFunc, int order, Object target)
        {
            k_SceneViewOverlay_Window.Invoke(null, new[]
            {
                title, DelegateUtility.Cast(sceneViewFunc, k_SceneViewOverlay_WindowFunction),
                order,
                target,
                Enum.ToObject(k_SceneViewOverlay_WindowDisplayOption, 1),
                null
            });
        }

        [DrawGizmo(GizmoType.Selected)]
        static void DrawSelectedGizmo(PlanarReflectionProbe probe, GizmoType gizmoType)
        {
            var e = (PlanarReflectionProbeEditor)GetEditorFor(probe);
            if (e == null)
                return;

            var mat = Matrix4x4.TRS(probe.transform.position, probe.transform.rotation, Vector3.one);
            InfluenceVolumeUI.DrawGizmos(
                probe.influenceVolume,
                mat,
                InfluenceVolumeUI.HandleType.None,
                InfluenceVolumeUI.HandleType.Base | InfluenceVolumeUI.HandleType.Influence
            );

            if (e.showChromeGizmo)
                DrawCapturePositionGizmo(probe);
        }

        static void DrawCapturePositionGizmo(PlanarReflectionProbe probe)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            // Capture gizmo
            if (k_QuadMesh == null)
                k_QuadMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            if (k_PreviewMaterial == null)
                k_PreviewMaterial = new Material(Shader.Find("Debug/PlanarReflectionProbePreview"));
            if (k_PreviewOutlineMaterial == null)
                k_PreviewOutlineMaterial = new Material(Shader.Find("Hidden/UnlitTransparentColored"));

            var proxyToWorld = probe.proxyToWorld;
            var settings = probe.settings;

            // When a user creates a new mirror, the capture position is at the exact position of the mirror mesh.
            // We need to offset slightly the gizmo to avoid a Z-fight in that case, as it looks like a bug
            // for users discovering the planar reflection.
            var mirrorPositionProxySpace = settings.proxySettings.mirrorPositionProxySpace;
            mirrorPositionProxySpace += settings.proxySettings.mirrorRotationProxySpace * Vector3.forward * 0.001f;

            var mirrorPosition = proxyToWorld.MultiplyPoint(mirrorPositionProxySpace);
            var mirrorRotation = (proxyToWorld.rotation * settings.proxySettings.mirrorRotationProxySpace * Quaternion.Euler(0, 180, 0)).normalized;
            var renderData = probe.renderData;

            var gpuProj = GL.GetGPUProjectionMatrix(renderData.projectionMatrix, true);
            var gpuView = renderData.worldToCameraRHS;
            var vp = gpuProj * gpuView;

            var cameraPositionWS = Vector3.zero;
            var capturePositionWS = renderData.capturePosition;
            if (SceneView.currentDrawingSceneView?.camera != null)
                cameraPositionWS = SceneView.currentDrawingSceneView.camera.transform.position;
            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                cameraPositionWS = Vector3.zero;
                // For Camera relative rendering, we need to translate with the position of the currently rendering camera
                capturePositionWS -= cameraPositionWS;
            }

            // Draw outline
            k_PreviewOutlineMaterial.SetColor("_Color", InfluenceVolumeUI.k_GizmoThemeColorBase);
            k_PreviewOutlineMaterial.SetPass(0);
            Graphics.DrawMeshNow(k_QuadMesh, Matrix4x4.TRS(mirrorPosition, mirrorRotation, Vector3.one * capturePointPreviewSize * 2.1f));

            k_PreviewMaterial.SetTexture("_MainTex", probe.texture);
            k_PreviewMaterial.SetMatrix("_CaptureVPMatrix", vp);
            k_PreviewMaterial.SetFloat("_Exposure", 1.0f);
            k_PreviewMaterial.SetVector("_CameraPositionWS", new Vector4(cameraPositionWS.x, cameraPositionWS.y, -cameraPositionWS.z, 0));
            k_PreviewMaterial.SetVector("_CapturePositionWS", new Vector4(capturePositionWS.x, capturePositionWS.y, -capturePositionWS.z, 0));
            k_PreviewMaterial.SetPass(0);
            Graphics.DrawMeshNow(k_QuadMesh, Matrix4x4.TRS(mirrorPosition, mirrorRotation, Vector3.one * capturePointPreviewSize * 2));
        }

        static void InitIcons()
        {
            s_MipMapLow = EditorGUIUtility.IconContent("PreTextureMipMapLow");
            s_MipMapHigh = EditorGUIUtility.IconContent("PreTextureMipMapHigh");
            s_ExposureLow = EditorGUIUtility.IconContent("SceneViewLighting");
            s_PreLabel = "preLabel";
        }
    }

    struct PlanarReflectionProbeUISettingsProvider : HDProbeUI.IProbeUISettingsProvider, InfluenceVolumeUI.IInfluenceUISettingsProvider
    {
        bool InfluenceVolumeUI.IInfluenceUISettingsProvider.drawOffset => false;
        bool InfluenceVolumeUI.IInfluenceUISettingsProvider.drawNormal => false;
        bool InfluenceVolumeUI.IInfluenceUISettingsProvider.drawFace => false;

        ProbeSettingsOverride HDProbeUI.IProbeUISettingsProvider.displayedCaptureSettings => new ProbeSettingsOverride
        {
            probe = ProbeSettingsFields.frustumFieldOfViewMode
                | ProbeSettingsFields.frustumAutomaticScale
                | ProbeSettingsFields.frustumViewerScale
                | ProbeSettingsFields.frustumFixedValue
                | ProbeSettingsFields.resolution
                | ProbeSettingsFields.roughReflections,
            camera = new CameraSettingsOverride
            {
                camera = (CameraSettingsFields)(-1) & ~(
                   CameraSettingsFields.flipYMode
                   | CameraSettingsFields.frustumAspect
                   | CameraSettingsFields.cullingInvertFaceCulling
                   | CameraSettingsFields.frustumMode
                   | CameraSettingsFields.frustumProjectionMatrix
                   | CameraSettingsFields.frustumFieldOfView
               )
            }
        };

        public ProbeSettingsOverride displayedAdvancedCaptureSettings => new ProbeSettingsOverride
        {
            probe = ProbeSettingsFields.proxyMirrorPositionProxySpace
                | ProbeSettingsFields.proxyMirrorRotationProxySpace
                | ProbeSettingsFields.lightingRangeCompression,
            camera = new CameraSettingsOverride()
        };

        ProbeSettingsOverride HDProbeUI.IProbeUISettingsProvider.displayedCustomSettings => new ProbeSettingsOverride
        {
            probe = ProbeSettingsFields.lightingLightLayer
                | ProbeSettingsFields.lightingMultiplier
                | ProbeSettingsFields.lightingWeight
                | ProbeSettingsFields.lightingFadeDistance,
            camera = new CameraSettingsOverride
            {
                camera = CameraSettingsFields.none
            }
        };

        Type HDProbeUI.IProbeUISettingsProvider.customTextureType => typeof(Texture2D);
        static readonly HDProbeUI.ToolBar[] k_Toolbars =
        {
            HDProbeUI.ToolBar.InfluenceShape | HDProbeUI.ToolBar.Blend,
            HDProbeUI.ToolBar.MirrorPosition | HDProbeUI.ToolBar.MirrorRotation,
            HDProbeUI.ToolBar.ShowChromeGizmo
        };
        HDProbeUI.ToolBar[] HDProbeUI.IProbeUISettingsProvider.toolbars => k_Toolbars;

        static Dictionary<KeyCode, HDProbeUI.ToolBar> k_ToolbarShortCutKey = new Dictionary<KeyCode, HDProbeUI.ToolBar>
        {
            { KeyCode.Alpha1, HDProbeUI.ToolBar.InfluenceShape },
            { KeyCode.Alpha2, HDProbeUI.ToolBar.Blend },
            { KeyCode.Alpha3, HDProbeUI.ToolBar.MirrorPosition },
            { KeyCode.Alpha4, HDProbeUI.ToolBar.MirrorRotation }
        };
        Dictionary<KeyCode, HDProbeUI.ToolBar> HDProbeUI.IProbeUISettingsProvider.shortcuts => k_ToolbarShortCutKey;
    }
}
