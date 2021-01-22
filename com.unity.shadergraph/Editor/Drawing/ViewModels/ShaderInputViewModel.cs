using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class ShaderInputViewModel : ISGViewModel
    {
        public GraphData Model { get; set; }

        public VisualElement ParentView { get; set; }

        bool IsInputExposed { get; set; }

        string InputName { get; set; }

        string InputTypeName { get; set; }

        public void Reset()
        {
        }
    }
}
