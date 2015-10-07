using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEditor.Graphs.Material
{
    [Title("Channels/Swizzle Node")]
    class SwizzleNode : Function2Input
    {
        [SerializeField]
        private int[] m_SwizzleChannel = new int[4];

        public override void Init()
        {
            base.Init();
            name = "SwizzleNode";
        }

        public override void NodeUI(GraphGUI host)
        {
            base.NodeUI(host);
            string[] channelNames = { "X", "Y", "Z", "W" };
            string[] values = { "0", "1", "Input1.x", "Input1.y", "Input1.z", "Input1.w", "Input2.x", "Input2.y", "Input2.z", "Input2.w" };
            EditorGUI.BeginChangeCheck();
            for (int n = 0; n < 4; n++)
                m_SwizzleChannel[n] = EditorGUILayout.Popup(channelNames[n] + "=", m_SwizzleChannel[n], values);
            if (EditorGUI.EndChangeCheck())
                RegeneratePreviewShaders();
        }

        protected override string GetFunctionName()
        {
            return "";
        }

        protected override string GetFunctionCallBody(string inputValue1, string inputValue2)
        {
            string[] inputValues = { inputValue1, inputValue2 };
            string[] channelNames = { "x", "y", "z", "w" };
            string s = precision + "4(";
            for (int n = 0; n < 4; n++)
            {
                int c = m_SwizzleChannel[n];
                if (c < 2)
                    s += c;
                else
                {
                    c -= 2;
                    s += inputValues[c >> 2] + "." + channelNames[c & 3];
                }
                s += (n == 3) ? ")" : ",";
            }
            return s;
        }
    }
}
