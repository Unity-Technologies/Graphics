using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Channels/Splat Node")]
    class SplatNode : Function1Input
    {
        [SerializeField]
        private int m_SwizzleChannel;

        public override void Init()
        {
            base.Init();
            name = "SplatNode";
            m_SwizzleChannel = 0;
        }

        private string GetChannelFromConfiguration()
        {
            switch (m_SwizzleChannel)
            {
                case 0:
                    return "xxxx";
                case 1:
                    return "yyyy";
                case 2:
                    return "zzzz";
                default:
                    return "wwww";
            }
        }

        public override void NodeUI(GraphGUI host)
        {
            base.NodeUI();
            string[] values = {"x", "y", "z", "w"};
            EditorGUI.BeginChangeCheck();
            m_SwizzleChannel = EditorGUILayout.Popup("Channel", m_SwizzleChannel, values);
            if (EditorGUI.EndChangeCheck())
                RegeneratePreviewShaders();
        }

        protected override string GetFunctionName()
        {
            return "";
        }

        protected override string GetFunctionCallBody(string inputValue)
        {
            return inputValue + "." + GetChannelFromConfiguration();
        }
    }
}
