using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Drawing
{
    [Serializable]
    public class WinowDockingLayout : MonoBehaviour
    {
        [SerializeField]
        bool m_DockingLeft;

        public bool dockingLeft
        {
            get { return m_DockingLeft; }
            set { m_DockingLeft = value; }
        }

        [SerializeField]
        bool m_DockingTop;

        public bool dockingTop
        {
            get { return m_DockingTop; }
            set { m_DockingTop = value; }
        }

        [SerializeField]
        float m_VerticalOffset;

        public float verticalOffset
        {
            get { return m_VerticalOffset; }
            set { m_VerticalOffset = value; }
        }

        [SerializeField]
        float m_HorizontalOffset;

        public float horizontalOffset
        {
            get { return m_HorizontalOffset; }
            set { m_HorizontalOffset = value; }
        }
    }
}
