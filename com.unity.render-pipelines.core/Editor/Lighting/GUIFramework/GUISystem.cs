using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GUIFramework
{
    /// <summary>
    /// GUI System
    /// </summary>
    public class GUISystem
    {
        private readonly int kControlIDCheckHashCode = "ControlIDCheckHashCode".GetHashCode();

        private List<Control> m_Controls = new List<Control>();
        private List<GUIAction> m_Actions = new List<GUIAction>();
        private List<HandlesManipulator> m_Manipulators = new List<HandlesManipulator>();
        private IGUIState m_GUIState;
        private int m_PrevNearestControl = -1;
        private LayoutData m_PrevNearestLayoutData = LayoutData.zero;
#if GUIFRAMEWORK_CONTROL_ID_CHECK
        private int m_ControlIDCheck = -1;
#endif

        /// <summary>
        /// Initializes and returns an instance of GUISystem
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        public GUISystem(IGUIState guiState)
        {
            m_GUIState = guiState;
        }

        /// <summary>
        /// Add control to the internal list of controls
        /// </summary>
        /// <param name="control">Control have to be added</param>
        public void AddControl(Control control)
        {
            if (control == null)
                throw new NullReferenceException("Control is null");

            m_Controls.Add(control);
        }

        /// <summary>
        /// Remove control from the internal list of controls
        /// </summary>
        /// <param name="control">Control have to be removed</param>
        public void RemoveControl(Control control)
        {
            m_Controls.Remove(control);
        }

        /// <summary>
        /// Add Action to the internal list of actions
        /// </summary>
        /// <param name="action">Action added</param>
        public void AddAction(GUIAction action)
        {
            if (action == null)
                throw new NullReferenceException("Action is null");

            m_Actions.Add(action);
        }

        /// <summary>
        /// Remove Action from the internal list of actions
        /// </summary>
        /// <param name="action">Action removed</param>
        public void RemoveAction(GUIAction action)
        {
            m_Actions.Remove(action);
        }

        /// <summary>
        /// Add Manipulator to the internal list of manipulators
        /// </summary>
        /// <param name="manipulator">Manipulator added</param>
        public void AddManipulator(HandlesManipulator manipulator)
        {
            if (manipulator == null)
                throw new NullReferenceException("Manipulator is null");

            m_Manipulators.Add(manipulator);
        }

        /// <summary>
        /// Remove Manipulator from the internal list of manipulators
        /// </summary>
        /// <param name="manipulator">Manipulator removed</param>
        public void RemoveManipulator(HandlesManipulator manipulator)
        {
            m_Manipulators.Remove(manipulator);
        }

        /// <summary>
        /// Calls the methods in its invocation list when enter in the GUI
        /// </summary>
        public void OnGUI()
        {
#if GUIFRAMEWORK_CONTROL_ID_CHECK
            var controlIDCheck = m_GUIState.GetControlID(kControlIDCheckHashCode, FocusType.Passive);

            if (m_GUIState.eventType == EventType.Layout)
                m_ControlIDCheck = controlIDCheck;
            else if (m_GUIState.eventType != EventType.Used && m_ControlIDCheck != controlIDCheck)
                Debug.LogWarning("GetControlID at event " + m_GUIState.eventType + " returns a controlID different from the one in Layout event");
#endif

            var nearestLayoutData = LayoutData.zero;

            foreach (var manipulator in m_Manipulators)
                manipulator.OnGUI(m_GUIState);

            foreach (var control in m_Controls)
                control.GetControl(m_GUIState);

            if (m_GUIState.eventType == EventType.Layout)
            {
                foreach (var control in m_Controls)
                    control.BeginLayout(m_GUIState);

                foreach (var control in m_Controls)
                {
                    control.Layout(m_GUIState);
                    nearestLayoutData = LayoutData.Nearest(nearestLayoutData, control.layoutData);
                }

                foreach (var control in m_Controls)
                    m_GUIState.AddControl(control.ID, control.layoutData.distance);

                foreach (var control in m_Controls)
                    control.EndLayout(m_GUIState);

                foreach (var manipulator in m_Manipulators)
                    manipulator.EndLayout(m_GUIState);

                if (m_PrevNearestControl == m_GUIState.nearestControl)
                {
                    if (nearestLayoutData.index != m_PrevNearestLayoutData.index)
                        m_GUIState.Repaint();
                }
                else
                {
                    m_PrevNearestControl = m_GUIState.nearestControl;
                    m_GUIState.Repaint();
                }

                m_PrevNearestLayoutData = nearestLayoutData;
            }

            if (m_GUIState.eventType == EventType.Repaint)
            {
                foreach (var action in m_Actions)
                    if (action.IsRepaintEnabled(m_GUIState))
                        action.PreRepaint(m_GUIState);

                foreach (var control in m_Controls)
                    control.Repaint(m_GUIState);
            }

            var repaintOnMouseMove = false;

            foreach (var action in m_Actions)
            {
                if (IsMouseMoveEvent())
                    repaintOnMouseMove |= action.IsRepaintOnMouseMoveEnabled(m_GUIState);

                action.OnGUI(m_GUIState);
            }

            if (repaintOnMouseMove)
                m_GUIState.Repaint();
        }

        /// <summary>
        /// Calls the methods in its invocation list when the Mouse Moves
        /// </summary>
        /// <returns>true, if mouse moved</returns>
        private bool IsMouseMoveEvent()
        {
            return m_GUIState.eventType == EventType.MouseMove || m_GUIState.eventType == EventType.MouseDrag;
        }
    }
}
