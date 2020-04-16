using System;
using System.Reflection;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;

namespace Drawing.Inspector
{
    [SGPropertyDrawer(typeof(GraphData))]
    public class GraphDataPropertyDrawer : IPropertyDrawer
    {
        public delegate void ChangeConcretePrecisionCallback(ConcretePrecision newValue);
        public delegate void PostTargetSettingsChangedCallback();

        private PostTargetSettingsChangedCallback m_postChangeTargetSettingsCallback;
        private ChangeConcretePrecisionCallback m_postChangeConcretePrecisionCallback;

        // Targets have persistent data that needs to be maintained between interactions
        // Hence we are storing a reference to this particular property drawer to continue using it
        // private TargetPropertyDrawer m_TargetPropertyDrawer = new TargetPropertyDrawer();

        //Dictionary<Target, bool> m_TargetFoldouts = new Dictionary<Target, bool>();

        public void GetPropertyData(
            PostTargetSettingsChangedCallback postChangeValueCallback,
            ChangeConcretePrecisionCallback changeConcretePrecisionCallback)
        {
            m_postChangeTargetSettingsCallback = postChangeValueCallback;
            m_postChangeConcretePrecisionCallback = changeConcretePrecisionCallback;
        }

        internal VisualElement CreateGUI(GraphData graphData)
        {
            if (graphData == null)
                throw new InvalidCastException(
                    "Attempting to draw something that isn't of type GraphData with a GraphDataPropertyDrawer");

            var propertySheet = new VisualElement() {name = "graphSettings"};

            var enumPropertyDrawer = new EnumPropertyDrawer();
            propertySheet.Add(enumPropertyDrawer.CreateGUI(
                newValue => { m_postChangeConcretePrecisionCallback((ConcretePrecision) newValue); },
                graphData.concretePrecision,
                "Precision",
                ConcretePrecision.Float,
                out var propertyVisualElement));

            /* #TODO: Inspector - Uncomment when Matt lands stacks into master
            // Add Label
            var targetSettingsLabel = new Label("Target Settings");
            targetSettingsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            propertySheet.Add(new PropertyRow(targetSettingsLabel));

            // Target Dropdown Field
            propertySheet.Add(new PropertyRow(new Label("Targets")), (row) =>
            {
                row.Add(new IMGUIContainer(() =>
                {
                    EditorGUI.BeginChangeCheck();
                    graphData.activeTargetBitmask = EditorGUILayout.MaskField(graphData.activeTargetBitmask,
                        graphData.validTargets.Select(x => x.displayName).ToArray(), GUILayout.Width(100f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        graphData.UpdateActiveTargets();
                        m_postChangeTargetSettingsCallback();
                    }
                }));
            });

            // Iterate active TargetImplementations
            foreach(var target in graphData.activeTargets)
            {
                // Ensure enabled state is being tracked and get value
                bool foldoutActive = true;
                if(!m_TargetFoldouts.TryGetValue(target, out foldoutActive))
                {
                    m_TargetFoldouts.Add(target, foldoutActive);
                }

                // Create foldout
                var foldout = new Foldout() { text = target.displayName + " settings", value = foldoutActive };
                propertySheet.Add(foldout);
                foldout.RegisterValueChangedCallback(evt =>
                {
                    // Update foldout value and rebuild
                    m_TargetFoldouts[target] = evt.newValue;
                    foldout.value = evt.newValue;
                    m_postChangeTargetSettingsCallback();
                });

                if(foldout.value)
                {
                    var targetPropertyDrawer = new TargetPropertyDrawer();
                    void TargetSettingsChangedCallback() => m_postChangeTargetSettingsCallback();
                    targetPropertyDrawer.GetPropertyData(TargetSettingsChangedCallback);
                    targetPropertyDrawer.CreateGUI(target, out var targetSettings);
                    propertySheet.Add(targetSettings);
                    InspectorUtils.GatherInspectorContent(propertySheet, target, TargetSettingsChangedCallback);
                }
            }
            */
            return propertySheet;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            return this.CreateGUI((GraphData)actualObject);
        }
    }

    /*[SGPropertyDrawer(typeof(Target))]
    public class TargetPropertyDrawer : IPropertyDrawer
    {
        private Action m_targetSettingsChangedCallback;

        public void GetPropertyData(Action targetSettingsChangedCallback)
        {
            m_targetSettingsChangedCallback = targetSettingsChangedCallback;
        }

        public VisualElement DrawProperty(
            PropertyInfo propertyInfo,
            object actualObject,
            Inspectable attribute)
        {
            var target = (Target) actualObject;
            if (target == null)
                throw new InvalidCastException(
                    "Attempting to draw something that isn't of type Target with a TargetPropertyDrawer");

            return CreateGUI(target, out var visualElement);
        }

        internal VisualElement CreateGUI(Target target, out VisualElement element)
        {
            var visualElement = new VisualElement() {name = "targetSettings"};

            var textArrayPropertyDrawer = new TextArrayPropertyDrawer();

            // SubTargets Dropdown Field
            var subTargetRow = textArrayPropertyDrawer.CreateGUIForField(newValue =>
                {
                    if (Equals(target.activeSubTargetIndex, newValue))
                        return;

                    target.activeSubTargetIndex = newValue;
                    m_targetSettingsChangedCallback();
                },
                target.subTargetNames,
                "Material", out var textArrayField);

            // Set the active subtarget index
            var subTargetField = (PopupField<string>) textArrayField;
            subTargetField.index = target.activeSubTargetIndex;
            visualElement.Add(subTargetRow);

            InspectorUtils.GatherInspectorContent(visualElement, target.activeSubTarget, m_targetSettingsChangedCallback);

            element = visualElement;
            return element;
        }
    }*/
}

