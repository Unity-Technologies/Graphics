#if ENABLE_UIELEMENTS_MODULE && (UNITY_EDITOR || DEVELOPMENT_BUILD)
#define ENABLE_RENDERING_DEBUGGER_UI
#endif

#if ENABLE_RENDERING_DEBUGGER_UI
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering
{
    [AddComponentMenu("")] // Hide from Add Component menu
    class RuntimePersistentDebugUI : MonoBehaviour
    {
        UIDocument m_Document;
        VisualElement m_PersistentRoot;
        readonly Dictionary<DebugUI.Value, VisualElement> m_Values = new();
        readonly HashSet<DebugUI.ValueTuple> m_ValueTuples = new();

        void Awake()
        {
            RecreateGUI();
        }

        void RecreateGUI()
        {
            var resources = GraphicsSettings.GetRenderPipelineSettings<RenderingDebuggerRuntimeResources>();
            if (m_Document == null)
                m_Document = gameObject.AddComponent<UIDocument>();
            m_Document.panelSettings = resources.panelSettings;
            var rootVisualElement = m_Document.rootVisualElement;
            var styleSheets = resources.styleSheets;
            foreach (var uss in styleSheets)
                rootVisualElement.styleSheets.Add(uss);

            // Prevent input
            rootVisualElement.pickingMode = PickingMode.Ignore;
            rootVisualElement.focusable = false;
            rootVisualElement.tabIndex = -1;

            var persistentRoot = new VisualElement();
            persistentRoot.AddToClassList("debug-window-persistent-root");
            rootVisualElement.Add(persistentRoot);
            m_PersistentRoot = persistentRoot;
        }

        // Toggles persistent value widget on/off.
        internal void Toggle(DebugUI.Value valueWidget, string displayName = null)
        {
            // Remove & return
            if (m_Values.TryGetValue(valueWidget, out var existingVisualElement))
            {
                DebugManager.instance.schedulerTracker.SetEnabled(DebugUI.Context.RuntimePersistent, valueWidget, false);

                m_Values.Remove(valueWidget);
                m_PersistentRoot.Remove(existingVisualElement);
                return;
            }

            // Add
            var valueContainer = new VisualElement();
            valueContainer.AddToClassList("debug-window-persistent-value-container");

            var label = new Label(displayName);
            label.AddToClassList("debug-window-persistent-value-name");
            valueContainer.Add(label);

            var value = valueWidget.ToVisualElement(DebugUI.Context.RuntimePersistent);
            value.AddToClassList("debug-window-persistent-value-value");
            valueContainer.Add(value);

            m_PersistentRoot.Add(valueContainer);
            m_Values.Add(valueWidget, valueContainer);

            DebugManager.instance.schedulerTracker.SetEnabled(DebugUI.Context.RuntimePersistent, valueWidget, true);
        }

        // For ValueTuples (multiple values on one row), we cycle through the columns, and turn the widget
        // off after the last column.
        internal void Toggle(DebugUI.ValueTuple valueTupleWidget, int? forceTupleIndex = null)
        {
            int tupleIndex = valueTupleWidget.pinnedElementIndex;

            // Clear old widget
            if (m_ValueTuples.Contains(valueTupleWidget))
            {
                m_ValueTuples.Remove(valueTupleWidget);
                Toggle(valueTupleWidget.values[tupleIndex]);
            }

            if (forceTupleIndex != null)
                tupleIndex = forceTupleIndex.Value;

            // Enable next widget (unless at the last index)
            if (tupleIndex + 1 < valueTupleWidget.numElements)
            {
                valueTupleWidget.pinnedElementIndex = tupleIndex + 1;

                // Add column to name
                string displayName = valueTupleWidget.displayName;
                if (valueTupleWidget.parent is DebugUI.Foldout foldout)
                {
                    var columnLabels = foldout.columnLabels;
                    if (columnLabels != null && valueTupleWidget.pinnedElementIndex < columnLabels.Length)
                    {
                        displayName += $" ({columnLabels[valueTupleWidget.pinnedElementIndex]})";
                    }
                }

                Toggle(valueTupleWidget.values[valueTupleWidget.pinnedElementIndex], displayName);
                m_ValueTuples.Add(valueTupleWidget);
            }
            else
            {
                valueTupleWidget.pinnedElementIndex = -1;
            }
        }

        internal bool IsEmpty()
        {
            return m_Values.Count == 0;
        }
    }
}
#endif
