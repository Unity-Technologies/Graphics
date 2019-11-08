using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
    class BlackboardFieldView : BlackboardField, IInspectable
    {
        GraphData m_GraphData;
        ShaderInput m_Input;

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

        public BlackboardFieldView(GraphData m_GraphDataData, ShaderInput input, Texture icon, string text, string typeText)
            : base (icon, text, typeText)
        {
            m_GraphData = m_GraphDataData;
            m_Input = input;

            CreateCallbacks();
        }

        void CreateCallbacks()
        {
            m_KeyDownCallback = new EventCallback<KeyDownEvent>(evt =>
            {
                // Record Undo for input field edit
                if (m_UndoGroup == -1)
                {
                    m_UndoGroup = Undo.GetCurrentGroup();
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change property value");
                }
                // Handle scaping input field edit
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

        public PropertySheet GetInspectorContent()
        {
            var propertySheet = new PropertySheet();
            {
                BuildExposedField(propertySheet);
                BuildReferenceNameField(propertySheet);
                BuildPropertyFields(propertySheet);
                BuildKeywordFields(propertySheet);
            }
            return propertySheet;
        }

        void AddRow(PropertySheet propertySheet, string labelText, VisualElement control, bool enabled = true)
        {
            control.SetEnabled(enabled);
            propertySheet.Add(new PropertyRow(new Label(labelText)), (row) =>
            {
                row.Add(control); 
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
                m_ResetReferenceMenu = (IManipulator)Activator.CreateInstance(typeof(ContextualMenuManipulator), (Action<ContextualMenuPopulateEvent>)BuildContextualMenu);
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
                    foreach (var node in m_GraphData.GetNodes<PropertyNode>())
                        node.Dirty(modificationScope);
                    break;
                case ShaderKeyword keyword:
                {
                    foreach (var node in m_GraphData.GetNodes<KeywordNode>())
                    {
                        node.UpdateNode();
                        node.Dirty(modificationScope);
                    }

                    // Cant determine if Sub Graphs contain the keyword so just update them
                    foreach (var node in m_GraphData.GetNodes<SubGraphNode>())
                    {
                        node.Dirty(modificationScope);
                    }
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

#region Default Fields
        void BuildExposedField(PropertySheet propertySheet)
        {
            if(!m_GraphData.isSubGraph)
            {
                var exposedToogle = new Toggle();
                exposedToogle.OnToggleChanged(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change Exposed Toggle");
                    m_Input.generatePropertyBlock = evt.newValue;
                    icon = m_Input.generatePropertyBlock ? BlackboardProvider.exposedIcon : null;
                    // Rebuild();
                    DirtyNodes(ModificationScope.Graph);
                });
                exposedToogle.value = m_Input.generatePropertyBlock;
                AddRow(propertySheet, "Exposed", exposedToogle, m_Input.isExposable);
            }
        }

        void BuildReferenceNameField(PropertySheet propertySheet)
        {
            if(!m_GraphData.isSubGraph || m_Input is ShaderKeyword)
            {
                m_ReferenceNameField = new TextField(512, false, false, ' ') { isDelayed = true };
                m_ReferenceNameField.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyNameReferenceField"));
                m_ReferenceNameField.value = m_Input.referenceName;
                m_ReferenceNameField.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change Reference Name");
                    if (m_ReferenceNameField.value != m_Input.referenceName)
                        m_GraphData.SanitizeGraphInputReferenceName(m_Input, evt.newValue);
                    
                    m_ReferenceNameField.value = m_Input.referenceName;
                    if (string.IsNullOrEmpty(m_Input.overrideReferenceName))
                        m_ReferenceNameField.RemoveFromClassList("modified");
                    else
                        m_ReferenceNameField.AddToClassList("modified");

                    // Rebuild();
                    DirtyNodes(ModificationScope.Graph);
                    UpdateReferenceNameResetMenu();
                });
                if (!string.IsNullOrEmpty(m_Input.overrideReferenceName))
                    m_ReferenceNameField.AddToClassList("modified");

                AddRow(propertySheet, "Reference", m_ReferenceNameField, m_Input.isRenamable);
            }
        }
#endregion

#region Property Fields
        void BuildPropertyFields(PropertySheet propertySheet)
        {
            var property = m_Input as AbstractShaderProperty;
            if(property == null)
                return;

            switch(property)
            {
                case Vector1ShaderProperty vector1Property:
                    BuildVector1PropertyField(propertySheet, vector1Property);
                    break;
                case Vector2ShaderProperty vector2Property:
                    BuildVector2PropertyField(propertySheet, vector2Property);
                    break;
                case Vector3ShaderProperty vector3Property:
                    BuildVector3PropertyField(propertySheet, vector3Property);
                    break;
                case Vector4ShaderProperty vector4Property:
                    BuildVector4PropertyField(propertySheet, vector4Property);
                    break;
                case ColorShaderProperty colorProperty:
                    BuildColorPropertyField(propertySheet, colorProperty);
                    break;
                case Texture2DShaderProperty texture2DProperty:
                    BuildTexture2DPropertyField(propertySheet, texture2DProperty);
                    break;
                case Texture2DArrayShaderProperty texture2DArrayProperty:
                    BuildTexture2DArrayPropertyField(propertySheet, texture2DArrayProperty);
                    break;
                case Texture3DShaderProperty texture3DProperty:
                    BuildTexture3DPropertyField(propertySheet, texture3DProperty);
                    break;
                case CubemapShaderProperty cubemapProperty:
                    BuildCubemapPropertyField(propertySheet, cubemapProperty);
                    break;
                case BooleanShaderProperty booleanProperty:
                    BuildBooleanPropertyField(propertySheet, booleanProperty);
                    break;
                case Matrix2ShaderProperty matrix2Property:
                    BuildMatrix2PropertyField(propertySheet, matrix2Property);
                    break;
                case Matrix3ShaderProperty matrix3Property:
                    BuildMatrix3PropertyField(propertySheet, matrix3Property);
                    break;
                case Matrix4ShaderProperty matrix4Property:
                    BuildMatrix4PropertyField(propertySheet, matrix4Property);
                    break;
                case SamplerStateShaderProperty samplerStateProperty:
                    BuildSamplerStatePropertyField(propertySheet, samplerStateProperty);
                    break;
                case GradientShaderProperty gradientProperty:
                    BuildGradientPropertyField(propertySheet, gradientProperty);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            BuildPrecisionField(propertySheet, property);
            BuildGpuInstancingField(propertySheet, property);
        }

        void BuildPrecisionField(PropertySheet propertySheet, AbstractShaderProperty property)
        {
            var precisionField = new EnumField((Enum)property.precision);
            precisionField.RegisterValueChangedCallback(evt =>
            {
                m_GraphData.owner.RegisterCompleteObjectUndo("Change Precision");
                if (property.precision == (Precision)evt.newValue)
                    return;
                
                property.precision = (Precision)evt.newValue;
                m_GraphData.ValidateGraph();
                precisionField.MarkDirtyRepaint();
                DirtyNodes();
            });

            AddRow(propertySheet, "Precision", precisionField);
        }
        
        void BuildGpuInstancingField(PropertySheet propertySheet, AbstractShaderProperty property)
        {
            Toggle gpuInstancedToogle = new Toggle { value = property.gpuInstanced };
            gpuInstancedToogle.OnToggleChanged(evt =>
            {
                m_GraphData.owner.RegisterCompleteObjectUndo("Change Hybrid Instanced Toggle");
                property.gpuInstanced = evt.newValue;
                DirtyNodes(ModificationScope.Graph);
            });

            AddRow(propertySheet, "Hybrid Instanced (experimental)", gpuInstancedToogle, property.isGpuInstanceable);
        }

        void BuildVector1PropertyField(PropertySheet propertySheet, Vector1ShaderProperty property)
        {
            switch (property.floatType)
            {
                case FloatType.Slider:
                    {
                        float min = Mathf.Min(property.value, property.rangeValues.x);
                        float max = Mathf.Max(property.value, property.rangeValues.y);
                        property.rangeValues = new Vector2(min, max);

                        var defaultField = new FloatField { value = property.value };
                        var minField = new FloatField { value = property.rangeValues.x };
                        var maxField = new FloatField { value = property.rangeValues.y };

                        defaultField.RegisterValueChangedCallback(evt =>
                        {
                            property.value = (float)evt.newValue;
                            this.MarkDirtyRepaint();
                        });
                        defaultField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
                        {
                            m_GraphData.owner.RegisterCompleteObjectUndo("Change Property Value");
                            float minValue = Mathf.Min(property.value, property.rangeValues.x);
                            float maxValue = Mathf.Max(property.value, property.rangeValues.y);
                            property.rangeValues = new Vector2(minValue, maxValue);
                            minField.value = minValue;
                            maxField.value = maxValue;
                            DirtyNodes();
                        });
                        minField.RegisterValueChangedCallback(evt =>
                        {
                            m_GraphData.owner.RegisterCompleteObjectUndo("Change Range Property Minimum");
                            property.rangeValues = new Vector2((float)evt.newValue, property.rangeValues.y);
                            DirtyNodes();
                        });
                        minField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
                        {
                            property.value = Mathf.Max(Mathf.Min(property.value, property.rangeValues.y), property.rangeValues.x);
                            defaultField.value = property.value;
                            DirtyNodes();
                        });
                        maxField.RegisterValueChangedCallback(evt =>
                        {
                            m_GraphData.owner.RegisterCompleteObjectUndo("Change Range Property Maximum");
                            property.rangeValues = new Vector2(property.rangeValues.x, (float)evt.newValue);
                            DirtyNodes();
                        });
                        maxField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
                        {
                            property.value = Mathf.Max(Mathf.Min(property.value, property.rangeValues.y), property.rangeValues.x);
                            defaultField.value = property.value;
                            DirtyNodes();
                        });
                        
                        AddRow(propertySheet, "Default", defaultField);
                        AddRow(propertySheet, "Min", minField);
                        AddRow(propertySheet, "Max", maxField);
                    }
                    break;
                case FloatType.Integer:
                    {
                        property.value = (int)property.value;
                        var defaultField = new IntegerField { value = (int)property.value };
                        defaultField.RegisterValueChangedCallback(evt =>
                        {
                            m_GraphData.owner.RegisterCompleteObjectUndo("Change Property Value");
                            property.value = (int)evt.newValue;
                            DirtyNodes();
                        });
                        AddRow(propertySheet, "Default", defaultField);
                    }
                    break;
                default:
                    {
                        var defaultField = new FloatField { value = property.value };
                        defaultField.RegisterValueChangedCallback(evt =>
                        {
                            m_GraphData.owner.RegisterCompleteObjectUndo("Change Property Value");
                            property.value = (float)evt.newValue;
                            DirtyNodes();
                        });
                        AddRow(propertySheet, "Default", defaultField);
                    }
                    break;
            }

            if(!m_GraphData.isSubGraph)
            {
                var modeField = new EnumField(property.floatType);
                modeField.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change Vector1 Mode");
                    property.floatType = (FloatType)evt.newValue;
                    // Rebuild();
                });
                AddRow(propertySheet, "Mode", modeField);
            }
        }
        
        void BuildVector2PropertyField(PropertySheet propertySheet, Vector2ShaderProperty property)
        {
            var field = new Vector2Field { value = property.value };

            field.Q("unity-x-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(m_KeyDownCallback);
            field.Q("unity-x-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(m_FocusOutCallback);
            field.Q("unity-y-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(m_KeyDownCallback);
            field.Q("unity-y-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(m_FocusOutCallback);

            // Called after KeyDownEvent
            field.RegisterValueChangedCallback(evt =>
                {
                    // Only true when setting value via FieldMouseDragger
                    // Undo recorded once per dragger release              
                    if (m_UndoGroup == -1)
                        m_GraphData.owner.RegisterCompleteObjectUndo("Change property value");
                    
                    property.value = evt.newValue;
                    DirtyNodes();
                });
            AddRow(propertySheet, "Default", field);
        }

        void BuildVector3PropertyField(PropertySheet propertySheet, Vector3ShaderProperty property)
        {
            var field = new Vector3Field { value = property.value };

            field.Q("unity-x-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(m_KeyDownCallback);
            field.Q("unity-x-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(m_FocusOutCallback);
            field.Q("unity-y-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(m_KeyDownCallback);
            field.Q("unity-y-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(m_FocusOutCallback);
            field.Q("unity-z-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(m_KeyDownCallback);
            field.Q("unity-z-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(m_FocusOutCallback);

            // Called after KeyDownEvent
            field.RegisterValueChangedCallback(evt =>
                {
                    // Only true when setting value via FieldMouseDragger
                    // Undo recorded once per dragger release              
                    if (m_UndoGroup == -1)
                        m_GraphData.owner.RegisterCompleteObjectUndo("Change property value");
                    
                    property.value = evt.newValue;
                    DirtyNodes();
                });
            AddRow(propertySheet, "Default", field);
        }

        void BuildVector4PropertyField(PropertySheet propertySheet, Vector4ShaderProperty property)
        {
            var field = new Vector4Field { value = property.value };

            field.Q("unity-x-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(m_KeyDownCallback);
            field.Q("unity-x-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(m_FocusOutCallback);
            field.Q("unity-y-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(m_KeyDownCallback);
            field.Q("unity-y-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(m_FocusOutCallback);
            field.Q("unity-z-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(m_KeyDownCallback);
            field.Q("unity-z-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(m_FocusOutCallback);
            field.Q("unity-w-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(m_KeyDownCallback);
            field.Q("unity-w-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(m_FocusOutCallback);

            // Called after KeyDownEvent
            field.RegisterValueChangedCallback(evt =>
                {
                    // Only true when setting value via FieldMouseDragger
                    // Undo recorded once per dragger release              
                    if (m_UndoGroup == -1)
                        m_GraphData.owner.RegisterCompleteObjectUndo("Change property value");
                    
                    property.value = evt.newValue;
                    DirtyNodes();
                });
            AddRow(propertySheet, "Default", field);
        }

        void BuildColorPropertyField(PropertySheet propertySheet, ColorShaderProperty property)
        {
            var colorField = new ColorField { value = property.value, showEyeDropper = false, hdr = property.colorMode == ColorMode.HDR };
            colorField.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change property value");
                    property.value = evt.newValue;
                    DirtyNodes();
                });
            AddRow(propertySheet, "Default", colorField);

            if(!m_GraphData.isSubGraph)
            {
                var colorModeField = new EnumField((Enum)property.colorMode);
                colorModeField.RegisterValueChangedCallback(evt =>
                    {
                        m_GraphData.owner.RegisterCompleteObjectUndo("Change Color Mode");
                        if (property.colorMode == (ColorMode)evt.newValue)
                            return;
                        property.colorMode = (ColorMode)evt.newValue;
                        colorField.hdr = property.colorMode == ColorMode.HDR;
                        colorField.MarkDirtyRepaint();
                        DirtyNodes();
                    });
                AddRow(propertySheet, "Mode", colorModeField);
            }
        }

        void BuildTexture2DPropertyField(PropertySheet propertySheet, Texture2DShaderProperty property)
        {
            var field = new ObjectField { value = property.value.texture, objectType = typeof(Texture) };
            field.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change property value");
                    property.value.texture = (Texture)evt.newValue;
                    DirtyNodes();
                });
            AddRow(propertySheet, "Default", field);

            if(!m_GraphData.isSubGraph)
            {
                var defaultMode = (Enum)Texture2DShaderProperty.DefaultType.Grey;
                var textureMode = property.generatePropertyBlock ? (Enum)property.defaultType : defaultMode;
                var defaultModeField = new EnumField(textureMode);
                    defaultModeField.RegisterValueChangedCallback(evt =>
                        {
                            m_GraphData.owner.RegisterCompleteObjectUndo("Change Texture Mode");
                            if (property.defaultType == (Texture2DShaderProperty.DefaultType)evt.newValue)
                                return;
                            property.defaultType = (Texture2DShaderProperty.DefaultType)evt.newValue;
                            DirtyNodes(ModificationScope.Graph);
                        });
                AddRow(propertySheet, "Mode", defaultModeField, property.generatePropertyBlock);
            }
        }

        void BuildTexture2DArrayPropertyField(PropertySheet propertySheet, Texture2DArrayShaderProperty property)
        {
            var field = new ObjectField { value = property.value.textureArray, objectType = typeof(Texture2DArray) };
            field.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change property value");
                    property.value.textureArray = (Texture2DArray)evt.newValue;
                    DirtyNodes();
                });
            AddRow(propertySheet, "Default", field);
        }

        void BuildTexture3DPropertyField(PropertySheet propertySheet, Texture3DShaderProperty property)
        {
            var field = new ObjectField { value = property.value.texture, objectType = typeof(Texture3D) };
            field.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change property value");
                    property.value.texture = (Texture3D)evt.newValue;
                    DirtyNodes();
                });
            AddRow(propertySheet, "Default", field);
        }

        void BuildCubemapPropertyField(PropertySheet propertySheet, CubemapShaderProperty property)
        {
            var field = new ObjectField { value = property.value.cubemap, objectType = typeof(Cubemap) };
            field.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change property value");
                    property.value.cubemap = (Cubemap)evt.newValue;
                    DirtyNodes();
                });
            AddRow(propertySheet, "Default", field);
        }

        void BuildBooleanPropertyField(PropertySheet propertySheet, BooleanShaderProperty property)
        {
            var field = new Toggle() { value = property.value };
            field.OnToggleChanged(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change property value");
                    property.value = evt.newValue;
                    DirtyNodes();
                });
            AddRow(propertySheet, "Default", field);
        }

        void BuildMatrix2PropertyField(PropertySheet propertySheet, Matrix2ShaderProperty property)
        {
            var row0Field = new Vector2Field { value = property.value.GetRow(0) };
            row0Field.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change Property Value");
                    Vector2 row1 = property.value.GetRow(1);
                    property.value = new Matrix4x4()
                    {
                        m00 = evt.newValue.x,
                        m01 = evt.newValue.y,
                        m02 = 0,
                        m03 = 0,
                        m10 = row1.x,
                        m11 = row1.y,
                        m12 = 0,
                        m13 = 0,
                        m20 = 0,
                        m21 = 0,
                        m22 = 0,
                        m23 = 0,
                        m30 = 0,
                        m31 = 0,
                        m32 = 0,
                        m33 = 0,
                    };
                    DirtyNodes();
                });
            AddRow(propertySheet, "Default", row0Field);

            var row1Field = new Vector2Field { value = property.value.GetRow(1) };
            row1Field.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change Property Value");
                    Vector2 row0 = property.value.GetRow(0);
                    property.value = new Matrix4x4()
                    {
                        m00 = row0.x,
                        m01 = row0.y,
                        m02 = 0,
                        m03 = 0,
                        m10 = evt.newValue.x,
                        m11 = evt.newValue.y,
                        m12 = 0,
                        m13 = 0,
                        m20 = 0,
                        m21 = 0,
                        m22 = 0,
                        m23 = 0,
                        m30 = 0,
                        m31 = 0,
                        m32 = 0,
                        m33 = 0,
                    };
                    DirtyNodes();
                });
            AddRow(propertySheet, "", row1Field);
        }

        void BuildMatrix3PropertyField(PropertySheet propertySheet, Matrix3ShaderProperty property)
        {
            var row0Field = new Vector3Field { value = property.value.GetRow(0) };
            row0Field.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change Property Value");
                    Vector3 row1 = property.value.GetRow(1);
                    Vector3 row2 = property.value.GetRow(2);
                    property.value = new Matrix4x4()
                    {
                        m00 = evt.newValue.x,
                        m01 = evt.newValue.y,
                        m02 = evt.newValue.z,
                        m03 = 0,
                        m10 = row1.x,
                        m11 = row1.y,
                        m12 = row1.z,
                        m13 = 0,
                        m20 = row2.x,
                        m21 = row2.y,
                        m22 = row2.z,
                        m23 = 0,
                        m30 = 0,
                        m31 = 0,
                        m32 = 0,
                        m33 = 0,
                    };
                    DirtyNodes();
                });
            AddRow(propertySheet, "Default", row0Field);

            var row1Field = new Vector3Field { value = property.value.GetRow(1) };
            row1Field.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change Property Value");
                    Vector3 row0 = property.value.GetRow(0);
                    Vector3 row2 = property.value.GetRow(2);
                    property.value = new Matrix4x4()
                    {
                        m00 = row0.x,
                        m01 = row0.y,
                        m02 = row0.z,
                        m03 = 0,
                        m10 = evt.newValue.x,
                        m11 = evt.newValue.y,
                        m12 = evt.newValue.z,
                        m13 = 0,
                        m20 = row2.x,
                        m21 = row2.y,
                        m22 = row2.z,
                        m23 = 0,
                        m30 = 0,
                        m31 = 0,
                        m32 = 0,
                        m33 = 0,
                    };
                    DirtyNodes();
                });
            
            AddRow(propertySheet, "", row1Field);

            var row2Field = new Vector3Field { value = property.value.GetRow(2) };
            row2Field.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change Property Value");
                    Vector3 row0 = property.value.GetRow(0);
                    Vector3 row1 = property.value.GetRow(1);
                    property.value = new Matrix4x4()
                    {
                        m00 = row0.x,
                        m01 = row0.y,
                        m02 = row0.z,
                        m03 = 0,
                        m10 = row1.x,
                        m11 = row1.y,
                        m12 = row1.z,
                        m13 = 0,
                        m20 = evt.newValue.x,
                        m21 = evt.newValue.y,
                        m22 = evt.newValue.z,
                        m23 = 0,
                        m30 = 0,
                        m31 = 0,
                        m32 = 0,
                        m33 = 0,
                    };
                    DirtyNodes();
                });
            AddRow(propertySheet, "", row2Field);
        }

        void BuildMatrix4PropertyField(PropertySheet propertySheet, Matrix4ShaderProperty property)
        {
            var row0Field = new Vector4Field { value = property.value.GetRow(0) };
            row0Field.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change Property Value");
                    Vector4 row1 = property.value.GetRow(1);
                    Vector4 row2 = property.value.GetRow(2);
                    Vector4 row3 = property.value.GetRow(3);
                    property.value = new Matrix4x4()
                    {
                        m00 = evt.newValue.x,
                        m01 = evt.newValue.y,
                        m02 = evt.newValue.z,
                        m03 = evt.newValue.w,
                        m10 = row1.x,
                        m11 = row1.y,
                        m12 = row1.z,
                        m13 = row1.w,
                        m20 = row2.x,
                        m21 = row2.y,
                        m22 = row2.z,
                        m23 = row2.w,
                        m30 = row3.x,
                        m31 = row3.y,
                        m32 = row3.z,
                        m33 = row3.w,
                    };
                    DirtyNodes();
                });
            AddRow(propertySheet, "Default", row0Field);

            var row1Field = new Vector4Field { value = property.value.GetRow(1) };
            row1Field.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change Property Value");
                    Vector4 row0 = property.value.GetRow(0);
                    Vector4 row2 = property.value.GetRow(2);
                    Vector4 row3 = property.value.GetRow(3);
                    property.value = new Matrix4x4()
                    {
                        m00 = row0.x,
                        m01 = row0.y,
                        m02 = row0.z,
                        m03 = row0.w,
                        m10 = evt.newValue.x,
                        m11 = evt.newValue.y,
                        m12 = evt.newValue.z,
                        m13 = evt.newValue.w,
                        m20 = row2.x,
                        m21 = row2.y,
                        m22 = row2.z,
                        m23 = row2.w,
                        m30 = row3.x,
                        m31 = row3.y,
                        m32 = row3.z,
                        m33 = row3.w,
                    };
                    DirtyNodes();
                });
            AddRow(propertySheet, "", row1Field);

            var row2Field = new Vector4Field { value = property.value.GetRow(2) };
            row2Field.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change Property Value");
                    Vector4 row0 = property.value.GetRow(0);
                    Vector4 row1 = property.value.GetRow(1);
                    Vector4 row3 = property.value.GetRow(3);
                    property.value = new Matrix4x4()
                    {
                        m00 = row0.x,
                        m01 = row0.y,
                        m02 = row0.z,
                        m03 = row0.w,
                        m10 = row1.x,
                        m11 = row1.y,
                        m12 = row1.z,
                        m13 = row1.w,
                        m20 = evt.newValue.x,
                        m21 = evt.newValue.y,
                        m22 = evt.newValue.z,
                        m23 = evt.newValue.w,
                        m30 = row3.x,
                        m31 = row3.y,
                        m32 = row3.z,
                        m33 = row3.w,
                    };
                    DirtyNodes();
                });
            AddRow(propertySheet, "", row2Field);

            var row3Field = new Vector4Field { value = property.value.GetRow(3) };
            row3Field.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change Property Value");
                    Vector4 row0 = property.value.GetRow(0);
                    Vector4 row1 = property.value.GetRow(1);
                    Vector4 row2 = property.value.GetRow(2);
                    property.value = new Matrix4x4()
                    {
                        m00 = row0.x,
                        m01 = row0.y,
                        m02 = row0.z,
                        m03 = row0.w,
                        m10 = row1.x,
                        m11 = row1.y,
                        m12 = row1.z,
                        m13 = row1.w,
                        m20 = row2.x,
                        m21 = row2.y,
                        m22 = row2.z,
                        m23 = row2.w,
                        m30 = evt.newValue.x,
                        m31 = evt.newValue.y,
                        m32 = evt.newValue.z,
                        m33 = evt.newValue.w,
                    };
                    DirtyNodes();
                });
            AddRow(propertySheet, "", row3Field);
        }

        void BuildSamplerStatePropertyField(PropertySheet propertySheet, SamplerStateShaderProperty property)
        {
            var filterField = new EnumField(property.value.filter);
            filterField.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change Property Value");
                    TextureSamplerState state = property.value;
                    state.filter = (TextureSamplerState.FilterMode)evt.newValue;
                    property.value = state;
                    // Rebuild();
                    DirtyNodes(ModificationScope.Graph);
                });
            AddRow(propertySheet, "Filter", filterField);

            var wrapField = new EnumField(property.value.wrap);
            wrapField.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change Property Value");
                    TextureSamplerState state = property.value;
                    state.wrap = (TextureSamplerState.WrapMode)evt.newValue;
                    property.value = state;
                    // Rebuild();
                    DirtyNodes(ModificationScope.Graph);
                });
            AddRow(propertySheet, "Wrap", wrapField);
        }

        void BuildGradientPropertyField(PropertySheet propertySheet, GradientShaderProperty property)
        {
            var field = new GradientField { value = property.value };
            field.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change Property Value");
                    property.value = evt.newValue;
                    DirtyNodes();
                });
            AddRow(propertySheet, "Default", field);
        }
