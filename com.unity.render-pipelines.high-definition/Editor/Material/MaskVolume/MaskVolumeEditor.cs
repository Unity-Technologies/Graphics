using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditorInternal;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MaskVolume))]
    internal class MaskVolumeEditor : Editor
    {
        internal const EditMode.SceneViewEditMode k_EditShape = EditMode.SceneViewEditMode.ReflectionProbeBox;
        internal const EditMode.SceneViewEditMode k_EditBlend = EditMode.SceneViewEditMode.GridBox;
        internal const EditMode.SceneViewEditMode k_EditPaint = EditMode.SceneViewEditMode.GridPainting;

        static Dictionary<MaskVolume, HierarchicalBox> shapeBoxes = new Dictionary<MaskVolume, HierarchicalBox>();
        internal static Dictionary<MaskVolume, HierarchicalBox> blendBoxes = new Dictionary<MaskVolume, HierarchicalBox>();

        internal static Color32 BrushColor = new Color32(255, 0, 0, 255);
        internal static bool BrushApplyRed = true;
        internal static bool BrushApplyGreen = true;
        internal static bool BrushApplyBlue = true;
        internal static float BrushHardness = 1f;

        SerializedMaskVolume m_SerializedMaskVolume;

        internal static readonly MaskVolumeBrush Brush = new MaskVolumeBrush();

        protected void OnEnable()
        {
            m_SerializedMaskVolume = new SerializedMaskVolume(serializedObject);

            shapeBoxes.Clear();
            blendBoxes.Clear();
            for (int i = 0; i < targets.Length; ++i)
            {
                var shapeBox = shapeBoxes[targets[i] as MaskVolume] = new HierarchicalBox(MaskVolumeUI.Styles.k_GizmoColorBase, MaskVolumeUI.Styles.k_BaseHandlesColor);
                shapeBox.monoHandle = false;
                blendBoxes[targets[i] as MaskVolume] = new HierarchicalBox(MaskVolumeUI.Styles.k_GizmoColorBase, InfluenceVolumeUI.k_HandlesColor, parent: shapeBox);
            }

            Brush.OnApply += OnApplyBrush;
            Brush.OnStopApplying += OnStopApplyingBrush;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        void OnDisable()
        {
            Brush.StopIfApplying();

            Brush.OnApply -= OnApplyBrush;
            Brush.OnStopApplying -= OnStopApplyingBrush;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            MaskVolumeUI.Inspector.Draw(m_SerializedMaskVolume, this);

            m_SerializedMaskVolume.Apply();
        }

        static Vector3 CenterBlendLocalPosition(MaskVolume maskVolume)
        {
            Vector3 size = maskVolume.parameters.size;
            Vector3 posBlend = maskVolume.parameters.positiveFade;
            posBlend.x *= size.x;
            posBlend.y *= size.y;
            posBlend.z *= size.z;
            Vector3 negBlend = maskVolume.parameters.negativeFade;
            negBlend.x *= size.x;
            negBlend.y *= size.y;
            negBlend.z *= size.z;
            Vector3 localPosition = (negBlend - posBlend) * 0.5f;
            return localPosition;
        }

        static Vector3 BlendSize(MaskVolume maskVolume)
        {
            Vector3 size = maskVolume.parameters.size;
            Vector3 blendSize = (Vector3.one - maskVolume.parameters.positiveFade - maskVolume.parameters.negativeFade);
            blendSize.x *= size.x;
            blendSize.y *= size.y;
            blendSize.z *= size.z;
            return blendSize;
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        static void DrawGizmosSelected(MaskVolume maskVolume, GizmoType gizmoType)
        {
            using (new Handles.DrawingScope(Matrix4x4.TRS(maskVolume.transform.position, maskVolume.transform.rotation, Vector3.one)))
            {
                // Blend box
                if (!blendBoxes.TryGetValue(maskVolume, out HierarchicalBox blendBox)) { return; }
                blendBox.center = CenterBlendLocalPosition(maskVolume);
                blendBox.size = BlendSize(maskVolume);
                Color baseColor = maskVolume.parameters.debugColor;
                baseColor.a = 8/255f;
                blendBox.baseColor = baseColor;
                blendBox.DrawHull(EditMode.editMode == k_EditBlend);

                // Bounding box.
                if (!shapeBoxes.TryGetValue(maskVolume, out HierarchicalBox shapeBox)) { return; }
                shapeBox.center = Vector3.zero;
                shapeBox.size = maskVolume.parameters.size;
                shapeBox.DrawHull(EditMode.editMode == k_EditShape);
            }
        }

        protected void OnSceneGUI()
        {
            MaskVolume maskVolume = target as MaskVolume;

            /* if (Event.current.type == EventType.Layout)
                maskVolume.DrawSelectedMasks(); */

            if (!blendBoxes.TryGetValue(maskVolume, out HierarchicalBox blendBox)) { return; }
            if (!shapeBoxes.TryGetValue(maskVolume, out HierarchicalBox shapeBox)) { return; }

            if (EditMode.editMode != EditMode.SceneViewEditMode.GridPainting)
                Brush.StopIfApplying();

            switch (EditMode.editMode)
            {
                case k_EditBlend:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(maskVolume.transform.position, maskVolume.transform.rotation, Vector3.one)))
                    {
                        //contained must be initialized in all case
                        shapeBox.center = Vector3.zero;
                        shapeBox.size = maskVolume.parameters.size;

                        blendBox.monoHandle = !maskVolume.parameters.advancedFade;
                        blendBox.center = CenterBlendLocalPosition(maskVolume);
                        blendBox.size = BlendSize(maskVolume);
                        EditorGUI.BeginChangeCheck();
                        blendBox.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(maskVolume, "Change Mask Volume Blend");

                            //work in local space to compute the change on positiveFade and negativeFade
                            Vector3 newCenterBlendLocalPosition = blendBox.center;
                            Vector3 halfSize = blendBox.size * 0.5f;
                            Vector3 size = maskVolume.parameters.size;
                            Vector3 posFade = newCenterBlendLocalPosition + halfSize;
                            posFade.x = 0.5f - posFade.x / size.x;
                            posFade.y = 0.5f - posFade.y / size.y;
                            posFade.z = 0.5f - posFade.z / size.z;
                            Vector3 negFade = newCenterBlendLocalPosition - halfSize;
                            negFade.x = 0.5f + negFade.x / size.x;
                            negFade.y = 0.5f + negFade.y / size.y;
                            negFade.z = 0.5f + negFade.z / size.z;
                            maskVolume.parameters.positiveFade = posFade;
                            maskVolume.parameters.negativeFade = negFade;
                        }
                    }
                    break;
                case k_EditShape:
                    //important: if the origin of the handle's space move along the handle,
                    //handles displacement will appears as moving two time faster.
                    using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, maskVolume.transform.rotation, Vector3.one)))
                    {
                        //contained must be initialized in all case
                        shapeBox.center = Quaternion.Inverse(maskVolume.transform.rotation) * maskVolume.transform.position;
                        shapeBox.size = maskVolume.parameters.size;

                        shapeBox.monoHandle = !maskVolume.parameters.advancedFade;
                        EditorGUI.BeginChangeCheck();
                        shapeBox.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(new Object[] { maskVolume, maskVolume.transform }, "Change Mask Volume Bounding Box");

                            maskVolume.parameters.size = shapeBox.size;

                            Vector3 delta = maskVolume.transform.rotation * shapeBox.center - maskVolume.transform.position;
                            maskVolume.transform.position += delta;
                        }
                    }
                    break;
                case k_EditPaint:
                    var sceneView = SceneView.currentDrawingSceneView;
                    Brush.OnSceneGUI(sceneView);
                    sceneView.Repaint();
                    break;
            }
        }

        bool m_ApplyingBrush;

        void OnApplyBrush(Vector3 position)
        {
            // TODO: Multi-editing.
            var maskVolume = (MaskVolume)target;

            var maskVolumeAsset = (MaskVolumeAsset)m_SerializedMaskVolume.maskVolumeAsset.objectReferenceValue;
            if (maskVolumeAsset == null)
                return;

            if (!m_ApplyingBrush)
            {
                Undo.RegisterCompleteObjectUndo(maskVolumeAsset, "Paint Volume");
                m_ApplyingBrush = true;
            }

            var parameters = maskVolume.parameters;
            var size = parameters.size;
            var voxelSize = new Vector3(size.x / maskVolumeAsset.resolutionX, size.y / maskVolumeAsset.resolutionY, size.z / maskVolumeAsset.resolutionZ);
            var firstVoxelLocalPosition = size * -0.5f + voxelSize * 0.5f;
            var maskTransform = maskVolume.transform;
            var localToWorld = Matrix4x4.TRS(maskTransform.position, maskTransform.rotation, Vector3.one);
            var worldToLocal = localToWorld.inverse;
            var localBrushPosition = worldToLocal.MultiplyPoint3x4(position);

            var minAffectedLocalPosition = localBrushPosition - firstVoxelLocalPosition - new Vector3(Brush.Radius, Brush.Radius, Brush.Radius);
            var maxAffectedLocalPosition = localBrushPosition - firstVoxelLocalPosition + new Vector3(Brush.Radius, Brush.Radius, Brush.Radius);

            var minX = Mathf.Max(Mathf.RoundToInt(minAffectedLocalPosition.x / voxelSize.x), 0);
            var minY = Mathf.Max(Mathf.RoundToInt(minAffectedLocalPosition.y / voxelSize.y), 0);
            var minZ = Mathf.Max(Mathf.RoundToInt(minAffectedLocalPosition.z / voxelSize.z), 0);
            var maxX = Mathf.Min(Mathf.RoundToInt(maxAffectedLocalPosition.x / voxelSize.x), maskVolumeAsset.resolutionX - 1);
            var maxY = Mathf.Min(Mathf.RoundToInt(maxAffectedLocalPosition.y / voxelSize.y), maskVolumeAsset.resolutionY - 1);
            var maxZ = Mathf.Min(Mathf.RoundToInt(maxAffectedLocalPosition.z / voxelSize.z), maskVolumeAsset.resolutionZ - 1);

            var dataSHL0 = maskVolumeAsset.payload.dataSHL0;
            var strideSHL0 = MaskVolumePayload.GetDataSHL0Stride();

            for (int z = minZ; z <= maxZ; z++)
            {
                var yStart = z * maskVolumeAsset.resolutionY;
                for (int y = minY; y <= maxY; y++)
                {
                    var xStart = (yStart + y) * maskVolumeAsset.resolutionX;
                    for (int x = minX; x <= maxX; x++)
                    {
                        var i = xStart + x;

                        var point = firstVoxelLocalPosition + new Vector3(voxelSize.x * x, voxelSize.y * y, voxelSize.z * z);

                        var toBrush = localBrushPosition - point;

                        var longestComponent = Mathf.Max(Mathf.Max(Mathf.Abs(toBrush.x), Mathf.Abs(toBrush.y)), Mathf.Abs(toBrush.z));
                        var halfVoxelLength = Vector3.Scale(toBrush / longestComponent, voxelSize).magnitude * 0.5f;

                        var outerRadius = Brush.Radius + halfVoxelLength;
                        var innerRadius = Brush.Radius - halfVoxelLength;
                        if (innerRadius > 0f)
                            innerRadius *= BrushHardness;

                        var distanceToBrush = toBrush.magnitude;
                        var opacity = MaskVolumePayload.ToUNormByte(MaskVolumePayload.FromUNormByte(BrushColor.a) * (outerRadius - distanceToBrush) / (outerRadius - innerRadius));
                        if (opacity > 0)
                        {
                            var indexDataBaseSHL0 = i * strideSHL0;
                            if (BrushApplyRed)
                                dataSHL0[indexDataBaseSHL0 + 0] = ApplyBrushChannel(dataSHL0[indexDataBaseSHL0 + 0], BrushColor.r, opacity); // shAr.w
                            if (BrushApplyGreen)
                                dataSHL0[indexDataBaseSHL0 + 1] = ApplyBrushChannel(dataSHL0[indexDataBaseSHL0 + 1], BrushColor.g, opacity); // shAg.w
                            if (BrushApplyBlue)
                                dataSHL0[indexDataBaseSHL0 + 2] = ApplyBrushChannel(dataSHL0[indexDataBaseSHL0 + 2], BrushColor.b, opacity); // shAb.w
                        }
                    }
                }
            }

            maskVolume.dataUpdated = true;
        }

        static byte ApplyBrushChannel(byte value, byte targetValue, byte opacity)
        {
            var delta = targetValue - value;
            if (delta < 0)
            {
                if (delta < -opacity)
                    delta = -opacity;
            }
            else
            {
                if (delta > opacity)
                    delta = opacity;
            }
            return (byte)(value + delta);
        }

        void OnStopApplyingBrush()
        {
            m_ApplyingBrush = false;
        }

        void OnUndoRedoPerformed()
        {
            ((MaskVolume)target).dataUpdated = true;
            // TODO: Figure out where to mark it as potentially changed when undo/redo happens without an enabled editor for changed volume.
        }
    }
}
