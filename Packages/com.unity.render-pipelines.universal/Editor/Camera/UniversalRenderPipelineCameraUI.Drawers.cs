using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<UniversalRenderPipelineSerializedCamera>;

    static partial class UniversalRenderPipelineCameraUI
    {
        [URPHelpURL("camera-component-reference")]
        public enum Expandable
        {
            /// <summary> Projection</summary>
            Projection = 1 << 0,
            /// <summary> Physical</summary>
            Physical = 1 << 1,
            /// <summary> Output</summary>
            Output = 1 << 2,
            /// <summary> Orthographic</summary>
            Orthographic = 1 << 3,
            /// <summary> RenderLoop</summary>
            RenderLoop = 1 << 4,
            /// <summary> Rendering</summary>
            Rendering = 1 << 5,
            /// <summary> Environment</summary>
            Environment = 1 << 6,
            /// <summary> Stack</summary>
            Stack = 1 << 7,
        }

        public enum ExpandableAdditional
        {
            /// <summary> Rendering</summary>
            Rendering = 1 << 0,
        }

        static readonly ExpandedState<Expandable, Camera> k_ExpandedState = new(Expandable.Projection, "URP");
        static readonly AdditionalPropertiesState<ExpandableAdditional, Camera> k_ExpandedAdditionalState = new(0, "URP");

        public static readonly CED.IDrawer SectionProjectionSettings = CED.FoldoutGroup(
            CameraUI.Styles.projectionSettingsHeaderContent,
            Expandable.Projection,
            k_ExpandedState,
            FoldoutOption.Indent,
            CED.Group(
                DrawerProjection
                ),
            PhysicalCamera.Drawer
        );

        public static readonly CED.IDrawer SectionStackSettings =
            CED.Conditional(
                (serialized, editor) => (CameraRenderType)serialized.cameraType.intValue == CameraRenderType.Base,
                CED.FoldoutGroup(Styles.stackSettingsText, Expandable.Stack, k_ExpandedState, FoldoutOption.Indent, CED.Group(DrawerStackCameras)));

        public static readonly CED.IDrawer[] Inspector =
        {
            CED.Group(
                PrepareOnTileValidationWarning,
                DrawerCameraType
                ),
            SectionProjectionSettings,
            Rendering.Drawer,
            SectionStackSettings,
            Environment.Drawer,
            Output.Drawer
        };

        static void DrawerProjection(UniversalRenderPipelineSerializedCamera p, Editor owner)
        {
            var camera = p.serializedObject.targetObject as Camera;
            bool pixelPerfectEnabled = camera.TryGetComponent<PixelPerfectCamera>(out var pixelPerfectCamera) && pixelPerfectCamera.enabled;
            if (pixelPerfectEnabled)
                EditorGUILayout.HelpBox(Styles.pixelPerfectInfo, MessageType.Info);

            using (new EditorGUI.DisabledGroupScope(pixelPerfectEnabled))
                CameraUI.Drawer_Projection(p, owner);
        }

        static void DrawerCameraType(UniversalRenderPipelineSerializedCamera p, Editor owner)
        {
            int selectedRenderer = p.renderer.intValue;
            ScriptableRenderer scriptableRenderer = UniversalRenderPipeline.asset.GetRenderer(selectedRenderer);

            EditorGUI.BeginChangeCheck();

            CameraRenderType originalCamType = (CameraRenderType)p.cameraType.intValue;
            CameraRenderType camType = scriptableRenderer.SupportsCameraStackingType(CameraRenderType.Overlay) ? originalCamType : CameraRenderType.Base;
            EditorGUI.BeginDisabledGroup(scriptableRenderer.SupportedCameraStackingTypes() == 0);
            camType = (CameraRenderType)EditorGUILayout.EnumPopup(
                Styles.cameraType,
                camType,
                e => scriptableRenderer.SupportsCameraStackingType((CameraRenderType)e),
                false
            );
            EditorGUI.EndDisabledGroup();

            if (EditorGUI.EndChangeCheck() || camType != originalCamType)
            {
                p.cameraType.intValue = (int)camType;
                if (camType == CameraRenderType.Overlay)
                {
                    p.baseCameraSettings.clearFlags.intValue = (int)CameraClearFlags.Nothing;
                }
            }

            EditorGUILayout.Space();
        }

        static void DrawerStackCameras(UniversalRenderPipelineSerializedCamera p, Editor owner)
        {
            if (owner is UniversalRenderPipelineCameraEditor cameraEditor)
            {
                cameraEditor.DrawStackSettings();
                DisplayOnTileValidationWarning(p.cameras, p => p.arraySize != 0, Styles.cameraStackLabelForOnTileValidation, p);
            }
        }

        struct OnTileValidationInfos
        {
            public readonly bool enabled;
            public readonly string rendererName;
            public readonly ScriptableRendererData assetToOpen;
            public OnTileValidationInfos(string rendererName, ScriptableRendererData assetToOpen)
            {
                enabled = true;
                this.rendererName = rendererName;
                this.assetToOpen = assetToOpen;
            }
        }

        static OnTileValidationInfos lastOnTileValidationInfos;

        static void PrepareOnTileValidationWarning(UniversalRenderPipelineSerializedCamera serialized, Editor owner)
        {
            // Rules:
            //   - mono selection: Display warning if RendererData's OnTileValidation is enabled (with 'Open' behaviour)
            //   - multi selection:
            //      - Display warning if all RendererData's OnTileValidation are enabled
            //      - Only have 'Open' behaviour if all RendererData are the same
            
            lastOnTileValidationInfos = default;

            // Note: UniversalRenderPipeline.asset should not be null or this inspector would not be shown.
            // Just in case off though:
            if (UniversalRenderPipeline.asset == null)
                return;

            bool HasOnTileValidationAtIndex(int index, out ScriptableRendererData rendererData)
                =>  UniversalRenderPipeline.asset.TryGetRendererData(index, out rendererData)
                    && rendererData is UniversalRendererData universalData 
                    && universalData.onTileValidation;

            // If impacted section are not opened, early exit
            if (!(k_ExpandedState[Expandable.Rendering] || k_ExpandedState[Expandable.Stack]))
                return;

            ScriptableRendererData rendererData = null;

            if (!serialized.renderer.hasMultipleDifferentValues)
            {
                if (!HasOnTileValidationAtIndex(serialized.renderer.intValue, out rendererData))
                    return;

                lastOnTileValidationInfos = new OnTileValidationInfos($"'{rendererData.name}'", assetToOpen: rendererData);
                return;
            }

            bool targetSameAsset = true;
            var firstAdditionalData = (UniversalAdditionalCameraData)serialized.serializedAdditionalDataObject.targetObjects[0];
            if (!HasOnTileValidationAtIndex(firstAdditionalData.rendererIndex, out rendererData))
                return;

            using var o = StringBuilderPool.Get(out var sb);
            sb.Append("'");
            sb.Append(rendererData.name);
            sb.Append("'");
            for (int i = 1; i < serialized.serializedAdditionalDataObject.targetObjects.Length; ++i)
            {
                var additionalCameraData = (UniversalAdditionalCameraData)serialized.serializedAdditionalDataObject.targetObjects[i];
                if (!HasOnTileValidationAtIndex(additionalCameraData.rendererIndex, out var otherRenderer))
                    return;

                targetSameAsset &= rendererData == otherRenderer;
                sb.Append(", '");
                sb.Append(otherRenderer.name);
                sb.Append("'");
            }

            lastOnTileValidationInfos = new OnTileValidationInfos(sb.ToString(), assetToOpen: targetSameAsset ? rendererData : null);
        }

        static void DisplayOnTileValidationWarning(SerializedProperty prop, Func<SerializedProperty, bool> shouldDisplayWarning, GUIContent label, UniversalRenderPipelineSerializedCamera serialized)
        {
            if (!lastOnTileValidationInfos.enabled
                || prop == null 
                || shouldDisplayWarning == null 
                || prop.hasMultipleDifferentValues 
                || !shouldDisplayWarning(prop))
                return;

            if (lastOnTileValidationInfos.assetToOpen != null)
                CoreEditorUtils.DrawFixMeBox(
                    string.Format(Styles.formaterOnTileValidation, label == null ? prop.displayName : label.text, lastOnTileValidationInfos.rendererName),
                    MessageType.Warning,
                    "Open",
                    () => AssetDatabase.OpenAsset(lastOnTileValidationInfos.assetToOpen));
            else
                EditorGUILayout.HelpBox(
                    string.Format(Styles.formaterOnTileValidation, label == null ? prop.displayName : label.text, lastOnTileValidationInfos.rendererName),
                    MessageType.Warning);
        }
    }
}
