using System.Linq;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary> Camera UI Shared Properties among SRP</summary>
    public static partial class CameraUI
    {
        /// <summary>Camera Projection type</summary>
        public enum ProjectionType
        {
            /// <summary> Perspective</summary>
            Perspective,
            /// <summary> Orthographic</summary>
            Orthographic
        }

        /// <summary>Camera Projection matrix mode</summary>
        public enum ProjectionMatrixMode
        {
            /// <summary> Explicit</summary>
            Explicit,
            /// <summary> Implicit</summary>
            Implicit,
            /// <summary> PhysicalPropertiesBased</summary>
            PhysicalPropertiesBased,
        }

        static bool s_FovChanged;
        static float s_FovLastValue;

        static ProjectionType DrawerProjectionType(ISerializedCamera p, Editor owner)
        {
            var cam = p.baseCameraSettings;

            ProjectionType projectionType;

            Rect perspectiveRect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(perspectiveRect, Styles.projectionContent, cam.orthographic);
            {
                projectionType = cam.orthographic.boolValue ? ProjectionType.Orthographic : ProjectionType.Perspective;

                EditorGUI.BeginChangeCheck();
                projectionType = (ProjectionType)EditorGUI.EnumPopup(perspectiveRect, Styles.projectionContent, projectionType);
                if (EditorGUI.EndChangeCheck())
                    cam.orthographic.boolValue = (projectionType == ProjectionType.Orthographic);
            }
            EditorGUI.EndProperty();

            return projectionType;
        }

        static void DrawerOrthographicType(ISerializedCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.baseCameraSettings.orthographicSize, Styles.sizeContent);
            Drawer_FieldClippingPlanes(p, owner);
        }

        static void DrawerPerspectiveType(ISerializedCamera p, Editor owner)
        {
            var cam = p.baseCameraSettings;

            var targets = p.serializedObject.targetObjects;
            var camera0 = targets[0] as Camera;

            float fovCurrentValue;
            bool multipleDifferentFovValues = false;
            bool isPhysicalCamera = p.projectionMatrixMode.intValue == (int)ProjectionMatrixMode.PhysicalPropertiesBased;

            var rect = EditorGUILayout.GetControlRect();

            var guiContent = EditorGUI.BeginProperty(rect, Styles.FOVAxisModeContent, cam.fovAxisMode);
            EditorGUI.showMixedValue = cam.fovAxisMode.hasMultipleDifferentValues;

            CoreEditorUtils.DrawEnumPopup<Camera.FieldOfViewAxis>(rect, guiContent, cam.fovAxisMode);

            bool fovAxisVertical = cam.fovAxisMode.intValue == 0;

            if (!fovAxisVertical && !cam.fovAxisMode.hasMultipleDifferentValues)
            {
                float aspectRatio = isPhysicalCamera ? cam.sensorSize.vector2Value.x / cam.sensorSize.vector2Value.y : camera0.aspect;
                // camera.aspect is not serialized so we have to check all targets.
                fovCurrentValue = Camera.VerticalToHorizontalFieldOfView(camera0.fieldOfView, aspectRatio);
                if (targets.Cast<Camera>().Any(camera => camera.fieldOfView != fovCurrentValue))
                    multipleDifferentFovValues = true;
            }
            else
            {
                fovCurrentValue = cam.verticalFOV.floatValue;
                multipleDifferentFovValues = cam.fovAxisMode.hasMultipleDifferentValues;
            }

            EditorGUI.showMixedValue = multipleDifferentFovValues;
            var content = EditorGUI.BeginProperty(EditorGUILayout.BeginHorizontal(), Styles.fieldOfViewContent, cam.verticalFOV);
            EditorGUI.BeginDisabledGroup(p.projectionMatrixMode.hasMultipleDifferentValues || isPhysicalCamera && (cam.sensorSize.hasMultipleDifferentValues || cam.fovAxisMode.hasMultipleDifferentValues));
            EditorGUI.BeginChangeCheck();
            s_FovLastValue = EditorGUILayout.Slider(content, fovCurrentValue, 0.00001f, 179f);
            s_FovChanged = EditorGUI.EndChangeCheck();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndProperty();
            EditorGUI.showMixedValue = false;

            Drawer_FieldClippingPlanes(p, owner);

            content = EditorGUI.BeginProperty(EditorGUILayout.BeginHorizontal(), Styles.physicalCameraContent, p.projectionMatrixMode);
            EditorGUI.showMixedValue = p.projectionMatrixMode.hasMultipleDifferentValues;

            EditorGUI.BeginChangeCheck();
            isPhysicalCamera = EditorGUILayout.Toggle(content, isPhysicalCamera);
            if (EditorGUI.EndChangeCheck())
            {
                p.projectionMatrixMode.intValue = isPhysicalCamera ? (int)ProjectionMatrixMode.PhysicalPropertiesBased : (int)ProjectionMatrixMode.Implicit;
                s_FovChanged = true;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndProperty();

            EditorGUI.showMixedValue = false;
            if (s_FovChanged)
            {
                if (!isPhysicalCamera || p.projectionMatrixMode.hasMultipleDifferentValues)
                {
                    cam.verticalFOV.floatValue = fovAxisVertical
                        ? s_FovLastValue
                        : Camera.HorizontalToVerticalFieldOfView(s_FovLastValue, camera0.aspect);
                }
                else if (!p.projectionMatrixMode.hasMultipleDifferentValues)
                {
                    cam.verticalFOV.floatValue = fovAxisVertical
                        ? s_FovLastValue
                        : Camera.HorizontalToVerticalFieldOfView(s_FovLastValue, camera0.aspect);
                }
            }
        }

        /// <summary>Draws projection related fields on the inspector</summary>
        /// <param name="p"><see cref="ISerializedCamera"/> The serialized camera</param>
        /// <param name="owner"><see cref="Editor"/> The editor owner calling this drawer</param>
        public static void Drawer_Projection(ISerializedCamera p, Editor owner)
        {
            // Most of this is replicated from CameraEditor.DrawProjection as we don't want to draw
            // it the same way it's done in non-SRP cameras. Unfortunately, because a lot of the
            // code is internal, we have to copy/paste some stuff from the editor code :(
            var projectionType = DrawerProjectionType(p, owner);

            if (p.baseCameraSettings.orthographic.hasMultipleDifferentValues)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                if (projectionType == ProjectionType.Orthographic)
                {
                    DrawerOrthographicType(p, owner);
                }
                else
                {
                    DrawerPerspectiveType(p, owner);
                }
            }
        }

        static void Drawer_FieldClippingPlanes(ISerializedCamera p, Editor owner)
        {
            CoreEditorUtils.DrawMultipleFields(
                Styles.clippingPlaneMultiFieldTitle,
                new[] { p.baseCameraSettings.nearClippingPlane, p.baseCameraSettings.farClippingPlane },
                new[] { Styles.nearPlaneContent, Styles.farPlaneContent });
        }
    }
}
