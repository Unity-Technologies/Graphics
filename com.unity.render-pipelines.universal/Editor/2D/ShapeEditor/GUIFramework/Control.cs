using System;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.Path2D.GUIFramework
{
    internal abstract class Control
    {
        private string m_Name;
        private int m_NameHashCode;
        private int m_ID;
        private LayoutData m_LayoutData;
        private int m_ActionID = -1;
        private LayoutData m_HotLayoutData;

        public string name
        {
            get { return m_Name; }
        }

        public int ID
        {
            get { return m_ID; }
        }

        public int actionID
        {
            get { return m_ActionID; }
        }

        public LayoutData layoutData
        {
            get { return m_LayoutData; }
            set { m_LayoutData = value; }
        }

        public LayoutData hotLayoutData
        {
            get { return m_HotLayoutData; }
        }

        public Control(string name)
        {
            m_Name = name;
            m_NameHashCode = name.GetHashCode();
        }

        public void GetControl(IGUIState guiState)
        {
            m_ID = guiState.GetControlID(m_NameHashCode, FocusType.Passive);
        }

        internal void SetActionID(int actionID)
        {
            m_ActionID = actionID;
            m_HotLayoutData = m_LayoutData;
        }

        public void BeginLayout(IGUIState guiState)
        {
            Debug.Assert(guiState.eventType == EventType.Layout);

            m_LayoutData = OnBeginLayout(LayoutData.zero, guiState);
        }

        public void Layout(IGUIState guiState)
        {
            Debug.Assert(guiState.eventType == EventType.Layout);

            for (var i = 0; i < GetCount(); ++i)
            {
                if (guiState.hotControl == actionID && hotLayoutData.index == i)
                    continue;

                var layoutData = new LayoutData()
                {
                    index = i,
                    position = GetPosition(guiState, i),
                    distance = GetDistance(guiState, i),
                    forward = GetForward(guiState, i),
                    up = GetUp(guiState, i),
                    right = GetRight(guiState, i),
                    userData = GetUserData(guiState, i)
                };

                m_LayoutData = LayoutData.Nearest(m_LayoutData, layoutData);
            }
        }

        public void EndLayout(IGUIState guiState)
        {
            Debug.Assert(guiState.eventType == EventType.Layout);

            OnEndLayout(guiState);
        }

        public void Repaint(IGUIState guiState)
        {
            for (var i = 0; i < GetCount(); ++i)
                OnRepaint(guiState, i);
        }

        protected virtual LayoutData OnBeginLayout(LayoutData data, IGUIState guiState)
        {
            return data;
        }

        protected virtual void OnEndLayout(IGUIState guiState)
        {
        }

        protected virtual void OnRepaint(IGUIState guiState, int index)
        {
        }

        protected virtual int GetCount()
        {
            return 1;
        }

        protected virtual Vector3 GetPosition(IGUIState guiState, int index)
        {
            return Vector3.zero;
        }

        protected virtual Vector3 GetForward(IGUIState guiState, int index)
        {
            return Vector3.forward;
        }

        protected virtual Vector3 GetUp(IGUIState guiState, int index)
        {
            return Vector3.up;
        }

        protected virtual Vector3 GetRight(IGUIState guiState, int index)
        {
            return Vector3.right;
        }

        protected virtual float GetDistance(IGUIState guiState, int index)
        {
            return layoutData.distance;
        }

        protected virtual object GetUserData(IGUIState guiState, int index)
        {
            return null;
        }
    }
}
