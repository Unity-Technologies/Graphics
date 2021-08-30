using System;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class ShaderInputViewModel : ISGViewModel
    {
        public ShaderInput model { get; set; }

        public VisualElement parentView { get; set; }

        internal bool isSubGraph { get; set; }
        internal bool isInputExposed { get; set; }

        internal string inputName { get; set; }

        internal string inputTypeName { get; set; }

        internal Action<IGraphDataAction> requestModelChangeAction { get; set; }

        public void ResetViewModelData()
        {
            isSubGraph = false;
            isInputExposed = false;
            inputName = String.Empty;
            inputTypeName = String.Empty;
            requestModelChangeAction = null;
        }
    }
}
