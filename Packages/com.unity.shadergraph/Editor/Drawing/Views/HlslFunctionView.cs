using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Inspector;

namespace UnityEditor.ShaderGraph.Drawing
{
    [Serializable]
    public enum HlslSourceType { File, String };

    internal class HlslFunctionView : VisualElement
    {
        private EnumField m_Type;
        private TextField m_FunctionName;
        private ObjectField m_FunctionSource;
        private Toggle m_FunctionSourceUsePragmas;
        private TextField m_FunctionBody;

        internal HlslFunctionView(CustomFunctionNode node)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/HlslFunctionView"));
            Draw(node);
        }

        private void Draw(CustomFunctionNode node)
        {
            var currentControls = this.Children().ToArray();
            for (int i = 0; i < currentControls.Length; i++)
                currentControls[i].RemoveFromHierarchy();

            m_Type = new EnumField(node.sourceType);
            m_Type.RegisterValueChangedCallback(s =>
            {
                if ((HlslSourceType)s.newValue != node.sourceType)
                {
                    node.owner.owner.RegisterCompleteObjectUndo("Change Function Type");
                    node.sourceType = (HlslSourceType)s.newValue;
                    Draw(node);
                    node.owner.ClearErrorsForNode(node);
                    node.ValidateNode();
                    node.Dirty(ModificationScope.Graph);
                }
            });

            m_FunctionName = new TextField { value = node.functionName, multiline = false };
            m_FunctionName.RegisterCallback<FocusInEvent>(s =>
            {
                if (m_FunctionName.value == CustomFunctionNode.defaultFunctionName)
                    m_FunctionName.value = "";
            });
            m_FunctionName.RegisterCallback<FocusOutEvent>(s =>
            {
                if (m_FunctionName.value == "")
                    m_FunctionName.value = CustomFunctionNode.defaultFunctionName;
                else
                    m_FunctionName.value = NodeUtils.ConvertToValidHLSLIdentifier(m_FunctionName.value);

                if (m_FunctionName.value != node.functionName)
                {
                    node.owner.owner.RegisterCompleteObjectUndo("Change Function Name");
                    node.functionName = m_FunctionName.value;
                    node.ValidateNode();
                    node.Dirty(ModificationScope.Graph);
                    this.GetFirstAncestorOfType<InspectorView>()?.RefreshInspectables();
                }
            });

            string path = AssetDatabase.GUIDToAssetPath(node.functionSource);
            m_FunctionSource = new ObjectField() { value = AssetDatabase.LoadAssetAtPath<ShaderInclude>(path), objectType = typeof(ShaderInclude) };
            m_FunctionSource.RegisterValueChangedCallback(s =>
            {
                long localId;
                string guidString = string.Empty;
                if (s.newValue != null)
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier((ShaderInclude)s.newValue, out guidString, out localId);
                }

                if (guidString != node.functionSource)
                {
                    node.owner.owner.RegisterCompleteObjectUndo("Change Function Source");
                    node.functionSource = guidString;
                    node.ValidateNode();
                    node.Dirty(ModificationScope.Graph);
                }
            });

            m_FunctionSourceUsePragmas = new Toggle
            {
                value = node.functionSourceUsePragmas
            };
            m_FunctionSourceUsePragmas.RegisterValueChangedCallback(s =>
            {
                if (s.newValue != node.functionSourceUsePragmas)
                {
                    node.owner.owner.RegisterCompleteObjectUndo("Change Function Source Pragma Usage");
                    node.functionSourceUsePragmas = s.newValue;
                    node.ValidateNode();
                    node.Dirty(ModificationScope.Graph);
                }
            });

            m_FunctionBody = new TextField { value = node.functionBody, multiline = true };
            m_FunctionBody.AddToClassList("sg-hlsl-function-view__body");
            m_FunctionBody.verticalScrollerVisibility = ScrollerVisibility.Auto;

            var functionBodyScrollView = m_FunctionBody.Q<ScrollView>();
            functionBodyScrollView.mode = ScrollViewMode.VerticalAndHorizontal;
            functionBodyScrollView.horizontalScrollerVisibility = ScrollerVisibility.Auto;

            m_FunctionBody.RegisterCallback<FocusInEvent>(s =>
            {
                if (m_FunctionBody.value == CustomFunctionNode.defaultFunctionBody)
                    m_FunctionBody.value = "";
            });
            m_FunctionBody.RegisterCallback<FocusOutEvent>(s =>
            {
                if (m_FunctionBody.value == "")
                    m_FunctionBody.value = CustomFunctionNode.defaultFunctionBody;

                if (m_FunctionBody.value != node.functionBody)
                {
                    node.owner.owner.RegisterCompleteObjectUndo("Change Function Body");
                    node.functionBody = m_FunctionBody.value;
                    node.ValidateNode();
                    node.Dirty(ModificationScope.Graph);
                }
            });

            VisualElement typeRow = new VisualElement() { name = "Row" };
            {
                typeRow.Add(new Label("Type"));
                typeRow.Add(m_Type);
            }
            Add(typeRow);
            VisualElement nameRow = new VisualElement() { name = "Row" };
            {
                nameRow.Add(new Label("Name"));
                nameRow.Add(m_FunctionName);
            }
            Add(nameRow);
            switch (node.sourceType)
            {
                case HlslSourceType.File:
                    VisualElement sourceRow = new VisualElement() { name = "Row" };
                    {
                        sourceRow.Add(new Label("Source"));
                        sourceRow.Add(m_FunctionSource);
                    }
                    Add(sourceRow);

                    VisualElement functionSourceUsePragmasRow = new VisualElement() { name = "Row" };
                    {
                        functionSourceUsePragmasRow.Add(new Label("Use Pragmas"));
                        functionSourceUsePragmasRow.Add(m_FunctionSourceUsePragmas);
                        functionSourceUsePragmasRow.tooltip = "Determines whether or not Unity pragmas from the included file will be used in the generated shader.";
                    }
                    Add(functionSourceUsePragmasRow);

                    break;
                case HlslSourceType.String:
                    VisualElement bodyRow = new VisualElement() { name = "Row" };
                    {
                        bodyRow.Add(new Label("Body"));
                        bodyRow.style.height = 200;
                        bodyRow.Add(m_FunctionBody);
                    }
                    Add(bodyRow);
                    break;
            }
        }
    }
}
