using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditorInternal;
using UnityEngineInternal;

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
            var texelSize = new Vector3(size.x / probeVolumeAsset.resolutionX, size.y / probeVolumeAsset.resolutionY, size.z / probeVolumeAsset.resolutionZ);
            var firstTexelLocalPosition = size * -0.5f + texelSize * 0.5f;
            var probeTransform = probeVolume.transform;
            var localToWorld = Matrix4x4.TRS(probeTransform.position, probeTransform.rotation, Vector3.one);
            var worldToLocal = localToWorld.inverse;
            var localBrushPosition = worldToLocal.MultiplyPoint3x4(position);

            for (int z = 0, i = 0; z < probeVolumeAsset.resolutionZ; z++)
            for (int y = 0; y < probeVolumeAsset.resolutionY; y++)
            for (int x = 0; x < probeVolumeAsset.resolutionX; x++, i++)
            {
                var point = firstTexelLocalPosition + new Vector3(texelSize.x * x, texelSize.y * y, texelSize.z * z);

                var toBrush = localBrushPosition - point;
                // TODO: Use toBrush clamped by unit cube instead of normalized to support diagonal better.
                var halfTexelLength = Vector3.Scale(toBrush.normalized, texelSize).magnitude * 0.5f;

                var outerRadius = Brush.Radius + halfTexelLength;
                var innerRadius = (Brush.Radius - halfTexelLength) * BrushHardness;

                var distanceToBrush = toBrush.magnitude;
                var opacity = BrushColor.a * Mathf.Clamp01((outerRadius - distanceToBrush) / (outerRadius - innerRadius));
                if (opacity > 0f)
                {
                    var sh = new SphericalHarmonicsL1();
                    ProbeVolumePayload.GetSphericalHarmonicsL1FromIndex(ref sh, ref probeVolumeAsset.payload, i);

                    sh.shAr.w = Mathf.Lerp(sh.shAr.w, BrushColor.r, opacity);
                    sh.shAg.w = Mathf.Lerp(sh.shAg.w, BrushColor.g, opacity);
                    sh.shAb.w = Mathf.Lerp(sh.shAb.w, BrushColor.b, opacity);

                    ProbeVolumePayload.SetSphericalHarmonicsL1FromIndex(ref probeVolumeAsset.payload, sh, i);
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
