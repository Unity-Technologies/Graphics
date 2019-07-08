using System;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.Universal.Path2D.GUIFramework
{
    internal abstract class DefaultControl : Control
    {
        public static readonly float kPickDistance = 5f;
        
        public DefaultControl(string name) : base(name)
        {
        }

        protected override LayoutData OnBeginLayout(LayoutData data, IGUIState guiState)
        {
            data.distance = kPickDistance;
            return data;
        }
    }
}
