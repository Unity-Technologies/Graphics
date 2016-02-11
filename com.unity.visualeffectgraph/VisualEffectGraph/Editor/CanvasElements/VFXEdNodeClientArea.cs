using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdNodeClientArea : CanvasElement
    {
        private string m_Title;

        public VFXEdNodeBlockContainer NodeBlockContainer
        { get { return m_NodeBlockContainer; } }
        private VFXEdNodeBlockContainer m_NodeBlockContainer;

        public VFXEdNodeClientArea(Rect clientRect, VFXEdDataSource dataSource, string name)
        {
            translation = VFXEditorMetrics.NodeClientAreaPosition;
            m_Title = name;
            translation = clientRect.position;
            scale = new Vector2(clientRect.width, clientRect.height);
            m_Caps = Capabilities.Normal;
            m_NodeBlockContainer = new VFXEdNodeBlockContainer(clientRect.size, dataSource, name);
            AddChild(m_NodeBlockContainer);
        }


        public override void Layout()
        {
            base.Layout();
            scale = new Vector2(scale.x, m_NodeBlockContainer.scale.y + 41);
        }


        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Rect r = GetDrawableRect();

            if (selected)
                GUI.Box(r, "", VFXEditor.styles.NodeSelected);
            else
                GUI.Box(r, "", VFXEditor.styles.Node);

            GUI.Label(new Rect(0, r.y, r.width, 24), m_Title, VFXEditor.styles.NodeTitle);

            base.Render(parentRect, canvas);
        }

    }
}

