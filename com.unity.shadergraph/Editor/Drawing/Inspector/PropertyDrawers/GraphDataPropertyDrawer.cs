using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Data.Interfaces;
using Drawing.Views;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(GraphData))]
    public class GraphDataPropertyDrawer : IPropertyDrawer
    {
        public delegate void ChangeConcretePrecisionCallback(ConcretePrecision newValue);
        public delegate void PostTargetSettingsChangedCallback();

        PostTargetSettingsChangedCallback m_postChangeTargetSettingsCallback;
        ChangeConcretePrecisionCallback m_postChangeConcretePrecisionCallback;

        Dictionary<string, bool> m_TargetFoldouts = new Dictionary<string, bool>();

        List<string> userOrderedTargetNameList = new List<string>();

        public void GetPropertyData(
            PostTargetSettingsChangedCallback postChangeValueCallback,
            ChangeConcretePrecisionCallback changeConcretePrecisionCallback)
        {
            m_postChangeTargetSettingsCallback = postChangeValueCallback;
            m_postChangeConcretePrecisionCallback = changeConcretePrecisionCallback;
        }

        VisualElement GetSettings(GraphData graphData, Action onChange)
        {
            var element = new VisualElement() { name = "graphSettings" };

            if(graphData.isSubGraph)
                return element;

            void RegisterActionToUndo(string actionName)
            {
                graphData.owner.RegisterCompleteObjectUndo(actionName);
            }

            // Add Label
            var targetSettingsLabel = new Label("Target Settings");
            targetSettingsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            element.Add(new PropertyRow(targetSettingsLabel));

            var targetNameList = graphData.validTargets.Select(x => x.displayName);

            element.Add(new PropertyRow(new Label("Targets")), (row) =>
                {
                    row.Add(new IMGUIContainer(() => {
                        EditorGUI.BeginChangeCheck();
                        var activeTargetBitmask = EditorGUILayout.MaskField(graphData.activeTargetBitmask, targetNameList.ToArray(), GUILayout.Width(100f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            RegisterActionToUndo("Change active Targets");
                            graphData.activeTargetBitmask = activeTargetBitmask;
                            graphData.UpdateActiveTargets();
                            m_postChangeTargetSettingsCallback();
                        }
                    }));
                });


            // Initialize from the active targets whenever user changes them
            // Is there a way to retain order even with that?
            if (userOrderedTargetNameList.Count != graphData.activeTargets.Count())
            {
                var activeTargetNames = graphData.activeTargets.Select(x => x.displayName);
                userOrderedTargetNameList = activeTargetNames.ToList();
            }

            var reorderableTextListView = new ReorderableListView<string>(userOrderedTargetNameList);
            reorderableTextListView.OnListReorderedCallback += list =>
            {
                userOrderedTargetNameList = (List<string>)list;
                onChange();
            };
            element.Add(reorderableTextListView);

            // Iterate active TargetImplementations
            foreach(var targetName in reorderableTextListView.TextList)
            {
                // Ensure enabled state is being tracked and get value
                bool foldoutActive = true;
                if(!m_TargetFoldouts.TryGetValue(targetName, out foldoutActive))
                {
                    m_TargetFoldouts.Add(targetName, foldoutActive);
                }

                // Create foldout
                var foldout = new Foldout() { text = targetName, value = foldoutActive, name = "foldout" };
                element.Add(foldout);
                foldout.AddToClassList("MainFoldout");
                foldout.RegisterValueChangedCallback(evt =>
                {
                    // Update foldout value and rebuild
                    m_TargetFoldouts[targetName] = evt.newValue;
                    foldout.value = evt.newValue;
                    onChange();
                });

                if(foldout.value)
                {
                    var target = graphData.validTargets.Find(x => x.displayName == targetName);
                    // Get settings for Target
                    var context = new TargetPropertyGUIContext();
                    target.GetPropertiesGUI(ref context, onChange, RegisterActionToUndo);
                    element.Add(context);
                }
            }

            return element;
        }

        internal VisualElement CreateGUI(GraphData graphData)
        {
            var propertySheet = new VisualElement() {name = "graphSettings"};

            if (graphData == null)
            {
                Debug.Log("Attempting to draw something that isn't of type GraphData with a GraphDataPropertyDrawer");
                return propertySheet;
            }

            var enumPropertyDrawer = new EnumPropertyDrawer();
            propertySheet.Add(enumPropertyDrawer.CreateGUI(
                newValue => { m_postChangeConcretePrecisionCallback((ConcretePrecision) newValue); },
                graphData.concretePrecision,
                "Precision",
                ConcretePrecision.Single,
                out var propertyVisualElement));

            propertySheet.Add(GetSettings(graphData, () => this.m_postChangeTargetSettingsCallback()));

            return propertySheet;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI((GraphData)actualObject);
        }
    }
}

