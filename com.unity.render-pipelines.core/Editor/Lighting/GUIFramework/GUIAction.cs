using System;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// An interface that represents a GUI action.
    /// </summary>
    public abstract class GUIAction
    {
        private int m_ID = -1;

        /// <summary>
        /// Func for GetEnable
        /// </summary>
        public Func<IGUIState, GUIAction, bool> getEnable;
        /// <summary>
        /// Func for EnabledRepaint
        /// </summary>
        public Func<IGUIState, GUIAction, bool> enableRepaint;
        /// <summary>
        /// Func for repaintOnMouseMove
        /// </summary>
        public Func<IGUIState, GUIAction, bool> repaintOnMouseMove;
        /// <summary>
        /// Action for OnPreRepaint
        /// </summary>
        public Action<IGUIState, GUIAction> onPreRepaint;
        /// <summary>
        /// Func for OnRepaint
        /// </summary>
        public Action<IGUIState, GUIAction> onRepaint;

        /// <summary>
        /// The action ID.
        /// </summary>
        public int ID
        {
            get { return m_ID; }
        }

        /// <summary>
        /// Calls the methods in its invocation list when Unity draws this GUIAction's GUI.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        public void OnGUI(IGUIState guiState)
        {
            m_ID = guiState.GetControlID(GetType().GetHashCode(), FocusType.Passive);

            if (guiState.hotControl == 0 && IsEnabled(guiState) && CanTrigger(guiState) && GetTriggerCondition(guiState))
            {
                guiState.hotControl = ID;
                OnTrigger(guiState);
            }

            if (guiState.hotControl == ID)
            {
                if (GetFinishCondition(guiState))
                {
                    OnFinish(guiState);
                    guiState.hotControl = 0;
                }
                else
                {
                    OnPerform(guiState);
                }
            }

            if (guiState.eventType == EventType.Repaint && IsRepaintEnabled(guiState))
                Repaint(guiState);
        }

        /// <summary>
        /// Checks whether the GUIAction is enabled.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <returns>Returns `true` if the GUIAction is enabled in the custom editor. Otherwise, returns `false`.</returns>
        public bool IsEnabled(IGUIState guiState)
        {
            if (getEnable != null)
                return getEnable(guiState, this);

            return true;
        }

        /// <summary>
        /// Checks whether the GUIAction should repaint.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <returns>Returns `true` if the GUIAction should repaint. Otherwise, returns `false`.</returns>
        public bool IsRepaintEnabled(IGUIState guiState)
        {
            if (!IsEnabled(guiState))
                return false;

            if (enableRepaint != null)
                return enableRepaint(guiState, this);

            return true;
        }

        /// <summary>
        /// Preprocessing that occurs before the GUI repaints.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        public void PreRepaint(IGUIState guiState)
        {
            Debug.Assert(guiState.eventType == EventType.Repaint);

            if (IsEnabled(guiState) && onPreRepaint != null)
                onPreRepaint(guiState, this);
        }

        /// <summary>
        /// Calls the methods in its invocation list when repainting the GUI.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        private void Repaint(IGUIState guiState)
        {
            Debug.Assert(guiState.eventType == EventType.Repaint);

            if (onRepaint != null)
                onRepaint(guiState, this);
        }

        /// <summary>
        /// Checks whether the GUI should repaint if the mouse moves over it.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <returns>Returns `true` if the GUI should repaint if the moves moves over it. Otherwise, returns `false`.</returns>
        internal bool IsRepaintOnMouseMoveEnabled(IGUIState guiState)
        {
            if (!IsEnabled(guiState) || !IsRepaintEnabled(guiState))
                return false;

            if (repaintOnMouseMove != null)
                return repaintOnMouseMove(guiState, this);

            return false;
        }

        /// <summary>
        /// Determines whether the finish condition has been met.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <returns>Returns `true` if finish condition has been met. Otherwise, returns `false`.</returns>
        protected abstract bool GetFinishCondition(IGUIState guiState);
        /// <summary>
        /// Determines whether the trigger condition has been met.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <returns>Returns `true` if finish condition has been met. Otherwise, returns `false`.</returns>
        protected abstract bool GetTriggerCondition(IGUIState guiState);
        /// <summary>
        /// Determines whether the GUIAction can trigger.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <returns>Always returns `true`.</returns>
        protected virtual bool CanTrigger(IGUIState guiState) { return true; }
        /// <summary>
        /// Calls the methods in its invocation list when triggered.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        protected virtual void OnTrigger(IGUIState guiState)
        {
        }

        /// <summary>
        /// Calls the methods in its invocation list when performed.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        protected virtual void OnPerform(IGUIState guiState)
        {
        }

        /// <summary>
        /// Calls the methods in its invocation list when finished.
        /// </summary>
        /// <param name="guiState">The current state of the custom editor.</param>
        protected virtual void OnFinish(IGUIState guiState)
        {
        }
    }
}
