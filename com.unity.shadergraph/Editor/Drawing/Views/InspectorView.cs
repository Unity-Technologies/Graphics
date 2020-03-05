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

        public InspectorView(GraphData graphData)
        {
            name = "inspectorView";
            m_GraphData = graphData;

            // Styles
            style.width = 270;
            style.height = 400;
            style.position = Position.Absolute;
            style.right = 0;
            style.top = 0;
            style.backgroundColor = new Color(.17f, .17f, .17f, 1);

            PropertySheet ps = new PropertySheet();

            CreateTargetSettings(ps);
            ps.Add(new PropertyRow(new Label("")));

            CreateImplementationSettings(ps);
            ps.Add(new PropertyRow(new Label("")));
            
            Add(ps);
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
                            foreach (var node in m_GraphData.GetNodes<AbstractMaterialNode>())
                            {
                                node.Dirty(ModificationScope.Graph);
                            }
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
                            foreach (var node in m_GraphData.GetNodes<AbstractMaterialNode>())
                            {
                                node.Dirty(ModificationScope.Graph);
                            }
                        }
                    }));
                });
        }

        void CreateImplementationSettings(PropertySheet ps)
        {
            var implementationSettingsLabel =  new Label("Implementation Settings");
            implementationSettingsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            ps.Add(new PropertyRow(implementationSettingsLabel));
        }
    }
}
