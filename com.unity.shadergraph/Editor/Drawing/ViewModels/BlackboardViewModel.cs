using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardViewModel : ISGViewModel
    {
        public GraphData Model { get; set; }
        public string Subtitle { get; set; }

        public void ConstructFromModel(GraphData graphData)
        {
        }
    }
}
