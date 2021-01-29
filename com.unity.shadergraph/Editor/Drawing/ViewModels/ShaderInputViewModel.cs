using System;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class ShaderInputViewModel : ISGViewModel
    {
        public ShaderInput Model { get; set; }

        public VisualElement ParentView { get; set; }

        internal bool IsSubGraph { get; set; }
        internal bool IsInputExposed { get; set; }

        internal string InputName { get; set; }

        internal string InputTypeName { get; set; }

        internal Action<IGraphDataAction> RequestModelChangeAction { get; set; }
        public void Reset()
        {
        }
    }
}
