using System;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D.GUIFramework
{
    internal abstract class GUIAction
    {
        private int m_ID = -1;

        public Func<IGUIState, GUIAction, bool> enable;
        public Func<IGUIState, GUIAction, bool> enableRepaint;
        public Func<IGUIState, GUIAction, bool> repaintOnMouseMove;
        public Action<IGUIState, GUIAction> onPreRepaint;
        public Action<IGUIState, GUIAction> onRepaint;

        public int ID
        {
            get { return m_ID; }
        }

        public void OnGUI(IGUIState guiState)
        {
            m_ID = guiState.GetControlID(GetType().GetHashCode(), FocusType.Passive);

            if (guiState.hotControl == 0 && IsEnabled(guiState) && CanTrigger(guiState) && GetTriggerContidtion(guiState))
            {
                guiState.hotControl = ID;
                OnTrigger(guiState);
            }

            if (guiState.hotControl == ID)
            {
                if (GetFinishContidtion(guiState))
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

        public bool IsEnabled(IGUIState guiState)
        {
            if (enable != null)
                return enable(guiState, this);

            return true;
        }

        public bool IsRepaintEnabled(IGUIState guiState)
        {
            if (!IsEnabled(guiState))
                return false;

            if (enableRepaint != null)
                return enableRepaint(guiState, this);

            return true;
        }

        public void PreRepaint(IGUIState guiState)
        {
            Debug.Assert(guiState.eventType == EventType.Repaint);

            if (IsEnabled(guiState) && onPreRepaint != null)
                onPreRepaint(guiState, this);
        }

        private void Repaint(IGUIState guiState)
        {
            Debug.Assert(guiState.eventType == EventType.Repaint);
            
            if (onRepaint != null)
                onRepaint(guiState, this);
        }

        internal bool IsRepaintOnMouseMoveEnabled(IGUIState guiState)
        {
            if (!IsEnabled(guiState) || !IsRepaintEnabled(guiState))
                return false;

            if (repaintOnMouseMove != null)
                return repaintOnMouseMove(guiState, this);

            return false;
        }

        protected abstract bool GetFinishContidtion(IGUIState guiState);
        protected abstract bool GetTriggerContidtion(IGUIState guiState);
        protected virtual bool CanTrigger(IGUIState guiState) { return true; }
        protected virtual void OnTrigger(IGUIState guiState)
        {
            
        }

        protected virtual void OnPerform(IGUIState guiState)
        {

        }

        protected virtual void OnFinish(IGUIState guiState)
        {
            
        }
    }
}
