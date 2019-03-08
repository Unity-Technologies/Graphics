using System;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.LWRP.GUIFramework
{
    internal abstract class DefaultControl : Control
    {
        private readonly float kPickDistance = 5f;
        
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
