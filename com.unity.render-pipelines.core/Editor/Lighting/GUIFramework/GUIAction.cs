using System;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// GUI Action
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
        /// ID
        /// </summary>
        public int ID
        {
            get { return m_ID; }
        }

        /// <summary>
        /// OnGui
        /// </summary>
        /// <param name="guiState">The gui state</param>
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
        /// Is Enabled
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <returns>true if enabled</returns>
        public bool IsEnabled(IGUIState guiState)
        {
            if (getEnable != null)
                return getEnable(guiState, this);

            return true;
        }

        /// <summary>
        /// Is Repaint Enabled
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <returns>true if repaint enabled</returns>
        public bool IsRepaintEnabled(IGUIState guiState)
        {
            if (!IsEnabled(guiState))
                return false;

            if (enableRepaint != null)
                return enableRepaint(guiState, this);

            return true;
        }

        /// <summary>
        /// PreRepaint
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        public void PreRepaint(IGUIState guiState)
        {
            Debug.Assert(guiState.eventType == EventType.Repaint);

            if (IsEnabled(guiState) && onPreRepaint != null)
                onPreRepaint(guiState, this);
        }

        /// <summary>
        /// Repaint
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        private void Repaint(IGUIState guiState)
        {
            Debug.Assert(guiState.eventType == EventType.Repaint);

            if (onRepaint != null)
                onRepaint(guiState, this);
        }

        /// <summary>
        /// Is Repaint On Mouse Move Enabled
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <returns>true if repaint on mouse enabled</returns>
        internal bool IsRepaintOnMouseMoveEnabled(IGUIState guiState)
        {
            if (!IsEnabled(guiState) || !IsRepaintEnabled(guiState))
                return false;

            if (repaintOnMouseMove != null)
                return repaintOnMouseMove(guiState, this);

            return false;
        }

        /// <summary>
        /// GetFinishCondition
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <returns>true if finish condition validated</returns>
        protected abstract bool GetFinishCondition(IGUIState guiState);
        /// <summary>
        /// GetTriggerCondition
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <returns>true if the trigger condition validated</returns>
        protected abstract bool GetTriggerCondition(IGUIState guiState);
        /// <summary>
        /// CanTrigger
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        /// <returns>Always return true</returns>
        protected virtual bool CanTrigger(IGUIState guiState) { return true; }
        /// <summary>
        /// OnTrigger
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        protected virtual void OnTrigger(IGUIState guiState)
        {
        }

        /// <summary>
        /// OnPerform
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        protected virtual void OnPerform(IGUIState guiState)
        {
        }

        /// <summary>
        /// OnFinish
        /// </summary>
        /// <param name="guiState">The GUI State</param>
        protected virtual void OnFinish(IGUIState guiState)
        {
        }
    }
}
