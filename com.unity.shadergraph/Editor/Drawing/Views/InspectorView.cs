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

            Rebuild();
        }

        void Rebuild()
        {
            m_PropertySheet = new PropertySheet();

            CreateTargetSettings(m_PropertySheet);
            m_PropertySheet.Add(new PropertyRow(new Label("")));

            CreateImplementationSettings(m_PropertySheet);
            m_PropertySheet.Add(new PropertyRow(new Label("")));
            
            Add(m_PropertySheet);
        }

        void CreateTargetSettings(PropertySheet ps)
        {
            var targetSettingsLabel =  new Label("Target Settings");
            targetSettingsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            ps.Add(new PropertyRow(targetSettingsLabel));

            ps.Add(new PropertyRow(new Label("Target")), (row) =>
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

            ps.Add(new PropertyRow(new Label("Implementations")), (row) =>
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

        void CreateImplementationSettings(PropertySheet ps)
        {
            var implementationSettingsLabel =  new Label("Implementation Settings");
            implementationSettingsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            ps.Add(new PropertyRow(implementationSettingsLabel));

            foreach(var implementationData in m_GraphData.activeTargetImplementationDatas)
            {
                bool foldoutActive = true;
                if(!m_ImplementationFoldouts.TryGetValue(implementationData.implementation, out foldoutActive))
                {
                    m_ImplementationFoldouts.Add(implementationData.implementation, foldoutActive);
                }

                var foldout = new Foldout() { text = implementationData.implementation.displayName, value = foldoutActive };
                foldout.RegisterValueChangedCallback(evt => 
                {
                    m_ImplementationFoldouts.Remove(implementationData.implementation);
                    m_ImplementationFoldouts.Add(implementationData.implementation, evt.newValue);
                    foldout.value = evt.newValue;
                    Remove(m_PropertySheet);
                    Rebuild();
                });

                ps.Add(foldout);

                if(foldout.value)
                {
                    implementationData.GetProperties(ps, this);
                }
            }
        }

        // TODO: Currently I use this to force a recompile
        // TODO: How will the inspector actually work? (Sai)
        public void OnChange()
        {
            m_GraphData.UpdateSupportedBlocks();
            m_GraphData.outputNode.Dirty(ModificationScope.Graph);
            Remove(m_PropertySheet);
            Rebuild();
        }
    }
}
