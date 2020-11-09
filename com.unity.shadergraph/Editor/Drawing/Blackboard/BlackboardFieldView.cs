using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardFieldView : BlackboardField, IInspectable
    {
        readonly GraphData m_Graph;
        public GraphData graph => m_Graph;
        internal delegate void BlackBoardCallback();

        ShaderInput m_Input;

        [Inspectable("Shader Input", null)]
        public ShaderInput shaderInput => m_Input;

        static Type s_ContextualMenuManipulator = TypeCache.GetTypesDerivedFrom<MouseManipulator>().FirstOrDefault(t => t.FullName == "UnityEngine.UIElements.ContextualMenuManipulator");

        // Common
        IManipulator m_ResetReferenceMenu;

        private void DirtyNodes(ModificationScope modificationScope = ModificationScope.Node)
        {
            switch(m_Input)
            {
                case AbstractShaderProperty property:
                    var graphEditorView = GetFirstAncestorOfType<GraphEditorView>();
                    if(graphEditorView == null)
                        return;
                    var colorManager = graphEditorView.colorManager;
                    var nodes = graphEditorView.graphView.Query<MaterialNodeView>().ToList();

                    colorManager.SetNodesDirty(nodes);
                    colorManager.UpdateNodeViews(nodes);

                    foreach (var node in graph.GetNodes<PropertyNode>())
                    {
                        node.Dirty(modificationScope);
                    }
                    break;
                case ShaderKeyword keyword:
                    foreach (var node in graph.GetNodes<KeywordNode>())
                    {
                        node.UpdateNode();
                        node.Dirty(modificationScope);
                    }

                    // Cant determine if Sub Graphs contain the keyword so just update them
                    foreach (var node in graph.GetNodes<SubGraphNode>())
                    {
                        node.Dirty(modificationScope);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // When the properties are changed, this delegate is used to trigger an update in the view that represents those properties
        private Action m_inspectorUpdateTrigger;
        private BlackBoardCallback BlackBoardUpdateTrigger;
        private ShaderInputPropertyDrawer.ChangeReferenceNameCallback m_resetReferenceNameTrigger;

        public string inspectorTitle
        {
            get
            {
                switch(m_Input)
                {
                    case AbstractShaderProperty property:
                        return $"{m_Input.displayName} (Property)";
                    case ShaderKeyword keyword:
                        return $"{m_Input.displayName} (Keyword)";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public void InspectorUpdateTrigger()
        {
            if (m_inspectorUpdateTrigger != null)
                m_inspectorUpdateTrigger();
        }

        private void UpdateTypeText()
        {
            if(shaderInput is AbstractShaderProperty asp)
            {
                typeText = asp.GetPropertyTypeString();
            }
        }

        public BlackboardFieldView(GraphData graph, ShaderInput input, BlackBoardCallback updateBlackboardView,
            Texture icon, string text, string typeText) : base(icon, text, typeText)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/ShaderGraphBlackboard"));
            m_Graph = graph;
            m_Input = input;
            this.BlackBoardUpdateTrigger = updateBlackboardView;
            ShaderGraphPreferences.onAllowDeprecatedChanged += UpdateTypeText;
        }

        ~BlackboardFieldView()
        {
            ShaderGraphPreferences.onAllowDeprecatedChanged -= UpdateTypeText;
        }

        public object GetObjectToInspect()
        {
            return shaderInput;
        }

        void UpdateReferenceNameResetMenu()
        {
            if (string.IsNullOrEmpty(m_Input.overrideReferenceName))
            {
                this.RemoveManipulator(m_ResetReferenceMenu);
                m_ResetReferenceMenu = null;
            }
            else
            {
                m_ResetReferenceMenu = (IManipulator)Activator.CreateInstance(s_ContextualMenuManipulator, (Action<ContextualMenuPopulateEvent>)BuildContextualMenu);
                this.AddManipulator(m_ResetReferenceMenu);
            }
        }

        void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Reset Reference", e =>
                {
                    m_Input.overrideReferenceName = null;
                    m_resetReferenceNameTrigger(shaderInput.referenceName);
                    DirtyNodes(ModificationScope.Graph);
                }, DropdownMenuAction.AlwaysEnabled);
        }

#region PropertyDrawers
        public void SupplyDataToPropertyDrawer(IPropertyDrawer propertyDrawer, Action inspectorUpdateDelegate)
        {
            if(propertyDrawer is ShaderInputPropertyDrawer shaderInputPropertyDrawer)
            {
                shaderInputPropertyDrawer.GetPropertyData(
                    m_Graph.isSubGraph,
                    m_Graph,
                    ChangeExposedField,
                    ChangeDisplayNameField,
                    ChangeReferenceNameField,
                    () => m_Graph.ValidateGraph(),
                    () => m_Graph.OnKeywordChanged(),
                    ChangePropertyValue,
                    RegisterPropertyChangeUndo,
                    MarkNodesAsDirty);

                m_inspectorUpdateTrigger = inspectorUpdateDelegate;
                m_resetReferenceNameTrigger = shaderInputPropertyDrawer._resetReferenceNameCallback;

                this.RegisterCallback<DetachFromPanelEvent>(evt => m_inspectorUpdateTrigger());
            }
        }

        void ChangeExposedField(bool newValue)
        {
            m_Input.generatePropertyBlock = newValue;
            icon = m_Input.generatePropertyBlock ? BlackboardProvider.exposedIcon : null;
        }
        void ChangeDisplayNameField(string newValue)
        {
            if (newValue != m_Input.displayName)
            {
                m_Input.displayName = newValue;
                m_Graph.SanitizeGraphInputName(m_Input);
                this.BlackBoardUpdateTrigger();
            }
        }

        void ChangeReferenceNameField(string newValue)
        {
            if (newValue != m_Input.referenceName)
                m_Graph.SanitizeGraphInputReferenceName(m_Input, newValue);

            UpdateReferenceNameResetMenu();
        }

        void RegisterPropertyChangeUndo(string actionName)
        {
            m_Graph.owner.RegisterCompleteObjectUndo(actionName);
        }

        void MarkNodesAsDirty(bool triggerPropertyViewUpdate = false, ModificationScope modificationScope = ModificationScope.Node)
        {
            DirtyNodes(modificationScope);
            if(triggerPropertyViewUpdate)
                m_inspectorUpdateTrigger();
        }

        void ChangePropertyValue(object newValue)
        {
            var property = m_Input as AbstractShaderProperty;
            if(property == null)
                return;

            switch(property)
            {
                case BooleanShaderProperty booleanProperty:
                    booleanProperty.value = ((ToggleData)newValue).isOn;
                    break;
                case Vector1ShaderProperty vector1Property:
                    vector1Property.value = (float) newValue;
                    break;
                case Vector2ShaderProperty vector2Property:
                    vector2Property.value = (Vector2) newValue;
                    break;
                case Vector3ShaderProperty vector3Property:
                    vector3Property.value = (Vector3) newValue;
                    break;
                case Vector4ShaderProperty vector4Property:
                    vector4Property.value = (Vector4) newValue;
                    break;
                case ColorShaderProperty colorProperty:
                    colorProperty.value = (Color) newValue;
                    break;
                case Texture2DShaderProperty texture2DProperty:
                    texture2DProperty.value.texture = (Texture) newValue;
                    break;
                case Texture2DArrayShaderProperty texture2DArrayProperty:
                    texture2DArrayProperty.value.textureArray = (Texture2DArray) newValue;
                    break;
                case Texture3DShaderProperty texture3DProperty:
                    texture3DProperty.value.texture = (Texture3D) newValue;
                    break;
                case CubemapShaderProperty cubemapProperty:
                    cubemapProperty.value.cubemap = (Cubemap) newValue;
                    break;
                case Matrix2ShaderProperty matrix2Property:
                    matrix2Property.value = (Matrix4x4) newValue;
                    break;
                case Matrix3ShaderProperty matrix3Property:
                    matrix3Property.value = (Matrix4x4) newValue;
                    break;
                case Matrix4ShaderProperty matrix4Property:
                    matrix4Property.value = (Matrix4x4) newValue;
                    break;
                case SamplerStateShaderProperty samplerStateProperty:
                    samplerStateProperty.value = (TextureSamplerState) newValue;
                    break;
                case GradientShaderProperty gradientProperty:
                    gradientProperty.value = (Gradient) newValue;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            MarkDirtyRepaint();
        }
#endregion
    }
}
