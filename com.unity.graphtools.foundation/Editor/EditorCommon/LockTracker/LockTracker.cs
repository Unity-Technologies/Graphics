using System;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Serializable]
    class LockTracker
    {
        static readonly GUIContent k_LockMenuGUIContent = new GUIContent("Lock");

        [HideInInspector]
        public LockStateEvent lockStateChanged = new LockStateEvent();

        [HideInInspector]
        [SerializeField]
        bool m_IsLocked;

        public bool IsLocked
        {
            get => m_IsLocked;
            set
            {
                bool isLocked = m_IsLocked;
                m_IsLocked = value;
                if (isLocked == m_IsLocked)
                    return;
                lockStateChanged.Invoke(m_IsLocked);
            }
        }

        public void AddItemsToMenu(GenericMenu menu, bool disabled = false)
        {
            if (disabled)
                menu.AddDisabledItem(k_LockMenuGUIContent);
            else
                menu.AddItem(k_LockMenuGUIContent, IsLocked, FlipLocked);
        }

        void FlipLocked()
        {
            IsLocked = !IsLocked;
        }

        [Serializable]
        internal class LockStateEvent : UnityEvent<bool>
        {
        }
    }
}
