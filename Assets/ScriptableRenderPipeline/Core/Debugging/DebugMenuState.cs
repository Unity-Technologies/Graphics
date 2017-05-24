using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering
{
    [Serializable]
    public abstract class DebugItemState
        : ScriptableObject
    {
        public DebugItemState()
        {

        }

        public string panelName = "";
        public string itemName = "";

        public abstract void UpdateDebugItemValue();
        public abstract void SetValue(object value);

        public void Initialize(string itemName, string panelName)
        {
            this.panelName = panelName;
            this.itemName = itemName;
        }
    }

    public class DebugItemState<T> : DebugItemState
    {
        public T value;

        public override void SetValue(object value)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "DebugMenu State Update");
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            this.value = (T)value;
        }

        public override void UpdateDebugItemValue()
        {
            DebugMenuManager dmm = DebugMenuManager.instance;
            DebugPanel menu = dmm.GetDebugPanel(panelName);
            if (menu != null)
            {
                DebugItem item = menu.GetDebugItem(itemName);
                if (item != null)
                {
                    item.SetValue(value, false);
                }
            }

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

            // We need to delay the actual update because at this point, some menus might not be created yet (depending on call order) so we can't update their values.
            DebugMenuManager.instance.RequireUpdateFromDebugItemState();
        }

        public void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.undoRedoPerformed -= OnUndoRedoPerformed;
#endif
            // We check consistency in OnDisable instead of OnEnable because we compare the serialized state to the currently running debug menu so we need to make sure that all debug menu are properly created (which is not the case in OnEnable depending on call order)
            CheckConsistency();
        }


#if UNITY_EDITOR
        void OnUndoRedoPerformed()
        {
            // Maybe check a hash or something? So that we don't do that at each redo...
            UpdateAllDebugItems();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
#endif

        void CheckConsistency()
        {
            // Remove all objects that may have been removed from the debug menu since last serialization
            DebugMenuManager dmm = DebugMenuManager.instance;
            List<DebugItemState> tempList = new List<DebugItemState>();
            foreach(var itemState in  m_ItemStateList)
            {
                DebugItem item = null;
                DebugPanel menu = dmm.GetDebugPanel(itemState.panelName);
                if(menu != null)
                {
                    item = menu.GetDebugItem(itemState.itemName);
                }

                // Item no longer exist, clean up its state from the asset.
                if (item == null)
                {
                    tempList.Add(itemState);
                }
            }

            foreach(var itemState in tempList)
            {
                m_ItemStateList.Remove(itemState);
                Object.DestroyImmediate(itemState, true);
            }
        }

        public void AddDebugItemState(DebugItemState state)
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.AddObjectToAsset(state, this);
#endif
            m_ItemStateList.Add(state);
        }

        public DebugItemState FindDebugItemState(string itemName, string menuName)
        {
            return m_ItemStateList.Find(x => x.itemName == itemName && x.panelName == menuName);
        }

        public void UpdateAllDebugItems()
        {
            foreach (var itemState in m_ItemStateList)
            {
                itemState.UpdateDebugItemValue();
            }
        }
    }
}