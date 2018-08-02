using System;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;
using UnityEditor.Experimental.Rendering.HDPipeline;
using UnityEngine.Events;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal partial class HDReflectionProbeUI : HDProbeUI
    {
        const int k_AnimBoolSingleFieldCount = 4;
        static readonly int k_ReflectionProbeModeCount = Enum.GetValues(typeof(ReflectionProbeMode)).Length;
        static readonly int k_ReflectionInfluenceShapeCount = Enum.GetValues(typeof(InfluenceShape)).Length;
        static readonly int k_AnimBoolsCount = k_ReflectionProbeModeCount + k_ReflectionInfluenceShapeCount + k_AnimBoolSingleFieldCount;


        internal HDReflectionProbeUI()
        {
            toolBars = new[] { ToolBar.InfluenceShape | ToolBar.Blend | ToolBar.NormalBlend, ToolBar.CapturePosition };
        }

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

        public Matrix4x4 oldLocalSpace = Matrix4x4.identity;

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

        public override void Update()
        {
            operations = 0;

            SetModeTarget(data.mode.hasMultipleDifferentValues ? -1 : data.mode.intValue);
            SetShapeTarget(data.influenceVolume.shape.hasMultipleDifferentValues ? -1 : data.influenceVolume.shape.intValue);

            influenceVolume.Update();
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

        public AnimBool IsSectionExpandedShape(InfluenceShape value)
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
