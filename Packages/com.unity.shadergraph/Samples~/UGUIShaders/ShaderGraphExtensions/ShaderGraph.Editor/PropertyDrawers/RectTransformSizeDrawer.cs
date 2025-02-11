using System;
using System.Reflection;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers;
using FloatField = UnityEngine.UIElements.FloatField;

[SGPropertyDrawer(typeof(RectTransformSizeNode))]
public class RectTransformSizeDrawer : IPropertyDrawer, IGetNodePropertyDrawerPropertyData
{
    Action m_setNodesAsDirtyCallback;
    Action m_updateNodeViewsCallback;

    public Action inspectorUpdateDelegate { get; set; }

    public void DisposePropertyDrawer()
    {
        
    }

    public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
    {
        var node = (RectTransformSizeNode)actualObject;
        var propertySheet = new PropertySheet(PropertyDrawerUtils.CreateLabel($"{node.name} Node", 0, FontStyle.Bold));
        PropertyDrawerUtils.AddDefaultNodeProperties(propertySheet, node, m_setNodesAsDirtyCallback, m_updateNodeViewsCallback);

        var previewSizeField = new Vector2Field("Preview Size") { value = node.PreviewSize };
        previewSizeField.RegisterValueChangedCallback(v =>
        {
            m_setNodesAsDirtyCallback?.Invoke();
            node.owner.owner.RegisterCompleteObjectUndo("Change Preview Size");
            node.PreviewSize = v.newValue;
            m_updateNodeViewsCallback?.Invoke();
            node.Dirty(ModificationScope.Graph);
        });
        propertySheet.Add(previewSizeField);

        var previewScaleFactorField = new FloatField("Preview Scale Factor") { value = node.PreviewScaleFactor };
        previewScaleFactorField.RegisterValueChangedCallback(v =>
        {
            m_setNodesAsDirtyCallback?.Invoke();
            node.owner.owner.RegisterCompleteObjectUndo("Change Preview Scale Factor");
            node.PreviewScaleFactor = v.newValue;
            m_updateNodeViewsCallback?.Invoke();
            node.Dirty(ModificationScope.Graph);
        });
        propertySheet.Add(previewScaleFactorField);

        var previewPPUField = new FloatField("Preview Pixels Per Units") { value = node.PreviewPixelsPerUnit };
        previewPPUField.RegisterValueChangedCallback(v =>
        {
            m_setNodesAsDirtyCallback?.Invoke();
            node.owner.owner.RegisterCompleteObjectUndo("Change Preview Pixels Per Units");
            node.PreviewPixelsPerUnit = v.newValue;
            m_updateNodeViewsCallback?.Invoke();
            node.Dirty(ModificationScope.Graph);
        });
        propertySheet.Add(previewPPUField);

        return propertySheet;
    }

    public void GetPropertyData(Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
    {
        m_setNodesAsDirtyCallback = setNodesAsDirtyCallback;
        m_updateNodeViewsCallback = updateNodeViewsCallback;
    }
}
