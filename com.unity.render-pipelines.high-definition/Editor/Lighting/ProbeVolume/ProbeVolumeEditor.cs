using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditorInternal;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProbeVolume))]
    internal class ProbeVolumeEditor : Editor
    {
        internal const EditMode.SceneViewEditMode k_EditShape = EditMode.SceneViewEditMode.ReflectionProbeBox;
        internal const EditMode.SceneViewEditMode k_EditBlend = EditMode.SceneViewEditMode.GridBox;
        internal const EditMode.SceneViewEditMode k_EditPaint = EditMode.SceneViewEditMode.GridPainting;

        static Dictionary<ProbeVolume, HierarchicalBox> shapeBoxes = new Dictionary<ProbeVolume, HierarchicalBox>();
        internal static Dictionary<ProbeVolume, HierarchicalBox> blendBoxes = new Dictionary<ProbeVolume, HierarchicalBox>();

        internal static Color BrushColor = Color.red;
        internal static float BrushHardness = 1f;

        SerializedProbeVolume m_SerializedProbeVolume;

        internal static readonly ProbeVolumeBrush Brush = new ProbeVolumeBrush();

        protected void OnEnable()
        {
            m_SerializedProbeVolume = new SerializedProbeVolume(serializedObject);

            shapeBoxes.Clear();
            blendBoxes.Clear();
            for (int i = 0; i < targets.Length; ++i)
            {
                var shapeBox = shapeBoxes[targets[i] as ProbeVolume] = new HierarchicalBox(ProbeVolumeUI.Styles.k_GizmoColorBase, ProbeVolumeUI.Styles.k_BaseHandlesColor);
                shapeBox.monoHandle = false;
                blendBoxes[targets[i] as ProbeVolume] = new HierarchicalBox(ProbeVolumeUI.Styles.k_GizmoColorBase, InfluenceVolumeUI.k_HandlesColor, parent: shapeBox);
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

            ProbeVolumeUI.Inspector.Draw(m_SerializedProbeVolume, this);

            m_SerializedProbeVolume.Apply();
        }

        static Vector3 CenterBlendLocalPosition(ProbeVolume probeVolume)
        {
            Vector3 size = probeVolume.parameters.size;
            Vector3 posBlend = probeVolume.parameters.positiveFade;
            posBlend.x *= size.x;
            posBlend.y *= size.y;
            posBlend.z *= size.z;
            Vector3 negBlend = probeVolume.parameters.negativeFade;
            negBlend.x *= size.x;
            negBlend.y *= size.y;
            negBlend.z *= size.z;
            Vector3 localPosition = (negBlend - posBlend) * 0.5f;
            return localPosition;
        }

        static Vector3 BlendSize(ProbeVolume probeVolume)
        {
            Vector3 size = probeVolume.parameters.size;
            Vector3 blendSize = (Vector3.one - probeVolume.parameters.positiveFade - probeVolume.parameters.negativeFade);
            blendSize.x *= size.x;
            blendSize.y *= size.y;
            blendSize.z *= size.z;
            return blendSize;
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        static void DrawGizmosSelected(ProbeVolume probeVolume, GizmoType gizmoType)
        {
            using (new Handles.DrawingScope(Matrix4x4.TRS(probeVolume.transform.position, probeVolume.transform.rotation, Vector3.one)))
            {
                // Blend box
                if (!blendBoxes.TryGetValue(probeVolume, out HierarchicalBox blendBox)) { return; }
                blendBox.center = CenterBlendLocalPosition(probeVolume);
                blendBox.size = BlendSize(probeVolume);
                Color baseColor = probeVolume.parameters.debugColor;
                baseColor.a = 8/255f;
                blendBox.baseColor = baseColor;
                blendBox.DrawHull(EditMode.editMode == k_EditBlend);

                // Bounding box.
                if (!shapeBoxes.TryGetValue(probeVolume, out HierarchicalBox shapeBox)) { return; }
                shapeBox.center = Vector3.zero;
                shapeBox.size = probeVolume.parameters.size;
                shapeBox.DrawHull(EditMode.editMode == k_EditShape);
            }
        }

        protected void OnSceneGUI()
        {
            ProbeVolume probeVolume = target as ProbeVolume;

            if (Event.current.type == EventType.Layout)
                probeVolume.DrawSelectedProbes();

            if (!blendBoxes.TryGetValue(probeVolume, out HierarchicalBox blendBox)) { return; }
            if (!shapeBoxes.TryGetValue(probeVolume, out HierarchicalBox shapeBox)) { return; }

            if (EditMode.editMode != EditMode.SceneViewEditMode.GridPainting)
                Brush.StopIfApplying();

            switch (EditMode.editMode)
            {
                case k_EditBlend:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(probeVolume.transform.position, probeVolume.transform.rotation, Vector3.one)))
                    {
                        //contained must be initialized in all case
                        shapeBox.center = Vector3.zero;
                        shapeBox.size = probeVolume.parameters.size;

                        blendBox.monoHandle = !probeVolume.parameters.advancedFade;
                        blendBox.center = CenterBlendLocalPosition(probeVolume);
                        blendBox.size = BlendSize(probeVolume);
                        EditorGUI.BeginChangeCheck();
                        blendBox.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(probeVolume, "Change Probe Volume Blend");

                            //work in local space to compute the change on positiveFade and negativeFade
                            Vector3 newCenterBlendLocalPosition = blendBox.center;
                            Vector3 halfSize = blendBox.size * 0.5f;
                            Vector3 size = probeVolume.parameters.size;
                            Vector3 posFade = newCenterBlendLocalPosition + halfSize;
                            posFade.x = 0.5f - posFade.x / size.x;
                            posFade.y = 0.5f - posFade.y / size.y;
                            posFade.z = 0.5f - posFade.z / size.z;
                            Vector3 negFade = newCenterBlendLocalPosition - halfSize;
                            negFade.x = 0.5f + negFade.x / size.x;
                            negFade.y = 0.5f + negFade.y / size.y;
                            negFade.z = 0.5f + negFade.z / size.z;
                            probeVolume.parameters.positiveFade = posFade;
                            probeVolume.parameters.negativeFade = negFade;
                        }
                    }
                    break;
                case k_EditShape:
                    //important: if the origin of the handle's space move along the handle,
                    //handles displacement will appears as moving two time faster.
                    using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, probeVolume.transform.rotation, Vector3.one)))
                    {
                        //contained must be initialized in all case
                        shapeBox.center = Quaternion.Inverse(probeVolume.transform.rotation) * probeVolume.transform.position;
                        shapeBox.size = probeVolume.parameters.size;

                        shapeBox.monoHandle = !probeVolume.parameters.advancedFade;
                        EditorGUI.BeginChangeCheck();
                        shapeBox.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(new Object[] { probeVolume, probeVolume.transform }, "Change Probe Volume Bounding Box");

                            probeVolume.parameters.size = shapeBox.size;

                            Vector3 delta = probeVolume.transform.rotation * shapeBox.center - probeVolume.transform.position;
                            probeVolume.transform.position += delta;
                        }
                    }
                    break;
                case k_EditPaint:
                    Brush.OnSceneGUI(SceneView.currentDrawingSceneView);
                    break;
            }
        }

        bool m_ApplyingBrush;

        void OnApplyBrush(Vector3 position)
        {
            // TODO: Multi-editing.
            var probeVolume = (ProbeVolume)target;

            var probeVolumeAsset = (ProbeVolumeAsset)m_SerializedProbeVolume.probeVolumeAsset.objectReferenceValue;
            if (probeVolumeAsset == null)
                return;

            if (!m_ApplyingBrush)
            {
                Undo.RegisterCompleteObjectUndo(probeVolumeAsset, "Paint Volume");
                m_ApplyingBrush = true;
            }

            var parameters = probeVolume.parameters;
            var size = parameters.size;
            var voxelSize = new Vector3(size.x / probeVolumeAsset.resolutionX, size.y / probeVolumeAsset.resolutionY, size.z / probeVolumeAsset.resolutionZ);
            var firstVoxelLocalPosition = size * -0.5f + voxelSize * 0.5f;
            var probeTransform = probeVolume.transform;
            var localToWorld = Matrix4x4.TRS(probeTransform.position, probeTransform.rotation, Vector3.one);
            var worldToLocal = localToWorld.inverse;
            var localBrushPosition = worldToLocal.MultiplyPoint3x4(position);

            var minAffectedLocalPosition = localBrushPosition - firstVoxelLocalPosition - new Vector3(Brush.Radius, Brush.Radius, Brush.Radius);
            var maxAffectedLocalPosition = localBrushPosition - firstVoxelLocalPosition + new Vector3(Brush.Radius, Brush.Radius, Brush.Radius);

            var minX = Mathf.Max(Mathf.RoundToInt(minAffectedLocalPosition.x / voxelSize.x), 0);
            var minY = Mathf.Max(Mathf.RoundToInt(minAffectedLocalPosition.y / voxelSize.y), 0);
            var minZ = Mathf.Max(Mathf.RoundToInt(minAffectedLocalPosition.z / voxelSize.z), 0);
            var maxX = Mathf.Min(Mathf.RoundToInt(maxAffectedLocalPosition.x / voxelSize.x), probeVolumeAsset.resolutionX - 1);
            var maxY = Mathf.Min(Mathf.RoundToInt(maxAffectedLocalPosition.y / voxelSize.y), probeVolumeAsset.resolutionY - 1);
            var maxZ = Mathf.Min(Mathf.RoundToInt(maxAffectedLocalPosition.z / voxelSize.z), probeVolumeAsset.resolutionZ - 1);

            var dataSHL01 = probeVolumeAsset.payload.dataSHL01;
            var strideSHL01 = ProbeVolumePayload.GetDataSHL01Stride();

            for (int z = minZ; z <= maxZ; z++)
            {
                var yStart = z * probeVolumeAsset.resolutionY;
                for (int y = minY; y <= maxY; y++)
                {
                    var xStart = (yStart + y) * probeVolumeAsset.resolutionX;
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
                        var opacity = BrushColor.a * Mathf.Clamp01((outerRadius - distanceToBrush) / (outerRadius - innerRadius));
                        if (opacity > 0f)
                        {
                            var indexDataBaseSHL01 = i * strideSHL01;
                            dataSHL01[indexDataBaseSHL01 + 0] = Mathf.Lerp(dataSHL01[indexDataBaseSHL01 + 0], BrushColor.r, opacity); // shAr.w
                            dataSHL01[indexDataBaseSHL01 + 1] = Mathf.Lerp(dataSHL01[indexDataBaseSHL01 + 1], BrushColor.g, opacity); // shAg.w
                            dataSHL01[indexDataBaseSHL01 + 2] = Mathf.Lerp(dataSHL01[indexDataBaseSHL01 + 2], BrushColor.b, opacity); // shAb.w
                        }
                    }
                }
            }

            probeVolume.dataUpdated = true;
        }

        void OnStopApplyingBrush()
        {
            m_ApplyingBrush = false;
        }

        void OnUndoRedoPerformed()
        {
            ((ProbeVolume)target).dataUpdated = true;
            // TODO: Figure out where to mark it as potentially changed when undo/redo happens without an enabled editor for changed volume.
        }
    }
}