#endregion

#region Keyword Fields
        void BuildKeywordFields(PropertySheet propertySheet)
        {
            var keyword = m_Input as ShaderKeyword;
            if(keyword == null)
                return;
    
            // KeywordDefinition
            var keywordDefinitionField = new EnumField((Enum)keyword.keywordDefinition);
            keywordDefinitionField.RegisterValueChangedCallback(evt =>
            {
                m_GraphData.owner.RegisterCompleteObjectUndo("Change Keyword Type");
                if (keyword.keywordDefinition == (KeywordDefinition)evt.newValue)
                    return;
                keyword.keywordDefinition = (KeywordDefinition)evt.newValue;
                // Rebuild();
            });
            AddRow(propertySheet, "Definition", keywordDefinitionField, keyword.isEditable);

            // KeywordScope
            if(keyword.keywordDefinition != KeywordDefinition.Predefined)
            {
                var keywordScopeField = new EnumField((Enum)keyword.keywordScope);
                keywordScopeField.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change Keyword Type");
                    if (keyword.keywordScope == (KeywordScope)evt.newValue)
                        return;
                    keyword.keywordScope = (KeywordScope)evt.newValue;
                });
                AddRow(propertySheet, "Scope", keywordScopeField, keyword.isEditable);
            }

            switch(keyword.keywordType)
            {
                case KeywordType.Boolean:
                    BuildBooleanKeywordField(propertySheet, keyword);
                    break;
                case KeywordType.Enum:
                    BuildEnumKeywordField(propertySheet, keyword);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void BuildBooleanKeywordField(PropertySheet propertySheet, ShaderKeyword keyword)
        {
            // Default field
            var field = new Toggle() { value = keyword.value == 1 };
            field.OnToggleChanged(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change property value");
                    keyword.value = evt.newValue ? 1 : 0;
                    DirtyNodes(ModificationScope.Graph);
                });
            AddRow(propertySheet, "Default", field);
        }

        void BuildEnumKeywordField(PropertySheet propertySheet, ShaderKeyword keyword)
        {
            // Clamp value between entry list
            int value = Mathf.Clamp(keyword.value, 0, keyword.entries.Count - 1);

            // Default field
            var field = new PopupField<string>(keyword.entries.Select(x => x.displayName).ToList(), value);
            field.RegisterValueChangedCallback(evt =>
                {
                    m_GraphData.owner.RegisterCompleteObjectUndo("Change Keyword Value");
                    keyword.value = field.index;
                    DirtyNodes(ModificationScope.Graph);
                });
            AddRow(propertySheet, "Default", field);

            // Entries
            var m_Container = new IMGUIContainer(() => OnGUIHandler ()) { name = "ListContainer" };
            AddRow(propertySheet, "Entries", m_Container, keyword.isEditable);
        }

        private void OnGUIHandler()
        {
            if(m_ReorderableList == null)
            {
                RecreateList();
                AddCallbacks();
            }

            m_ReorderableList.index = m_SelectedIndex;
            m_ReorderableList.DoLayoutList();
        }

        internal void RecreateList()
        {
            if(!(m_Input is ShaderKeyword keyword))
                return;
            
            // Create reorderable list from entries
            m_ReorderableList = new ReorderableList(keyword.entries, typeof(KeywordEntry), true, true, true, true);
        }

        private void AddCallbacks() 
        {
            if(!(m_Input is ShaderKeyword keyword))
                return;
            
            // Draw Header      
            m_ReorderableList.drawHeaderCallback = (Rect rect) => 
            {
                int indent = 14;
                var displayRect = new Rect(rect.x + indent, rect.y, (rect.width - indent) / 2, rect.height);
                EditorGUI.LabelField(displayRect, "Display Name");
                var referenceRect = new Rect((rect.x + indent) + (rect.width - indent) / 2, rect.y, (rect.width - indent) / 2, rect.height);
                EditorGUI.LabelField(referenceRect, "Reference Suffix");
            };

            // Draw Element
            m_ReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => 
            {
                KeywordEntry entry = ((KeywordEntry)m_ReorderableList.list[index]);
                EditorGUI.BeginChangeCheck();
        
                var displayName = EditorGUI.DelayedTextField( new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight), entry.displayName, EditorStyles.label);
                var referenceName = EditorGUI.DelayedTextField( new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight), entry.referenceName, EditorStyles.label);

                displayName = GetDuplicateSafeDisplayName(entry.id, displayName);
                referenceName = GetDuplicateSafeReferenceName(entry.id, referenceName.ToUpper());
        
                if(EditorGUI.EndChangeCheck())
                {
                    keyword.entries[index] = new KeywordEntry(index + 1, displayName, referenceName);
            
                    DirtyNodes();
                    // Rebuild();
                }   
            };

            // Element height
            m_ReorderableList.elementHeightCallback = (int indexer) => 
            {
                return m_ReorderableList.elementHeight;
            };

            // Can add
            m_ReorderableList.onCanAddCallback = (ReorderableList list) => 
            {  
                return list.count < 8;
            };

            // Can remove
            m_ReorderableList.onCanRemoveCallback = (ReorderableList list) => 
            {  
                return list.count > 2;
            };

            // Add callback delegates
            m_ReorderableList.onSelectCallback += SelectEntry;
            m_ReorderableList.onAddCallback += AddEntry;
            m_ReorderableList.onRemoveCallback += RemoveEntry;
            m_ReorderableList.onReorderCallback += ReorderEntries;
        }

        private void SelectEntry(ReorderableList list)
        {
            m_SelectedIndex = list.index;
        }

        private void AddEntry(ReorderableList list)
        {
            if(!(m_Input is ShaderKeyword keyword))
                return;
            
            m_GraphData.owner.RegisterCompleteObjectUndo("Add Keyword Entry");

            var index = list.list.Count + 1;
            var displayName = GetDuplicateSafeDisplayName(index, "New");
            var referenceName = GetDuplicateSafeReferenceName(index, "NEW");

            // Add new entry
            keyword.entries.Add(new KeywordEntry(index, displayName, referenceName));

            // Update GUI
            // Rebuild();
            m_GraphData.OnKeywordChanged();
            m_SelectedIndex = list.list.Count - 1;
        }

        private void RemoveEntry(ReorderableList list)
        {
            if(!(m_Input is ShaderKeyword keyword))
                return;

            m_GraphData.owner.RegisterCompleteObjectUndo("Remove Keyword Entry");

            // Remove entry
            m_SelectedIndex = list.index;
            var selectedEntry = (KeywordEntry)m_ReorderableList.list[list.index];
            keyword.entries.Remove(selectedEntry);

            // Clamp value within new entry range
            int value = Mathf.Clamp(keyword.value, 0, keyword.entries.Count - 1);
            keyword.value = value;

            // Rebuild();
            m_GraphData.OnKeywordChanged();
        }

        private void ReorderEntries(ReorderableList list)
        {
            DirtyNodes();
        }

        public string GetDuplicateSafeDisplayName(int id, string name)
        {
            name = name.Trim();
            var entryList = m_ReorderableList.list as List<KeywordEntry>;
            return GraphUtil.SanitizeName(entryList.Where(p => p.id != id).Select(p => p.displayName), "{0} ({1})", name);
        }

        public string GetDuplicateSafeReferenceName(int id, string name)
        {
            name = name.Trim();
            name = Regex.Replace(name, @"(?:[^A-Za-z_0-9])|(?:\s)", "_");
            var entryList = m_ReorderableList.list as List<KeywordEntry>;
            return GraphUtil.SanitizeName(entryList.Where(p => p.id != id).Select(p => p.referenceName), "{0}_{1}", name);
        }
#endregion
    }
}
