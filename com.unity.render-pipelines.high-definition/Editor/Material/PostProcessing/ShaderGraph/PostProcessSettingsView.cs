using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline.Drawing
{
    class PostProcessSettingsView : VisualElement
    {
        PostProcessMasterNode m_Node;

        Label CreateLabel(string text, int indentLevel)
        {
            string label = "";
            for (var i = 0; i < indentLevel; i++)
            {
                label += "    ";
            }
            return new Label(label + text);
        }

        public PostProcessSettingsView(PostProcessMasterNode node)
        {
            m_Node = node;
            PropertySheet ps = new PropertySheet();                       
            Add(ps);
        }     
    }
}
