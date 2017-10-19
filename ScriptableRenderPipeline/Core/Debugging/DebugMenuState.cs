using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering
{
    public abstract class DebugItemState
        : ScriptableObject
    {
        public DebugItemState()
        {

        }

        public string panelName = "";
        public string itemName = "";

        protected DebugItem m_DebugItem = null;

        public bool isValid { get { return m_DebugItem != null; } }

        public abstract void UpdateDebugItemValue();
        public abstract void SetValue(object value);

        public void Initialize(DebugItem item)
        {
            this.panelName = item.panelName;
            this.itemName = item.name;
            m_DebugItem = item;
        }
    }

    public class DebugItemState<T> : DebugItemState
    {
        public T value;

        public override void SetValue(object value)
        {
            this.value = (T)value;
        }

        public override void UpdateDebugItemValue()
        {
            if(m_DebugItem == null)
            {
                if (itemName != "" && panelName != "")
                {
                    DebugMenuManager dmm = DebugMenuManager.instance;
                    DebugPanel menu = dmm.GetDebugPanel(panelName);
                    if (menu != null)
                    {
                        m_DebugItem = menu.GetDebugItem(itemName);
                    }
                }
            }

            if(m_DebugItem != null) // Can happen if not all menu are populated yet (depends on call order...)
                m_DebugItem.SetValue(value, false);
        }
    }

    public class DebugMenuState
        : ScriptableObject
    {
        [SerializeField]
        List<DebugItemState> m_ItemStateList = new List<DebugItemState>();

        public void OnEnable()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.undoRedoPerformed += OnUndoRedoPerformed;
#endif
            DebugMenuManager.instance.SetDebugMenuState(this);
        }

        public void OnDisable()
        {
            DebugMenuManager.instance.SetDebugMenuState(null);

#if UNITY_EDITOR
            UnityEditor.Undo.undoRedoPerformed -= OnUndoRedoPerformed;
#endif
        }

        public void ReInitializeDebugItemStates()
        {
            CleanUp();
            // Populate item states
            DebugMenuManager dmm = DebugMenuManager.instance;
            for (int panelIdx = 0; panelIdx < dmm.panelCount; ++panelIdx)
            {
                DebugPanel panel = dmm.GetDebugPanel(panelIdx);
                for (int itemIdx = 0; itemIdx < panel.itemCount; ++itemIdx)
                {
                    DebugItem item = panel.GetDebugItem(itemIdx);
                    DebugItemState debugItemState = FindDebugItemState(item);
                    if (debugItemState == null)
                    {
                        debugItemState = item.handler.CreateDebugItemState();
                        if (debugItemState != null)
                        {
                            debugItemState.hideFlags = HideFlags.DontSave;
                            debugItemState.Initialize(item);
                            debugItemState.SetValue(item.GetValue());
                            AddDebugItemState(debugItemState);
                        }
                        else
                        {
                            Debug.LogWarning(String.Format("DebugItemState for item {0} of type {1} is not provided.\nDid you implement CreateDebugItemState in your custom Handler?", item.name, item.type));
                        }
                    }
                }
            }

            UpdateAllDebugItems();
        }

        private void CleanUp()
        {
            foreach (var item in m_ItemStateList)
            {
                Object.DestroyImmediate(item);
            }

            m_ItemStateList.Clear();
        }

        public void OnDestroy()
        {
            CleanUp();
        }

        void OnUndoRedoPerformed()
        {
            // Maybe check a hash or something? So that we don't do that at each redo...
            UpdateAllDebugItems();
#if UNITY_EDITOR
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
#endif
        }

        private void AddDebugItemState(DebugItemState state)
        {
            m_ItemStateList.Add(state);
        }

        public DebugItemState FindDebugItemState(DebugItem item)
        {
            return m_ItemStateList.Find(x => x.itemName == item.name && x.panelName == item.panelName);
        }

        private void UpdateAllDebugItems()
        {
            foreach (var itemState in m_ItemStateList)
            {
                itemState.UpdateDebugItemValue();
            }
#if UNITY_EDITOR
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
#endif
        }
    }
}
