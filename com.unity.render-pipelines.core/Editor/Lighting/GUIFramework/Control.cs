using System;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// An interface that represents a UI control.
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
        /// The name of the control.
        /// </summary>
        public string name
        {
            get { return m_Name; }
        }

        /// <summary>
        /// The control ID. The GUI uses this to identify the control.
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
        /// Initializes and returns an instance of Control
        /// </summary>
        /// <param name="name">The name of the control</param>
        public Control(string name)
        {
            m_Name = name;
            m_NameHashCode = name.GetHashCode();
        }

        /// <summary>
        /// Gets the control from the guiState.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
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
        /// Begins the layout for this control. A call to EndLayout must always follow a call to this function.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        public void BeginLayout(IGUIState guiState)
        {
            Debug.Assert(guiState.eventType == EventType.Layout);

            if (m_Enabled)
                m_LayoutData = OnBeginLayout(LayoutData.zero, guiState);
        }

        /// <summary>
        /// Gets the control's layout data from the guiState. 
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
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
        /// Ends the layout for this control. This function must always follow a call to BeginLayout().
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        public void EndLayout(IGUIState guiState)
        {
            Debug.Assert(guiState.eventType == EventType.Layout);

            if (m_Enabled)
                OnEndLayout(guiState);
        }

        /// <summary>
        /// Repaints the control. 
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        public void Repaint(IGUIState guiState)
        {
            if (m_Enabled)
            {
                for (var i = 0; i < GetCount(); ++i)
                    OnRepaint(guiState, i);
            }
        }

        /// <summary>
        /// Checks whether the control is enabled in the custom editor.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <returns>Returns `true` if the control is enabled in the custom editor. Otherwise, returns `false`.</returns>
        protected virtual bool GetEnabled(IGUIState guiState)
        {
            return true;
        }

        /// <summary>
        /// Called when the control begins its layout.
        /// </summary>
        /// <param name="data">The data layout</param>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <returns>data</returns>
        protected virtual LayoutData OnBeginLayout(LayoutData data, IGUIState guiState)
        {
            return data;
        }

        /// <summary>
        /// Called when the control ends its layout.
        /// /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        protected virtual void OnEndLayout(IGUIState guiState)
        {
        }

        /// <summary>
        /// Called when the control repaints its contents.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The index.</param>
        protected virtual void OnRepaint(IGUIState guiState, int index)
        {
        }

        /// <summary>
        /// Get Count
        /// </summary>
        /// <returns>Returns 1.</returns>
        protected virtual int GetCount()
        {
            return 1;
        }

        /// <summary>
        /// Gets the position of the control.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The index.</param>
        /// <returns>Returns Vector3.zero.</returns>
        protected virtual Vector3 GetPosition(IGUIState guiState, int index)
        {
            return Vector3.zero;
        }

        /// <summary>
        /// Gets the forward vector of the control.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The index.</param>
        /// <returns>Returns Vector3.forward.</returns>
        protected virtual Vector3 GetForward(IGUIState guiState, int index)
        {
            return Vector3.forward;
        }

        /// <summary>
        /// Gets the up vector of the control.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The index.</param>
        /// <returns>Returns Vector3.up,</returns>
        protected virtual Vector3 GetUp(IGUIState guiState, int index)
        {
            return Vector3.up;
        }

        /// <summary>
        /// Gets the right vector of the control.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The index.</param>
        /// <returns>Returns Vector3.right.</returns>
        protected virtual Vector3 GetRight(IGUIState guiState, int index)
        {
            return Vector3.right;
        }

        /// <summary>
        /// Get Distance
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The index.</param>
        /// <returns>Returns layoutData.distance.</returns>
        protected virtual float GetDistance(IGUIState guiState, int index)
        {
            return layoutData.distance;
        }

        /// <summary>
        /// Gets the control's user data. 
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <param name="index">The index.</param>
        /// <returns>Returns `null`.</returns>
        protected virtual object GetUserData(IGUIState guiState, int index)
        {
            return null;
        }
    }
}
