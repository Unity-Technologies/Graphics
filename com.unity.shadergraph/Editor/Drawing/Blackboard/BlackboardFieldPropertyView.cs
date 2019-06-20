using System;
using System.Linq;
using System.Globalization;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;
using UnityEditor.Experimental.GraphView;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardFieldPropertyView : VisualElement
    {
        readonly BlackboardField m_BlackboardField;
        readonly GraphData m_Graph;

        AbstractShaderProperty m_Property;
        Toggle m_ExposedToogle;
        TextField m_ReferenceNameField;

        static Type s_ContextualMenuManipulator = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypesOrNothing()).FirstOrDefault(t => t.FullName == "UnityEngine.UIElements.ContextualMenuManipulator");

        IManipulator m_ResetReferenceMenu;

        public delegate void OnExposedToggle();
        private OnExposedToggle m_OnExposedToggle;
        int m_UndoGroup = -1;
        
        public BlackboardFieldPropertyView(BlackboardField blackboardField, GraphData graph, AbstractShaderProperty property)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/ShaderGraphBlackboard"));
            m_BlackboardField = blackboardField;
            m_Graph = graph;
            m_Property = property;

            if(!m_Graph.isSubGraph)
            {
                if(property.isExposable)
                {
                    m_ExposedToogle = new Toggle();
                    m_ExposedToogle.OnToggleChanged(evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change Exposed Toggle");
                        if(m_OnExposedToggle != null)
                            m_OnExposedToggle();
                        property.generatePropertyBlock = evt.newValue;
                        if (property.generatePropertyBlock)
                        {
                            m_BlackboardField.icon = BlackboardProvider.exposedIcon;
                        }
                        else
                        {
                            m_BlackboardField.icon = null;
                        }
                        DirtyNodes(ModificationScope.Graph);
                    });
                    m_ExposedToogle.value = property.generatePropertyBlock;
                    AddRow("Exposed", m_ExposedToogle);
                }

                if(property.isExposable)
                {
                    m_ReferenceNameField = new TextField(512, false, false, ' ');
                    m_ReferenceNameField.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyNameReferenceField"));
                    AddRow("Reference", m_ReferenceNameField);
                    m_ReferenceNameField.value = property.referenceName;
                    m_ReferenceNameField.isDelayed = true;
                    m_ReferenceNameField.RegisterValueChangedCallback(newName =>
                        {
                            m_Graph.owner.RegisterCompleteObjectUndo("Change Reference Name");
                            if (m_ReferenceNameField.value != m_Property.referenceName)
                            {
                                string newReferenceName = m_Graph.SanitizePropertyReferenceName(newName.newValue, property.guid);
                                property.overrideReferenceName = newReferenceName;
                            }
                            m_ReferenceNameField.value = property.referenceName;

                            if (string.IsNullOrEmpty(property.overrideReferenceName))
                                m_ReferenceNameField.RemoveFromClassList("modified");
                            else
                                m_ReferenceNameField.AddToClassList("modified");

                            DirtyNodes(ModificationScope.Graph);
                            UpdateReferenceNameResetMenu();
                        });

                    if (!string.IsNullOrEmpty(property.overrideReferenceName))
                        m_ReferenceNameField.AddToClassList("modified");
                }
            }

            // Key Undo callbacks for input fields
            EventCallback<KeyDownEvent> keyDownCallback = new EventCallback<KeyDownEvent>(evt =>
            {
                // Record Undo for input field edit
                if (m_UndoGroup == -1)
                {
                    m_UndoGroup = Undo.GetCurrentGroup();
                    m_Graph.owner.RegisterCompleteObjectUndo("Change property value");
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
            EventCallback<FocusOutEvent> focusOutCallback = new EventCallback<FocusOutEvent>(evt =>
            {
                // Reset UndoGroup when done editing input field
                m_UndoGroup = -1;
            });

            if (property is Vector1ShaderProperty)
            {
                var floatProperty = (Vector1ShaderProperty)property;
                BuildVector1PropertyView(floatProperty);
            }
            else if (property is Vector2ShaderProperty)
            {
                var vectorProperty = (Vector2ShaderProperty)property;
                var field = new Vector2Field { value = vectorProperty.value };

                field.Q("unity-x-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(keyDownCallback);
                field.Q("unity-x-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(focusOutCallback);
                field.Q("unity-y-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(keyDownCallback);
                field.Q("unity-y-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(focusOutCallback);

                // Called after KeyDownEvent
                field.RegisterValueChangedCallback(evt =>
                    {
                        // Only true when setting value via FieldMouseDragger
                        // Undo recorded once per dragger release              
                        if (m_UndoGroup == -1)
                        {
                            m_Graph.owner.RegisterCompleteObjectUndo("Change property value");
                        }
                        vectorProperty.value = evt.newValue;
                        DirtyNodes();
                    });
                AddRow("Default", field);
            }
            else if (property is Vector3ShaderProperty)
            {
                var vectorProperty = (Vector3ShaderProperty)property;
                var field = new Vector3Field { value = vectorProperty.value };

                field.Q("unity-x-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(keyDownCallback);
                field.Q("unity-x-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(focusOutCallback);
                field.Q("unity-y-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(keyDownCallback);
                field.Q("unity-y-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(focusOutCallback);
                field.Q("unity-z-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(keyDownCallback);
                field.Q("unity-z-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(focusOutCallback);

                // Called after KeyDownEvent
                field.RegisterValueChangedCallback(evt =>
                    {
                        // Only true when setting value via FieldMouseDragger
                        // Undo recorded once per dragger release              
                        if (m_UndoGroup == -1)
                        {
                            m_Graph.owner.RegisterCompleteObjectUndo("Change property value");
                        }
                        vectorProperty.value = evt.newValue;
                        DirtyNodes();
                    });
                AddRow("Default", field);
            }
            else if (property is Vector4ShaderProperty)
            {
                var vectorProperty = (Vector4ShaderProperty)property;
                var field = new Vector4Field { value = vectorProperty.value };

                field.Q("unity-x-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(keyDownCallback);
                field.Q("unity-x-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(focusOutCallback);
                field.Q("unity-y-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(keyDownCallback);
                field.Q("unity-y-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(focusOutCallback);
                field.Q("unity-z-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(keyDownCallback);
                field.Q("unity-z-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(focusOutCallback);
                field.Q("unity-w-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(keyDownCallback);
                field.Q("unity-w-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(focusOutCallback);

                // Called after KeyDownEvent
                field.RegisterValueChangedCallback(evt =>
                    {
                        // Only true when setting value via FieldMouseDragger
                        // Undo recorded once per dragger release              
                        if (m_UndoGroup == -1)
                        {
                            m_Graph.owner.RegisterCompleteObjectUndo("Change property value");
                        }
                        vectorProperty.value = evt.newValue;
                        DirtyNodes();
                    });
                AddRow("Default", field);
            }
            else if (property is ColorShaderProperty)
            {
                var colorProperty = (ColorShaderProperty)property;
                var colorField = new ColorField { value = property.defaultValue, showEyeDropper = false, hdr = colorProperty.colorMode == ColorMode.HDR };
                colorField.RegisterValueChangedCallback(evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change property value");
                        colorProperty.value = evt.newValue;
                        DirtyNodes();
                    });
                AddRow("Default", colorField);
                if(!m_Graph.isSubGraph)
                {
                    var colorModeField = new EnumField((Enum)colorProperty.colorMode);
                    colorModeField.RegisterValueChangedCallback(evt =>
                        {
                            m_Graph.owner.RegisterCompleteObjectUndo("Change Color Mode");
                            if (colorProperty.colorMode == (ColorMode)evt.newValue)
                                return;
                            colorProperty.colorMode = (ColorMode)evt.newValue;
                            colorField.hdr = colorProperty.colorMode == ColorMode.HDR;
                            colorField.MarkDirtyRepaint();
                            DirtyNodes();
                        });
                    AddRow("Mode", colorModeField);
                }
            }
            else if (property is TextureShaderProperty)
            {
                var textureProperty = (TextureShaderProperty)property;
                var field = new ObjectField { value = textureProperty.value.texture, objectType = typeof(Texture) };
                field.RegisterValueChangedCallback(evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change property value");
                        textureProperty.value.texture = (Texture)evt.newValue;
                        DirtyNodes();
                    });
                AddRow("Default", field);
                if(!m_Graph.isSubGraph)
                {
                    var defaultModeField = new EnumField((Enum)textureProperty.defaultType);
                    defaultModeField.RegisterValueChangedCallback(evt =>
                        {
                            m_Graph.owner.RegisterCompleteObjectUndo("Change Texture Mode");
                            if (textureProperty.defaultType == (TextureShaderProperty.DefaultType)evt.newValue)
                                return;
                            textureProperty.defaultType = (TextureShaderProperty.DefaultType)evt.newValue;
                            DirtyNodes(ModificationScope.Graph);
                        });
                    void ToggleDefaultModeFieldEnabled()
                    {
                        defaultModeField.SetEnabled(!defaultModeField.enabledSelf);
                    }
                    m_OnExposedToggle += ToggleDefaultModeFieldEnabled;
                    AddRow("Mode", defaultModeField);
                }
            }
            else if (property is Texture2DArrayShaderProperty)
            {
                var textureProperty = (Texture2DArrayShaderProperty)property;
                var field = new ObjectField { value = textureProperty.value.textureArray, objectType = typeof(Texture2DArray) };
                field.RegisterValueChangedCallback(evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change property value");
                        textureProperty.value.textureArray = (Texture2DArray)evt.newValue;
                        DirtyNodes();
                    });
                AddRow("Default", field);
            }
            else if (property is Texture3DShaderProperty)
            {
                var textureProperty = (Texture3DShaderProperty)property;
                var field = new ObjectField { value = textureProperty.value.texture, objectType = typeof(Texture3D) };
                field.RegisterValueChangedCallback(evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change property value");
                        textureProperty.value.texture = (Texture3D)evt.newValue;
                        DirtyNodes();
                    });
                AddRow("Default", field);
            }
            else if (property is CubemapShaderProperty)
            {
                var cubemapProperty = (CubemapShaderProperty)property;
                var field = new ObjectField { value = cubemapProperty.value.cubemap, objectType = typeof(Cubemap) };
                field.RegisterValueChangedCallback(evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change property value");
                        cubemapProperty.value.cubemap = (Cubemap)evt.newValue;
                        DirtyNodes();
                    });
                AddRow("Default", field);
            }
            else if (property is BooleanShaderProperty)
            {
                var booleanProperty = (BooleanShaderProperty)property;
                EventCallback<ChangeEvent<bool>> onBooleanChanged = evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change property value");
                        booleanProperty.value = evt.newValue;
                        DirtyNodes();
                    };
                var field = new Toggle();
                field.OnToggleChanged(onBooleanChanged);
                field.value = booleanProperty.value;
                AddRow("Default", field);
            }
            else if (property is Matrix2ShaderProperty)
            {
                var matrix2Property = (Matrix2ShaderProperty)property;
                var row0Field = new Vector2Field { value = matrix2Property.value.GetRow(0) };
                row0Field.RegisterValueChangedCallback(evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change Property Value");
                        Vector2 row1 = matrix2Property.value.GetRow(1);
                        matrix2Property.value = new Matrix4x4()
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
                AddRow("Default", row0Field);
                var row1Field = new Vector2Field { value = matrix2Property.value.GetRow(1) };
                row1Field.RegisterValueChangedCallback(evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change Property Value");
                        Vector2 row0 = matrix2Property.value.GetRow(0);
                        matrix2Property.value = new Matrix4x4()
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
                AddRow("", row1Field);
            }
            else if (property is Matrix3ShaderProperty)
            {
                var matrix3Property = (Matrix3ShaderProperty)property;
                var row0Field = new Vector3Field { value = matrix3Property.value.GetRow(0) };
                row0Field.RegisterValueChangedCallback(evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change Property Value");
                        Vector3 row1 = matrix3Property.value.GetRow(1);
                        Vector3 row2 = matrix3Property.value.GetRow(2);
                        matrix3Property.value = new Matrix4x4()
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
                AddRow("Default", row0Field);
                var row1Field = new Vector3Field { value = matrix3Property.value.GetRow(1) };
                row1Field.RegisterValueChangedCallback(evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change Property Value");
                        Vector3 row0 = matrix3Property.value.GetRow(0);
                        Vector3 row2 = matrix3Property.value.GetRow(2);
                        matrix3Property.value = new Matrix4x4()
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
                AddRow("", row1Field);
                var row2Field = new Vector3Field { value = matrix3Property.value.GetRow(2) };
                row2Field.RegisterValueChangedCallback(evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change Property Value");
                        Vector3 row0 = matrix3Property.value.GetRow(0);
                        Vector3 row1 = matrix3Property.value.GetRow(1);
                        matrix3Property.value = new Matrix4x4()
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
                AddRow("", row2Field);
            }
            else if (property is Matrix4ShaderProperty)
            {
                var matrix4Property = (Matrix4ShaderProperty)property;
                var row0Field = new Vector4Field { value = matrix4Property.value.GetRow(0) };
                row0Field.RegisterValueChangedCallback(evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change Property Value");
                        Vector4 row1 = matrix4Property.value.GetRow(1);
                        Vector4 row2 = matrix4Property.value.GetRow(2);
                        Vector4 row3 = matrix4Property.value.GetRow(3);
                        matrix4Property.value = new Matrix4x4()
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
                AddRow("Default", row0Field);
                var row1Field = new Vector4Field { value = matrix4Property.value.GetRow(1) };
                row1Field.RegisterValueChangedCallback(evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change Property Value");
                        Vector4 row0 = matrix4Property.value.GetRow(0);
                        Vector4 row2 = matrix4Property.value.GetRow(2);
                        Vector4 row3 = matrix4Property.value.GetRow(3);
                        matrix4Property.value = new Matrix4x4()
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
                AddRow("", row1Field);
                var row2Field = new Vector4Field { value = matrix4Property.value.GetRow(2) };
                row2Field.RegisterValueChangedCallback(evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change Property Value");
                        Vector4 row0 = matrix4Property.value.GetRow(0);
                        Vector4 row1 = matrix4Property.value.GetRow(1);
                        Vector4 row3 = matrix4Property.value.GetRow(3);
                        matrix4Property.value = new Matrix4x4()
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
                AddRow("", row2Field);
                var row3Field = new Vector4Field { value = matrix4Property.value.GetRow(3) };
                row3Field.RegisterValueChangedCallback(evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change Property Value");
                        Vector4 row0 = matrix4Property.value.GetRow(0);
                        Vector4 row1 = matrix4Property.value.GetRow(1);
                        Vector4 row2 = matrix4Property.value.GetRow(2);
                        matrix4Property.value = new Matrix4x4()
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
                AddRow("", row3Field);
            }
            else if (property is SamplerStateShaderProperty)
            {
                var samplerStateProperty = (SamplerStateShaderProperty)property;
                var filterField = new EnumField(samplerStateProperty.value.filter);
                filterField.RegisterValueChangedCallback(evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change Property Value");
                        samplerStateProperty.value.filter = (TextureSamplerState.FilterMode)evt.newValue;
                        DirtyNodes(ModificationScope.Graph);
                    });
                AddRow("Filter", filterField);
                var wrapField = new EnumField(samplerStateProperty.value.wrap);
                wrapField.RegisterValueChangedCallback(evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change Property Value");
                        samplerStateProperty.value.wrap = (TextureSamplerState.WrapMode)evt.newValue;
                        DirtyNodes(ModificationScope.Graph);
                    });
                AddRow("Wrap", wrapField);
            }
            else if (property is GradientShaderProperty)
            {
                var gradientProperty = (GradientShaderProperty)property;
                var field = new GradientField { value = gradientProperty.value };
                field.RegisterValueChangedCallback(evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change Property Value");
                        gradientProperty.value = evt.newValue;
                        DirtyNodes();
                    });
                AddRow("Default", field);
            }

            var precisionField = new EnumField((Enum)property.precision);
            precisionField.RegisterValueChangedCallback(evt =>
            {
                m_Graph.owner.RegisterCompleteObjectUndo("Change Precision");
                if (property.precision == (Precision)evt.newValue)
                    return;
                property.precision = (Precision)evt.newValue;
                m_Graph.ValidateGraph();
                precisionField.MarkDirtyRepaint();
                DirtyNodes();
            });
            AddRow("Precision", precisionField);

            AddToClassList("sgblackboardFieldPropertyView");

            UpdateReferenceNameResetMenu();
        }

        void BuildVector1PropertyView(Vector1ShaderProperty floatProperty)
        {
            VisualElement[] rows = null;

            switch (floatProperty.floatType)
            {
                case FloatType.Slider:
                    {
                        float min = Mathf.Min(floatProperty.value, floatProperty.rangeValues.x);
                        float max = Mathf.Max(floatProperty.value, floatProperty.rangeValues.y);
                        floatProperty.rangeValues = new Vector2(min, max);

                        var defaultField = new FloatField { value = floatProperty.value };
                        var minField = new FloatField { value = floatProperty.rangeValues.x };
                        var maxField = new FloatField { value = floatProperty.rangeValues.y };

                        defaultField.RegisterValueChangedCallback(evt =>
                        {
                            var value = (float)evt.newValue;
                            floatProperty.value = value;
                            this.MarkDirtyRepaint();
                        });
                        defaultField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
                        {
                            m_Graph.owner.RegisterCompleteObjectUndo("Change Property Value");
                            float minValue = Mathf.Min(floatProperty.value, floatProperty.rangeValues.x);
                            float maxValue = Mathf.Max(floatProperty.value, floatProperty.rangeValues.y);
                            floatProperty.rangeValues = new Vector2(minValue, maxValue);
                            minField.value = minValue;
                            maxField.value = maxValue;
                            DirtyNodes();
                        });
                        minField.RegisterValueChangedCallback(evt =>
                        {
                            m_Graph.owner.RegisterCompleteObjectUndo("Change Range Property Minimum");
                            float newValue = (float)evt.newValue;
                            floatProperty.rangeValues = new Vector2(newValue, floatProperty.rangeValues.y);
                            DirtyNodes();
                        });
                        minField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
                        {
                            floatProperty.value = Mathf.Max(Mathf.Min(floatProperty.value, floatProperty.rangeValues.y), floatProperty.rangeValues.x);
                            defaultField.value = floatProperty.value;
                            DirtyNodes();
                        });
                        maxField.RegisterValueChangedCallback(evt =>
                        {
                            m_Graph.owner.RegisterCompleteObjectUndo("Change Range Property Maximum");
                            float newValue = (float)evt.newValue;
                            floatProperty.rangeValues = new Vector2(floatProperty.rangeValues.x, newValue);
                            DirtyNodes();
                        });
                        maxField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
                        {
                            floatProperty.value = Mathf.Max(Mathf.Min(floatProperty.value, floatProperty.rangeValues.y), floatProperty.rangeValues.x);
                            defaultField.value = floatProperty.value;
                            DirtyNodes();
                        });
                        rows = new VisualElement[4];
                        rows[0] = CreateRow("Default", defaultField);
                        rows[2] = CreateRow("Min", minField);
                        rows[3] = CreateRow("Max", maxField);
                    }
                    break;
                case FloatType.Integer:
                    {
                        floatProperty.value = (int)floatProperty.value;
                        var defaultField = new IntegerField { value = (int)floatProperty.value };
                        defaultField.RegisterValueChangedCallback(evt =>
                        {
                            m_Graph.owner.RegisterCompleteObjectUndo("Change Property Value");
                            var value = (int)evt.newValue;
                            floatProperty.value = value;
                            DirtyNodes();
                        });
                        rows = new VisualElement[2];
                        rows[0] = CreateRow("Default", defaultField);
                    }
                    break;
                default:
                    {
                        var defaultField = new FloatField { value = floatProperty.value };
                        defaultField.RegisterValueChangedCallback(evt =>
                        {
                            m_Graph.owner.RegisterCompleteObjectUndo("Change Property Value");
                            var value = (float)evt.newValue;
                            floatProperty.value = value;
                            DirtyNodes();
                        });
                        rows = new VisualElement[2];
                        rows[0] = CreateRow("Default", defaultField);
                    }
                    break;
            }

            if(!m_Graph.isSubGraph)
            {
                var modeField = new EnumField(floatProperty.floatType);
                modeField.RegisterValueChangedCallback(evt =>
                {
                    m_Graph.owner.RegisterCompleteObjectUndo("Change Vector1 Mode");
                    var value = (FloatType)evt.newValue;
                    floatProperty.floatType = value;
                    if (rows != null)
                        RemoveElements(rows);
                    BuildVector1PropertyView(floatProperty);
                    this.MarkDirtyRepaint();
                });
                rows[1] = CreateRow("Mode", modeField);
            }

            if (rows == null)
                return;

            for (int i = 0; i < rows.Length; i++)
                Add(rows[i]);
        }

        void UpdateReferenceNameResetMenu()
        {
            if (string.IsNullOrEmpty(m_Property.overrideReferenceName))
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
                    m_Property.overrideReferenceName = null;
                    m_ReferenceNameField.value = m_Property.referenceName;
                    m_ReferenceNameField.RemoveFromClassList("modified");
                    DirtyNodes(ModificationScope.Graph);
                }, DropdownMenuAction.AlwaysEnabled);
        }

        VisualElement CreateRow(string labelText, VisualElement control)
        {
            VisualElement rowView = new VisualElement();

            rowView.AddToClassList("rowView");

            Label label = new Label(labelText);

            label.AddToClassList("rowViewLabel");
            rowView.Add(label);

            control.AddToClassList("rowViewControl");
            rowView.Add(control);

            return rowView;
        }

        VisualElement AddRow(string labelText, VisualElement control)
        {
            VisualElement rowView = CreateRow(labelText, control);
            Add(rowView);
            return rowView;
        }

        void RemoveElements(VisualElement[] elements)
        {
            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i].parent == this)
                    Remove(elements[i]);
            }
        }

        void DirtyNodes(ModificationScope modificationScope = ModificationScope.Node)
        {
            foreach (var node in m_Graph.GetNodes<PropertyNode>())
                node.Dirty(modificationScope);
        }
    }
}
