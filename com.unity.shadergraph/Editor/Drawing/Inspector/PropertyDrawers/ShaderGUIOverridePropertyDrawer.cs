using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Data.Interfaces;
using UnityEditor;
using UnityEditor.Graphing.Util;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    public struct ShaderGUIOverrideInfo
    {
        public bool OverrideEnabled;
        public string ShaderGUIOverride;

        public ShaderGUIOverrideInfo(bool overrideEnabled, string shaderGuiOverride)
        {
            this.OverrideEnabled = overrideEnabled;
            this.ShaderGUIOverride = shaderGuiOverride;
        }
    }

    [SGPropertyDrawer(typeof(ShaderGUIOverrideInfo))]
    class ShaderGUIOverridePropertyDrawer : IPropertyDrawer
    {
        internal delegate void ChangeValueCallback(ShaderGUIOverrideInfo newValue);

        private const string k_InvalidShaderGUI =
            "No class named {0} which derives from ShaderGUI was found in this project.";

        private AbstractMaterialNode m_MasterNode;

        // Need to keep a copy here as structs are value types and not reference types
        // Need to keep this value updated or else the lambdas operate on stale data
        private ShaderGUIOverrideInfo m_ShaderGUIOverrideInfo;

        public void GetPropertyData(AbstractMaterialNode masterNode)
        {
            this.m_MasterNode = masterNode;
        }

        private VisualElement CreateGUI(
            ChangeValueCallback valueChangedCallback,
            ShaderGUIOverrideInfo actualObject,
            out VisualElement shaderGUIOverrideField)
        {
            m_ShaderGUIOverrideInfo = actualObject;
            var propertySheet = new PropertySheet();
            shaderGUIOverrideField = null;

            string storedValue = actualObject.ShaderGUIOverride;
            string preferredGUI = GraphUtil.CurrentPipelinePreferredShaderGUI(m_MasterNode as IMasterNode);

            var boolPropertyDrawer = new BoolPropertyDrawer();
            propertySheet.Add(boolPropertyDrawer.CreateGUI(
                newValue =>
                {
                    m_ShaderGUIOverrideInfo.OverrideEnabled = newValue;
                    if (m_ShaderGUIOverrideInfo.OverrideEnabled)
                    {
                        // Display the pipeline's default upon activation, if it has one. Otherwise set up field to display user setting.
                        if (string.IsNullOrEmpty(storedValue) && !string.IsNullOrEmpty(preferredGUI))
                        {
                            ProcessShaderGUIField(preferredGUI);
                        }
                        else
                        {
                            ProcessShaderGUIField(storedValue);
                        }
                    }

                    valueChangedCallback(m_ShaderGUIOverrideInfo);
                    AddWarningIfNeeded(m_ShaderGUIOverrideInfo);
                    // Update the inspector after this value is changed as it needs to trigger a re-draw to expose the ShaderGUI text field
                    this.inspectorUpdateDelegate();
                },
                m_ShaderGUIOverrideInfo.OverrideEnabled,
                "Override ShaderGUI",
                out var boolKeywordField));


            if (actualObject.OverrideEnabled)
            {
                var textPropertyDrawer = new TextPropertyDrawer();
                propertySheet.Add(textPropertyDrawer.CreateGUI(
                    newValue =>
                    {
                        if (m_ShaderGUIOverrideInfo.ShaderGUIOverride == newValue)
                            return;

                        ProcessShaderGUIField(newValue);
                        valueChangedCallback(m_ShaderGUIOverrideInfo);
                        AddWarningIfNeeded(m_ShaderGUIOverrideInfo);
                    },
                    m_ShaderGUIOverrideInfo.ShaderGUIOverride,
                    "ShaderGUI",
                    out var propertyTextField
                ));

                // Reset to default if the value is ever set to null and override is enabled
                if (string.IsNullOrEmpty(storedValue))
                {
                    m_ShaderGUIOverrideInfo.ShaderGUIOverride = preferredGUI;
                }

                var textField = (TextField) propertyTextField;
                textField.value = m_ShaderGUIOverrideInfo.ShaderGUIOverride;
                shaderGUIOverrideField = textField;
                textField.isDelayed = true;
            }
            else
            {
                // Upon disable, set the value back to null (for pipeline switching reasons, among other reasons)
                if (storedValue == preferredGUI)
                {
                    m_ShaderGUIOverrideInfo.ShaderGUIOverride = null;
                    valueChangedCallback(m_ShaderGUIOverrideInfo);
                    AddWarningIfNeeded(m_ShaderGUIOverrideInfo);
                }
            }

            propertySheet.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return propertySheet;
        }

        void ProcessShaderGUIField(string newValue)
        {
            string sanitizedInput = Regex.Replace(newValue, @"(?:[^A-Za-z0-9._])|(?:\s)", "");
            if (HasPreferredGUI() && string.IsNullOrEmpty(sanitizedInput))
            {
                var defaultGUI = GraphUtil.CurrentPipelinePreferredShaderGUI(m_MasterNode as IMasterNode);
                m_ShaderGUIOverrideInfo.ShaderGUIOverride = defaultGUI;
            }
            else
            {
                m_ShaderGUIOverrideInfo.ShaderGUIOverride = sanitizedInput;
            }
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI(
                // Use the setter from the provided property as the callback
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newValue}),
                (ShaderGUIOverrideInfo) propertyInfo.GetValue(actualObject),
                out var textArrayField);
        }

        // Add a warning to the node if the ShaderGUI is not found by Unity.
        private void AddWarningIfNeeded(ShaderGUIOverrideInfo shaderGuiOverrideInfo)
        {
            if (shaderGuiOverrideInfo.OverrideEnabled && shaderGuiOverrideInfo.ShaderGUIOverride != null &&
                !ValidCustomEditorType(shaderGuiOverrideInfo.ShaderGUIOverride))
            {
                m_MasterNode.owner.messageManager?.ClearNodesFromProvider(m_MasterNode,
                    m_MasterNode.ToEnumerable());
                m_MasterNode.owner.messageManager?.AddOrAppendError(m_MasterNode, m_MasterNode.objectId,
                    new ShaderMessage(string.Format(k_InvalidShaderGUI, shaderGuiOverrideInfo.ShaderGUIOverride),
                        ShaderCompilerMessageSeverity.Warning));
            }
            else
            {
                m_MasterNode.owner.messageManager?.ClearNodesFromProvider(m_MasterNode,
                    m_MasterNode.ToEnumerable());
            }
        }

        // Matches what trunk does to extract CustomEditors (Editor/Mono/Inspector/ShaderGUI.cs: ExtractCustomEditorType)
        private bool ValidCustomEditorType(string customEditorName)
        {
            if (string.IsNullOrEmpty(customEditorName))
            {
                if (HasPreferredGUI())
                {
                    return false;
                }

                return true; // No default, so this is valid.
            }

            var unityEditorFullName =
                $"UnityEditor.{customEditorName}"; // For convenience: adding UnityEditor namespace is not needed in the shader
            foreach (var type in TypeCache.GetTypesDerivedFrom<ShaderGUI>())
            {
                if (type.FullName.Equals(customEditorName, StringComparison.Ordinal) ||
                    type.FullName.Equals(unityEditorFullName, StringComparison.Ordinal))
                {
                    return typeof(ShaderGUI).IsAssignableFrom(type);
                }
            }

            return false;
        }

        private bool HasPreferredGUI()
        {
            return !string.IsNullOrEmpty(GraphUtil.CurrentPipelinePreferredShaderGUI(m_MasterNode as IMasterNode));
        }
    }
}
