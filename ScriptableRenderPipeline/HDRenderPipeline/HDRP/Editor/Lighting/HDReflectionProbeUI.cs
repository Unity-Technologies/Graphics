using System;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    public partial class HDReflectionProbeUI : BaseUI<SerializedHDReflectionProbe>
    {
        const int k_AnimBoolSingleFieldCount = 4;
        static readonly int k_ReflectionProbeModeCount = Enum.GetValues(typeof(ReflectionProbeMode)).Length;
        static readonly int k_ReflectionInfluenceShapeCount = Enum.GetValues(typeof(ShapeType)).Length;
        static readonly int k_AnimBoolsCount = k_ReflectionProbeModeCount + k_ReflectionInfluenceShapeCount + k_AnimBoolSingleFieldCount;

        [Flags]
        public enum Operation
        {
            None = 0,
            FitVolumeToSurroundings = 1 << 0
        }
        Operation operations { get; set; }

        public Editor owner { get; set; }
        public bool HasOperation(Operation op) { return (operations & op) == op; }
        public void ClearOperation(Operation op) { operations &= ~op; }
        public void AddOperation(Operation op) { operations |= op; }

        public BoxBoundsHandle boxInfluenceHandle = new BoxBoundsHandle();
        public BoxBoundsHandle boxProjectionHandle = new BoxBoundsHandle();
        public BoxBoundsHandle boxBlendHandle = new BoxBoundsHandle();
        public BoxBoundsHandle boxBlendNormalHandle = new BoxBoundsHandle();
        public SphereBoundsHandle sphereInfluenceHandle = new SphereBoundsHandle();
        public SphereBoundsHandle sphereProjectionHandle = new SphereBoundsHandle();
        public SphereBoundsHandle sphereBlendHandle = new SphereBoundsHandle();
        public SphereBoundsHandle sphereBlendNormalHandle = new SphereBoundsHandle();
        public Matrix4x4 oldLocalSpace = Matrix4x4.identity;

        public AnimBool isSectionExpandedInfluenceVolume { get { return m_AnimBools[0]; } }
        public AnimBool isSectionExpandedSeparateProjection { get { return m_AnimBools[1]; } }
        public AnimBool isSectionExpandedCaptureSettings { get { return m_AnimBools[2]; } }
        public AnimBool isSectionExpandedAdditional { get { return m_AnimBools[3]; } }

        public bool HasAndClearOperation(Operation op)
        {
            var has = HasOperation(op);
            ClearOperation(op);
            return has;
        }

        public bool sceneViewEditing
        {
            get { return HDReflectionProbeEditor.IsReflectionProbeEditMode(EditMode.editMode) && EditMode.IsOwner(owner); }
        }

        public HDReflectionProbeUI()
            : base(k_AnimBoolsCount)
        {
            isSectionExpandedCaptureSettings.value = true;
            isSectionExpandedInfluenceVolume.value = true;
            isSectionExpandedAdditional.value = false;
        }

        public override void Update()
        {
            operations = 0;

            SetModeTarget(data.mode.intValue);
            SetShapeTarget(data.influenceShape.intValue);

            isSectionExpandedSeparateProjection.value = data.useSeparateProjectionVolume.boolValue;
            base.Update();
        }

        public AnimBool IsSectionExpandedMode(ReflectionProbeMode mode)
        {
            return m_AnimBools[k_AnimBoolSingleFieldCount + (int)mode];
        }

        public void SetModeTarget(int value)
        {
            for (var i = 0; i < k_ReflectionProbeModeCount; i++)
                GetReflectionProbeModeBool(i).target = i == value;
        }

        public AnimBool IsSectionExpandedShape(ShapeType value)
        {
            return m_AnimBools[k_AnimBoolSingleFieldCount + k_ReflectionProbeModeCount + (int)value];
        }

        public void SetShapeTarget(int value)
        {
            for (var i = 0; i < k_ReflectionInfluenceShapeCount; i++)
                GetReflectionInfluenceShapeBool(i).target = i == value;
        }

        internal void UpdateOldLocalSpace(ReflectionProbe target)
        {
            oldLocalSpace = HDReflectionProbeEditorUtility.GetLocalSpace(target);
        }

        AnimBool GetReflectionProbeModeBool(int i)
        {
            return m_AnimBools[k_AnimBoolSingleFieldCount + i];
        }

        AnimBool GetReflectionInfluenceShapeBool(int i)
        {
            return m_AnimBools[k_AnimBoolSingleFieldCount + k_ReflectionProbeModeCount  + i];
        }
    }
}
