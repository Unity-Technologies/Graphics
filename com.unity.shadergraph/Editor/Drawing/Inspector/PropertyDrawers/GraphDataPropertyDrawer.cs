using System;
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

        ChangeConcretePrecisionCallback m_postChangeConcretePrecisionCallback;

        public void GetPropertyData(ChangeConcretePrecisionCallback changeConcretePrecisionCallback)
        {
            m_postChangeConcretePrecisionCallback = changeConcretePrecisionCallback;
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
                ConcretePrecision.Float,
                out var propertyVisualElement));

            return propertySheet;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI((GraphData)actualObject);
        }
    }
}

