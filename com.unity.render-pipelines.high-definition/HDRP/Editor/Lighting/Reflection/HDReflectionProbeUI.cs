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
            get { return HDProbeUI.IsProbeEditMode(EditMode.editMode) && EditMode.IsOwner(owner); }
        }

        public override void Update()
        {
            operations = 0;
            base.Update();
        }

        internal void UpdateOldLocalSpace(ReflectionProbe target)
        {
            oldLocalSpace = HDReflectionProbeEditorUtility.GetLocalSpace(target);
        }
    }
}
