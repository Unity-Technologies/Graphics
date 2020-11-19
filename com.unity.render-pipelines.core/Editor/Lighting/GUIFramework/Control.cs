using System;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// Interface for control
    /// </summary>
    public abstract class Control
    {
        private string m_Name;
        private int m_NameHashCode;
        private int m_ID;
        private LayoutData m_LayoutData;
        private int m_ActionID = -1;
        private LayoutData m_HotLayoutData;
        private bool m_Enabled;

        /// <summary>
        /// Name
        /// </summary>
        public string name
        {
            get { return m_Name; }
        }

        /// <summary>
        /// Control ID
        /// </summary>
        public int ID
        {
            get { return m_ID; }
        }

        /// <summary>
        /// Action ID
        /// </summary>
        public int actionID
        {
            get { return m_ActionID; }
        }

        /// <summary>
        /// Layout Data
        /// </summary>
        public LayoutData layoutData
        {
            get { return m_LayoutData; }
            set { m_LayoutData = value; }
        }

        /// <summary>
        /// Hot layout Data
        /// </summary>
        public LayoutData hotLayoutData
        {
            get { return m_HotLayoutData; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name of the control</param>
        public Control(string name)
        {
            m_Name = name;
            m_NameHashCode = name.GetHashCode();
        }

        /// <summary>
        /// Get the control
        /// </summary>
        /// <param name="guiState">The </param>
        public void GetControl(IGUIState guiState)
        {
            if (guiState.eventType == EventType.Layout)
                m_Enabled = GetEnabled(guiState);

            m_ID = -1;

            if (m_Enabled)
                m_ID = guiState.GetControlID(m_NameHashCode, FocusType.Passive);
        }

        internal void SetActionID(int actionID)
        {
            m_ActionID = actionID;
            m_HotLayoutData = m_LayoutData;
        }

        /// <summary>
        /// Begin Layout, should be followed at the end by an EndLayout
        /// </summary>
        /// <param name="guiState">the gui state</param>
        public void BeginLayout(IGUIState guiState)
        {
            Debug.Assert(guiState.eventType == EventType.Layout);

            if (m_Enabled)
                m_LayoutData = OnBeginLayout(LayoutData.zero, guiState);
        }

        /// <summary>
        /// Layout
        /// </summary>
        /// <param name="guiState">the gui state</param>
        public void Layout(IGUIState guiState)
        {
            Debug.Assert(guiState.eventType == EventType.Layout);

            if (m_Enabled)
            {
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
            else
            {
                m_LayoutData = LayoutData.zero;
            }
        }

        /// <summary>
        /// End Layout, must be called to end an BeginLayout
        /// </summary>
        /// <param name="guiState">the gui state</param>
        public void EndLayout(IGUIState guiState)
        {
            Debug.Assert(guiState.eventType == EventType.Layout);

            if (m_Enabled)
                OnEndLayout(guiState);
        }

        /// <summary>
        /// Repaint
        /// </summary>
        /// <param name="guiState">the gui state</param>
        public void Repaint(IGUIState guiState)
        {
            if (m_Enabled)
            {
                for (var i = 0; i < GetCount(); ++i)
                    OnRepaint(guiState, i);
            }
        }

        /// <summary>
        /// Get Enabled
        /// </summary>
        /// <param name="guiState">The gui state</param>
        /// <returns>Always true</returns>
        protected virtual bool GetEnabled(IGUIState guiState)
        {
            return true;
        }

        /// <summary>
        /// On Begin Layout
        /// </summary>
        /// <param name="data">The data layout</param>
        /// <param name="guiState">The gui state</param>
        /// <returns>data</returns>
        protected virtual LayoutData OnBeginLayout(LayoutData data, IGUIState guiState)
        {
            return data;
        }

        /// <summary>
        /// On End Layout
        /// /// </summary>
        /// <param name="guiState">The gui state</param>
        protected virtual void OnEndLayout(IGUIState guiState)
        {
            
        }

        /// <summary>
        /// On Repaint
        /// </summary>
        /// <param name="guiState">The gui state</param>
        /// <param name="index">The index</param>
        protected virtual void OnRepaint(IGUIState guiState, int index)
        {
            
        }

        /// <summary>
        /// Get Count
        /// </summary>
        /// <returns>Always return 1</returns>
        protected virtual int GetCount()
        {
            return 1;
        }

        /// <summary>
        /// Get Position
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <param name="index">the index</param>
        /// <returns>Always return Vector3.zero</returns>
        protected virtual Vector3 GetPosition(IGUIState guiState, int index)
        {
            return Vector3.zero;
        }

        /// <summary>
        /// Get Forward
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <param name="index">the index</param>
        /// <returns>Always return Vector3.forward</returns>
        protected virtual Vector3 GetForward(IGUIState guiState, int index)
        {
            return Vector3.forward;
        }

        /// <summary>
        /// Get Up
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <param name="index">the index</param>
        /// <returns>Always return Vector3.up</returns>
        protected virtual Vector3 GetUp(IGUIState guiState, int index)
        {
            return Vector3.up;
        }

        /// <summary>
        /// Get Right
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <param name="index">the index</param>
        /// <returns>Always return Vector3.right</returns>
        protected virtual Vector3 GetRight(IGUIState guiState, int index)
        {
            return Vector3.right;
        }

        /// <summary>
        /// Get Distance
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <param name="index">the index</param>
        /// <returns>Always return layoutData.distance</returns>
        protected virtual float GetDistance(IGUIState guiState, int index)
        {
            return layoutData.distance;
        }

        /// <summary>
        /// Get User Data
        /// </summary>
        /// <param name="guiState"></param>
        /// <param name="index"></param>
        /// <returns>Always return null</returns>
        protected virtual object GetUserData(IGUIState guiState, int index)
        {
            return null;
        }
    }
}
