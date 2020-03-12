using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;

namespace UnityEditor.ShaderGraph.Drawing
{
    // TODO: Temporary Inspector
    // TODO: Replace this with Sai's work
    class InspectorView : VisualElement
    {
        GraphData m_GraphData;
        PropertySheet m_PropertySheet;

        // Track enabled states of foldouts
        Dictionary<ITargetImplementation, bool> m_ImplementationFoldouts;

        public InspectorView(GraphData graphData)
        {
            name = "inspectorView";
            m_GraphData = graphData;
            m_ImplementationFoldouts = new Dictionary<ITargetImplementation, bool>();

            // Styles
            style.width = 270;
            style.height = 400;
            style.position = Position.Absolute;
            style.right = 0;
            style.top = 0;
            style.backgroundColor = new Color(.17f, .17f, .17f, 1);
            style.flexDirection = FlexDirection.Column;

            Rebuild();
        }

        void Rebuild()
        {
            m_PropertySheet = new PropertySheet();

            // Add base settings
            CreateTargetSettings();
            m_PropertySheet.Add(new PropertyRow(new Label("")));

            // Add per-implementation settings
            CreateImplementationSettings();
            m_PropertySheet.Add(new PropertyRow(new Label("")));
            
            Add(m_PropertySheet);
        }

        void CreateTargetSettings()
        {
            // Add Label
            // Use PropertyView to maintain layout
            var targetSettingsLabel = new Label("Target Settings");
            targetSettingsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_PropertySheet.Add(new PropertyRow(targetSettingsLabel));

            m_PropertySheet.Add(new PropertyRow(new Label("Target")), (row) =>
                {
                    row.Add(new IMGUIContainer(() => {
                        EditorGUI.BeginChangeCheck();
                        m_GraphData.activeTargetIndex = EditorGUILayout.Popup(m_GraphData.activeTargetIndex, 
                            m_GraphData.validTargets.Select(x => x.displayName).ToArray(), GUILayout.Width(100f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_GraphData.UpdateTargets();
                            OnChange();
                        }
                    }));
                });

            m_PropertySheet.Add(new PropertyRow(new Label("Implementations")), (row) =>
                {
                    row.Add(new IMGUIContainer(() => {
                        EditorGUI.BeginChangeCheck();
                        m_GraphData.activeTargetImplementationBitmask = EditorGUILayout.MaskField(m_GraphData.activeTargetImplementationBitmask, 
                            m_GraphData.validImplementations.Select(x => x.displayName).ToArray(), GUILayout.Width(100f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_GraphData.UpdateTargets();
                            OnChange();
                        }
                    }));
                });
        }

        void CreateImplementationSettings()
        {
            // Add Label
            // Use PropertyView to maintain layout
            var implementationSettingsLabel =  new Label("Implementation Settings");
            implementationSettingsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_PropertySheet.Add(new PropertyRow(implementationSettingsLabel));

            foreach(var implementation in m_GraphData.activeTargetImplementations)
            {
                // Ensure enabled state is being tracked and get value
                bool foldoutActive = true;
                if(!m_ImplementationFoldouts.TryGetValue(implementation, out foldoutActive))
                {
                    m_ImplementationFoldouts.Add(implementation, foldoutActive);
                }

                // Create foldout
                var foldout = new Foldout() { text = implementation.displayName, value = foldoutActive };
                foldout.RegisterValueChangedCallback(evt => 
                {
                    // Re-add foldout using enabled value
                    m_ImplementationFoldouts.Remove(implementation);
                    m_ImplementationFoldouts.Add(implementation, evt.newValue);
                    foldout.value = evt.newValue;

                    // Rebuild full GUI
                    Remove(m_PropertySheet);
                    Rebuild();
                });

                m_PropertySheet.Add(foldout);
                
                if(foldout.value)
                {
                    // Draw ImplementationData properties
                    implementation.GetInspectorContent(m_PropertySheet, () => {
                        // TODO: Currently I use this action to force a recompile
                        // TODO: How will the inspector actually work? (Sai)
                        OnChange();
                    });
                }
            }
        }

        void OnChange()
        {
            m_GraphData.UpdateActiveBlocks();
            m_GraphData.outputNode.Dirty(ModificationScope.Graph);
            Remove(m_PropertySheet);
            Rebuild();
        }
    }
}
