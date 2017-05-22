using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering
{
    [Serializable]
    public abstract class DebugMenuItemState
        : ScriptableObject
    {
        public DebugMenuItemState()
        {

        }

        public string menuName = "";
        public string itemName = "";

        public abstract void UpdateValue();
        public abstract void SetValue(object value);

        public void Initialize(string itemName, string menuName)
        {
            this.menuName = menuName;
            this.itemName = itemName;
        }
    }

    public class DebugMenuItemState<T> : DebugMenuItemState
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

        public override void UpdateValue()
        {
            DebugMenuManager dmm = DebugMenuManager.instance;
            DebugMenu menu = dmm.GetDebugMenu(menuName);
            if (menu != null)
            {
                DebugMenuItem item = menu.GetDebugMenuItem(itemName);
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
        List<DebugMenuItemState> m_ItemStateList = new List<DebugMenuItemState>();

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
            UpdateAllItems();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
#endif

        void CheckConsistency()
        {
            // Remove all objects that may have been removed from the debug menu.
            DebugMenuManager dmm = DebugMenuManager.instance;
            List<DebugMenuItemState> tempList = new List<DebugMenuItemState>();
            foreach(var itemState in  m_ItemStateList)
            {
                DebugMenuItem item = null;
                DebugMenu menu = dmm.GetDebugMenu(itemState.menuName);
                if(menu != null)
                {
                    item = menu.GetDebugMenuItem(itemState.itemName);
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

        public void AddDebugItemState(DebugMenuItemState state)
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.AddObjectToAsset(state, this);
#endif
            m_ItemStateList.Add(state);
        }

        public DebugMenuItemState FindDebugItemState(string itemName, string menuName)
        {
            return m_ItemStateList.Find(x => x.itemName == itemName && x.menuName == menuName);
        }

        public void UpdateAllItems()
        {
            foreach (var itemState in m_ItemStateList)
            {
                itemState.UpdateValue();
            }
        }
    }
}