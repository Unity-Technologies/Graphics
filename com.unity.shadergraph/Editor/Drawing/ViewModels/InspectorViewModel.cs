using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class InspectorViewModel : ISGViewModel
    {
        public GraphData Model { get; set; }

        public VisualElement ParentView { get; set; }

        public void Reset()
        {
        }
    }
}
