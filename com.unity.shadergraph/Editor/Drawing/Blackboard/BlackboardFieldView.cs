using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Drawing.Inspector;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Internal;
using UnityEditorInternal;

namespace UnityEditor.ShaderGraph.Drawing
{
     abstract class BlackboardFieldView : VisualElement, IInspectable
    {
        readonly BlackboardField m_BlackboardField;
        readonly GraphData m_Graph;
        public GraphData graph => m_Graph;

        ShaderInput m_Input;

        [Inspectable("Shader Input", null)]
        public ShaderInput shaderInput
        {
            get { return m_Input; }
            set
            {
                var property = m_Input as AbstractShaderProperty;
                if(property == null)
                    return;

                switch(property)
                {
                    case Vector1ShaderProperty vector1Property:
                        vector1Property = (Vector1ShaderProperty)value;
                        break;
                    case Vector2ShaderProperty vector2Property:
                        break;
                    case Vector3ShaderProperty vector3Property:
                        break;
                    case Vector4ShaderProperty vector4Property:
                        break;
                    case ColorShaderProperty colorProperty:
                        break;
                    case Texture2DShaderProperty texture2DProperty:
                        break;
                    case Texture2DArrayShaderProperty texture2DArrayProperty:
                        break;
                    case Texture3DShaderProperty texture3DProperty:
                        break;
                    case CubemapShaderProperty cubemapProperty:
                        break;
                    case BooleanShaderProperty booleanProperty:
                        break;
                    case Matrix2ShaderProperty matrix2Property:
                        break;
                    case Matrix3ShaderProperty matrix3Property:
                        break;
                    case Matrix4ShaderProperty matrix4Property:
                        break;
                    case SamplerStateShaderProperty samplerStateProperty:
                        break;
                    case GradientShaderProperty gradientProperty:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                this.MarkDirtyRepaint();
            }
        }
        static Type s_ContextualMenuManipulator = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypesOrNothing()).FirstOrDefault(t => t.FullName == "UnityEngine.UIElements.ContextualMenuManipulator");

        // Common
        TextField m_ReferenceNameField;
        IManipulator m_ResetReferenceMenu;
        EventCallback<KeyDownEvent> m_KeyDownCallback;
        EventCallback<FocusOutEvent> m_FocusOutCallback;
        int m_UndoGroup = -1;

        // Keyword
        private ReorderableList m_ReorderableList;
        private IMGUIContainer m_Container;
        private int m_SelectedIndex;

        // When the properties are changed, this delegate is used to trigger an update in the view that represents those properties
        private Action m_propertyViewUpdateTrigger;

        public string displayName
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

        public BlackboardFieldView(BlackboardField blackboardField, GraphData graph, ShaderInput input)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/ShaderGraphBlackboard"));
            m_BlackboardField = blackboardField;
            m_Graph = graph;
            m_Input = input;

            CreateCallbacks();
        }

        public object GetUnderlyingObject()
        {
            return shaderInput;
        }

        void CreateCallbacks()
        {
            m_KeyDownCallback = new EventCallback<KeyDownEvent>(evt =>
            {
                // Record Undo for input field edit
                if (m_UndoGroup == -1)
                {
                    m_UndoGroup = Undo.GetCurrentGroup();
                    graph.owner.RegisterCompleteObjectUndo("Change property value");
                }
                // Handle escaping input field edit
                if (evt.keyCode == KeyCode.Escape && m_UndoGroup > -1)
                {
                    Undo.RevertAllDownToGroup(m_UndoGroup);
                    m_UndoGroup = -1;
                    evt.StopPropagation();
                }
                // Dont record Undo again until input field is unfocused
                m_UndoGroup++;
                this.MarkDirtyRepaint();
            });

            m_FocusOutCallback = new EventCallback<FocusOutEvent>(evt =>
            {
                // Reset UndoGroup when done editing input field
                m_UndoGroup = -1;
            });
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
                    m_ReferenceNameField.value = m_Input.referenceName;
                    m_ReferenceNameField.RemoveFromClassList("modified");
                    DirtyNodes(ModificationScope.Graph);
                }, DropdownMenuAction.AlwaysEnabled);
        }

        void DirtyNodes(ModificationScope modificationScope = ModificationScope.Node)
        {
            switch(m_Input)
            {
                case AbstractShaderProperty property:
                    foreach (var node in m_Graph.GetNodes<PropertyNode>())
                        node.Dirty(modificationScope);
                    break;
                case ShaderKeyword keyword:
                {
                    foreach (var node in m_Graph.GetNodes<KeywordNode>())
                    {
                        node.UpdateNode();
                        node.Dirty(modificationScope);
                    }

                    // Cant determine if Sub Graphs contain the keyword so just update them
                    foreach (var node in m_Graph.GetNodes<SubGraphNode>())
                    {
                        node.Dirty(modificationScope);
                    }
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

#region PropertyDrawers
        public void SupplyDataToPropertyDrawer(IPropertyDrawer propertyDrawer, Action inspectorUpdateDelegate)
        {
            if(propertyDrawer is ShaderInputPropertyDrawer shaderInputPropertyDrawer)
            {
                shaderInputPropertyDrawer.GetPropertyData(m_Graph.isSubGraph,
                    this.ChangeExposedField,
                    this.ChangeReferenceNameField,
                    () => { m_Graph.OnKeywordChanged(); },
                    this.ChangePropertyValue,
                    this.RegisterPropertyChangeUndo,
                    this.MarkNodesAsDirty);
            }

            this.m_propertyViewUpdateTrigger = inspectorUpdateDelegate;
        }

        public PropertyInfo[] GetPropertyInfo()
        {
            return this.GetType().GetProperties();
        }

        void ChangeExposedField(bool newValue)
        {
            m_Input.generatePropertyBlock = newValue;
            m_BlackboardField.icon = m_Input.generatePropertyBlock ? BlackboardProvider.exposedIcon : null;
        }

        void ChangeReferenceNameField(string newValue)
        {
            m_Graph.owner.RegisterCompleteObjectUndo("Change Reference Name");
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
                this.m_propertyViewUpdateTrigger();
        }

        void ChangePropertyValue(object newValue)
        {
            var property = m_Input as AbstractShaderProperty;
            if(property == null)
                return;

            switch(property)
            {
                case BooleanShaderProperty booleanProperty:
                    booleanProperty.value = (bool) newValue;
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

            this.MarkDirtyRepaint();
        }

#endregion
    }
}
