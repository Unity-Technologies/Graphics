using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    internal class RedirectNode : Node
    {
        public RedirectNode()
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/RedirectNode"));
        }
    }
}
