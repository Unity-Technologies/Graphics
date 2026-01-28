#if ENABLE_UIELEMENTS_MODULE && (UNITY_EDITOR || DEVELOPMENT_BUILD)
#define ENABLE_RENDERING_DEBUGGER_UI
#endif

#if ENABLE_RENDERING_DEBUGGER_UI

using System;
using System.Collections.Generic;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

// This file encapsulates the deprecated widget state handling logic for the DebugWindow. It will be removed in a future release.

#pragma warning disable CS0618 // Type or member is obsolete

namespace UnityEditor.Rendering
{
    [Serializable]
    sealed class WidgetStateDictionary : SerializedDictionary<string, DebugState> { }

    sealed partial class DebugWindow
    {
        [SerializeField]
        WidgetStateDictionary m_WidgetStates;

        static bool s_TypeMapDirty;
        static Dictionary<Type, Type> s_WidgetStateMap; // DebugUI.Widget type -> DebugState type

        [DidReloadScripts]
        static void OnEditorReload()
        {
            s_TypeMapDirty = true;
        }

        void HookLegacyWidgetStateHandlingCallbacks()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        void OnBeforeAssemblyReload()
        {
            UpdateWidgetStates(updateCurrentStates: true);
        }

        void OnAfterAssemblyReload()
        {
            ApplyStates();
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
                UpdateWidgetStates(updateCurrentStates: true);
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
                ApplyStates();
        }

        static void RebuildTypeMaps()
        {
            // Map states to widget (a single state can map to several widget types if the value to
            // serialize is the same)
            var attrType = typeof(DebugStateAttribute);
            var stateTypes = new List<Type>();
            foreach (var type in CoreUtils.GetAllTypesDerivedFrom<DebugState>())
            {
                if (type.IsDefined(attrType, false) && !type.IsAbstract)
                {
                    stateTypes.Add(type);
                }
            }

            s_WidgetStateMap = new Dictionary<Type, Type>();

            foreach (var stateType in stateTypes)
            {
                var attr = (DebugStateAttribute)stateType.GetCustomAttributes(attrType, false)[0];

                foreach (var t in attr.types)
                    s_WidgetStateMap.Add(t, stateType);
            }

            // Done
            s_TypeMapDirty = false;
        }

        void DestroyWidgetStates()
        {
            if (m_WidgetStates == null)
                return;

            // Clear all the states from memory
            foreach (var state in m_WidgetStates)
            {
                var s = state.Value;
                DestroyImmediate(s);
            }

            m_WidgetStates.Clear();
        }

        void ReloadWidgetStates()
        {
            if (m_WidgetStates == null)
                return;

            // Clear states from memory that don't have a corresponding widget
            List<string> keysToRemove = new ();
            foreach (var state in m_WidgetStates)
            {
                var widget = DebugManager.instance.GetItem(state.Key);
                if (widget == null)
                {
                    var s = state.Value;
                    DestroyImmediate(s);
                    keysToRemove.Add(state.Key);
                }
            }

            // Cleanup null entries because they can break the dictionary serialization
            foreach (var key in keysToRemove)
            {
                m_WidgetStates.Remove(key);
            }

            UpdateWidgetStates();
        }

        bool AreWidgetStatesValid()
        {
            foreach (var state in m_WidgetStates)
            {
                if (state.Value == null)
                {
                    return false;
                }
            }
            return true;
        }

        // We use item states to keep a cached value of each serializable debug items in order to
        // handle domain reloads and play mode entering/exiting
        // Note: no removal of orphan states
        void UpdateWidgetStates(bool updateCurrentStates = false)
        {
            foreach (var panel in DebugManager.instance.panels)
                UpdateWidgetStates(panel, updateCurrentStates);
        }

        DebugState GetOrCreateDebugStateForValueField(DebugUI.Widget widget, bool updateCurrentStates)
        {
            // Skip runtime & readonly only items
            if (widget.isInactiveInEditor)
                return null;

            if (widget is not DebugUI.IValueField valueField)
                return null;

            if (!widget.m_RequiresLegacyStateHandling)
                return null;

            string queryPath = widget.queryPath;
            if (!m_WidgetStates.TryGetValue(queryPath, out var state) || state == null)
            {
                var widgetType = widget.GetType();
                if (s_WidgetStateMap.TryGetValue(widgetType, out Type stateType))
                {
                    Assert.IsNotNull(stateType);
                    state = (DebugState)CreateInstance(stateType);
                    state.queryPath = queryPath;
                    state.SetValue(valueField.GetValue(), valueField);
                    m_WidgetStates[queryPath] = state;
                }
            }

            if (state != null && updateCurrentStates)
            {
                state.SetValue(valueField.GetValue(), valueField);
            }

            return state;
        }

        void UpdateWidgetStates(DebugUI.IContainer container, bool updateCurrentStates)
        {
            // Skip runtime only containers, we won't draw them so no need to serialize them either
            if (container is DebugUI.Widget actualWidget && actualWidget.isInactiveInEditor)
                return;

            // Recursively update widget states
            foreach (var widget in container.children)
            {
                // Skip non-serializable widgets but still traverse them in case one of their
                // children needs serialization support
                var state = GetOrCreateDebugStateForValueField(widget, updateCurrentStates);

                if (state != null)
                    continue;

                // Recurse if the widget is a container
                if (widget is DebugUI.IContainer containerField)
                    UpdateWidgetStates(containerField, updateCurrentStates);
            }
        }

        void ApplyStates()
        {
            // If we are in playmode, and the runtime UI is shown, avoid that the editor UI
            // applies the data of the internal debug states, as they are not kept in sync
            if (Application.isPlaying && DebugManager.instance.displayRuntimeUI)
                return;

            foreach (var state in m_WidgetStates)
                ApplyState(state.Key, state.Value);
        }

        void ApplyState(string queryPath, DebugState state)
        {
            if (state == null || !(DebugManager.instance.GetItem(queryPath) is DebugUI.IValueField widget))
                return;

            widget.SetValue(state.GetValue());
        }
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
#endif
