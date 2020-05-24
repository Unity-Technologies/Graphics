using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Data.Interfaces;
using Drawing.Inspector.PropertyDrawers;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using FloatField = UnityEditor.ShaderGraph.Drawing.FloatField;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(ShaderInput))]
    class ShaderInputPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ChangeExposedFieldCallback(bool newValue);
        internal delegate void ChangeReferenceNameCallback(string newValue);
        internal delegate void ChangeValueCallback(object newValue);
        internal delegate void PreChangeValueCallback(string actionName);
        internal delegate void PostChangeValueCallback(bool bTriggerPropertyUpdate = false, ModificationScope modificationScope = ModificationScope.Node);
        internal delegate void MessageManagerCallback(string message);

        // Keyword
        ReorderableList m_ReorderableList;
        int m_SelectedIndex;

        // Reference Name
        TextField m_ReferenceNameField;
        public ChangeReferenceNameCallback _resetReferenceNameCallback;

        ShaderInput shaderInput;

        bool isSubGraph { get ; set;  }
        ChangeExposedFieldCallback _exposedFieldChangedCallback;
        ChangeReferenceNameCallback _referenceNameChangedCallback;
        Action _precisionChangedCallback;
        Action _keywordChangedCallback;
        ChangeValueCallback _changeValueCallback;
        PreChangeValueCallback _preChangeValueCallback;
        PostChangeValueCallback _postChangeValueCallback;
        public void GetPropertyData(bool isSubGraph,
            ChangeExposedFieldCallback exposedFieldCallback,
            ChangeReferenceNameCallback referenceNameCallback,
            Action precisionChangedCallback,
            Action keywordChangedCallback,
            ChangeValueCallback changeValueCallback,
            PreChangeValueCallback preChangeValueCallback,
            PostChangeValueCallback postChangeValueCallback)
        {
            this.isSubGraph = isSubGraph;
            this._exposedFieldChangedCallback = exposedFieldCallback;
            this._referenceNameChangedCallback = referenceNameCallback;
            this._precisionChangedCallback = precisionChangedCallback;
            this._changeValueCallback = changeValueCallback;
            this._keywordChangedCallback = keywordChangedCallback;
            this._preChangeValueCallback = preChangeValueCallback;
            this._postChangeValueCallback = postChangeValueCallback;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(
            PropertyInfo propertyInfo,
            object actualObject,
            InspectableAttribute attribute)
        {
            var propertySheet = new PropertySheet();
            shaderInput = actualObject as ShaderInput;
            BuildExposedField(propertySheet);
            BuildReferenceNameField(propertySheet);
            BuildPropertyFields(propertySheet);
            BuildKeywordFields(propertySheet, shaderInput);
            return propertySheet;
        }

        void BuildExposedField(PropertySheet propertySheet)
        {
            if(!isSubGraph)
            {
                var toggleDataPropertyDrawer = new ToggleDataPropertyDrawer();
                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    evt =>
                    {
                        this._preChangeValueCallback("Change Exposed Toggle");
                        this._exposedFieldChangedCallback(evt.isOn);
                        this._postChangeValueCallback(false, ModificationScope.Graph);
                    },
                    new ToggleData(shaderInput.generatePropertyBlock),
                    "Exposed",
                    out var propertyToggle));
                propertyToggle.SetEnabled(shaderInput.isExposable);
            }
        }

        void BuildReferenceNameField(PropertySheet propertySheet)
        {
            if (!isSubGraph || shaderInput is ShaderKeyword)
            {
                var textPropertyDrawer = new TextPropertyDrawer();
                propertySheet.Add(textPropertyDrawer.CreateGUI(
                    null,
                    (string)shaderInput.referenceName,
                    "Reference",
                    out var propertyVisualElement));

                m_ReferenceNameField = (TextField) propertyVisualElement;
                m_ReferenceNameField.RegisterValueChangedCallback(
                    evt =>
                    {
                        this._preChangeValueCallback("Change Reference Name");
                        this._referenceNameChangedCallback(evt.newValue);

                        if (string.IsNullOrEmpty(shaderInput.overrideReferenceName))
                            m_ReferenceNameField.RemoveFromClassList("modified");
                        else
                            m_ReferenceNameField.AddToClassList("modified");

                        this._postChangeValueCallback(true, ModificationScope.Graph);
                    });

                _resetReferenceNameCallback = newValue =>
                {
                    m_ReferenceNameField.value = newValue;
                    m_ReferenceNameField.RemoveFromClassList("modified");
                };

                if(!string.IsNullOrEmpty(shaderInput.overrideReferenceName))
                    propertyVisualElement.AddToClassList("modified");
                propertyVisualElement.SetEnabled(shaderInput.isRenamable);
                propertyVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyNameReferenceField"));
            }
        }

        void BuildPropertyFields(PropertySheet propertySheet)
        {
            var property = shaderInput as AbstractShaderProperty;
            if(property == null)
                return;

            switch(property)
            {
            case Vector1ShaderProperty vector1Property:
                HandleVector1ShaderProperty(propertySheet, vector1Property);
                break;
            case Vector2ShaderProperty vector2Property:
                HandleVector2ShaderProperty(propertySheet, vector2Property);
                break;
            case Vector3ShaderProperty vector3Property:
                HandleVector3ShaderProperty(propertySheet, vector3Property);
                break;
            case Vector4ShaderProperty vector4Property:
                HandleVector4ShaderProperty(propertySheet, vector4Property);
                break;
            case ColorShaderProperty colorProperty:
                HandleColorProperty(propertySheet, colorProperty);
                break;
            case Texture2DShaderProperty texture2DProperty:
                HandleTexture2DProperty(propertySheet, texture2DProperty);
                break;
            case Texture2DArrayShaderProperty texture2DArrayProperty:
                HandleTexture2DArrayProperty(propertySheet, texture2DArrayProperty);
                break;
            case Texture3DShaderProperty texture3DProperty:
                HandleTexture3DProperty(propertySheet, texture3DProperty);
                break;
            case CubemapShaderProperty cubemapProperty:
                HandleCubemapProperty(propertySheet, cubemapProperty);
                break;
            case BooleanShaderProperty booleanProperty:
                HandleBooleanProperty(propertySheet, booleanProperty);
                break;
            case Matrix2ShaderProperty matrix2Property:
                HandleMatrix2PropertyField(propertySheet, matrix2Property);
                break;
            case Matrix3ShaderProperty matrix3Property:
                HandleMatrix3PropertyField(propertySheet, matrix3Property);
                break;
            case Matrix4ShaderProperty matrix4Property:
                HandleMatrix4PropertyField(propertySheet, matrix4Property);
                break;
            case SamplerStateShaderProperty samplerStateProperty:
                HandleSamplerStatePropertyField(propertySheet, samplerStateProperty);
                break;
            case GradientShaderProperty gradientProperty:
                HandleGradientPropertyField(propertySheet, gradientProperty);
                break;
            }

            BuildPrecisionField(propertySheet, property);
            if(property.isGpuInstanceable)
                BuildGpuInstancingField(propertySheet, property);
        }

        void BuildPrecisionField(PropertySheet propertySheet, AbstractShaderProperty property)
        {
            var enumPropertyDrawer = new EnumPropertyDrawer();
            propertySheet.Add(enumPropertyDrawer.CreateGUI(newValue =>
                {
                    this._preChangeValueCallback("Change Precision");
                    if (property.precision == (Precision) newValue)
                        return;
                    property.precision = (Precision)newValue;
                    this._precisionChangedCallback();
                    this._postChangeValueCallback();
                }, property.precision, "Precision", Precision.Inherit, out var precisionField));
        }

        void BuildGpuInstancingField(PropertySheet propertySheet, AbstractShaderProperty property)
        {
            var toggleDataPropertyDrawer = new ToggleDataPropertyDrawer();
            propertySheet.Add(toggleDataPropertyDrawer.CreateGUI( newValue =>
            {
                this._preChangeValueCallback("Change Hybrid Instanced Toggle");
                property.gpuInstanced = newValue.isOn;
                this._postChangeValueCallback(false, ModificationScope.Graph);
            }, new ToggleData(property.gpuInstanced), "Hybrid Instanced (experimental)", out var gpuInstancedToggle));

            gpuInstancedToggle.SetEnabled(property.isGpuInstanceable);
        }

        void HandleVector1ShaderProperty(PropertySheet propertySheet, Vector1ShaderProperty vector1ShaderProperty)
        {
            // Handle vector 1 mode parameters
            switch (vector1ShaderProperty.floatType)
            {
                case FloatType.Slider:
                    var floatPropertyDrawer = new FloatPropertyDrawer();
                    // Default field
                    propertySheet.Add(floatPropertyDrawer.CreateGUI(
                        newValue =>
                        {
                            _preChangeValueCallback("Change Property Value");
                            _changeValueCallback(newValue);
                            _postChangeValueCallback();
                        },
                        vector1ShaderProperty.value,
                        "Default",
                        out var propertyFloatField));

                    // Min field
                    propertySheet.Add(floatPropertyDrawer.CreateGUI(
                        newValue =>
                        {
                            if (newValue > vector1ShaderProperty.rangeValues.y)
                                propertySheet.warningContainer.Q<Label>().text = "Min cannot be greater than Max.";
                            _preChangeValueCallback("Change Range Property Minimum");
                            vector1ShaderProperty.rangeValues = new Vector2(newValue, vector1ShaderProperty.rangeValues.y);
                            _postChangeValueCallback();
                        },
                        vector1ShaderProperty.rangeValues.x,
                        "Min",
                        out var minFloatField));

                    // Max field
                    propertySheet.Add(floatPropertyDrawer.CreateGUI(
                        newValue =>
                        {
                            if (newValue < vector1ShaderProperty.rangeValues.x)
                                propertySheet.warningContainer.Q<Label>().text = "Max cannot be lesser than Min.";
                            this._preChangeValueCallback("Change Range Property Maximum");
                            vector1ShaderProperty.rangeValues = new Vector2(vector1ShaderProperty.rangeValues.x, newValue);
                            this._postChangeValueCallback();
                        },
                        vector1ShaderProperty.rangeValues.y,
                        "Max",
                        out var maxFloatField));

                    var defaultField = (FloatField) propertyFloatField;
                    var minField = (FloatField) minFloatField;
                    var maxField = (FloatField) maxFloatField;

                    minField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
                    {
                        propertySheet.warningContainer.Q<Label>().text = "";
                        vector1ShaderProperty.value = Mathf.Max(Mathf.Min(vector1ShaderProperty.value, vector1ShaderProperty.rangeValues.y), vector1ShaderProperty.rangeValues.x);
                        defaultField.value = vector1ShaderProperty.value;
                        _postChangeValueCallback();
                    });

                    maxField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
                    {
                        propertySheet.warningContainer.Q<Label>().text = "";
                        vector1ShaderProperty.value = Mathf.Max(Mathf.Min(vector1ShaderProperty.value, vector1ShaderProperty.rangeValues.y), vector1ShaderProperty.rangeValues.x);
                        defaultField.value = vector1ShaderProperty.value;
                        _postChangeValueCallback();
                    });
                    break;

                case FloatType.Integer:
                    var integerPropertyDrawer = new IntegerPropertyDrawer();
                    // Default field
                    propertySheet.Add(integerPropertyDrawer.CreateGUI(
                        newValue => this._changeValueCallback(newValue),
                        (int)vector1ShaderProperty.value,
                        "Default",
                        out var integerPropertyField));
                    break;

                default:
                    var defaultFloatPropertyDrawer = new FloatPropertyDrawer();
                    // Default field
                    propertySheet.Add(defaultFloatPropertyDrawer.CreateGUI(
                        newValue =>
                        {
                            this._preChangeValueCallback("Change property value");
                            this._changeValueCallback(newValue);
                            this._postChangeValueCallback();
                        },
                        vector1ShaderProperty.value,
                        "Default",
                        out var defaultFloatPropertyField));
                    break;
            }

            if (!isSubGraph)
            {
                var enumPropertyDrawer = new EnumPropertyDrawer();
                propertySheet.Add(enumPropertyDrawer.CreateGUI(
                    newValue =>
                    {
                        this._preChangeValueCallback("Change Vector1 Mode");
                        vector1ShaderProperty.floatType = (FloatType)newValue;
                        this._postChangeValueCallback(true);
                    },
                    vector1ShaderProperty.floatType,
                    "Mode",
                     FloatType.Default,
                    out var modePropertyEnumField));
            }
        }

        void HandleVector2ShaderProperty(PropertySheet propertySheet, Vector2ShaderProperty vector2ShaderProperty)
        {
            var vector2PropertyDrawer = new Vector2PropertyDrawer();
            vector2PropertyDrawer.preValueChangeCallback = () => this._preChangeValueCallback("Change property value");
            vector2PropertyDrawer.postValueChangeCallback = () => this._postChangeValueCallback();

            propertySheet.Add(vector2PropertyDrawer.CreateGUI(
                newValue=> _changeValueCallback(newValue),
                vector2ShaderProperty.value,
                "Default",
                out var propertyVec2Field));
        }

        void HandleVector3ShaderProperty(PropertySheet propertySheet, Vector3ShaderProperty vector3ShaderProperty)
        {
            var vector3PropertyDrawer = new Vector3PropertyDrawer();
            vector3PropertyDrawer.preValueChangeCallback = () => this._preChangeValueCallback("Change property value");
            vector3PropertyDrawer.postValueChangeCallback = () => this._postChangeValueCallback();

            propertySheet.Add(vector3PropertyDrawer.CreateGUI(
                newValue => _changeValueCallback(newValue),
                vector3ShaderProperty.value,
                "Default",
                out var propertyVec3Field));
        }

        void HandleVector4ShaderProperty(PropertySheet propertySheet, Vector4ShaderProperty vector4Property)
        {
            var vector4PropertyDrawer = new Vector4PropertyDrawer();
            vector4PropertyDrawer.preValueChangeCallback = () => this._preChangeValueCallback("Change property value");
            vector4PropertyDrawer.postValueChangeCallback = () => this._postChangeValueCallback();

            propertySheet.Add(vector4PropertyDrawer.CreateGUI(
                newValue => _changeValueCallback(newValue),
                vector4Property.value,
                "Default",
                out var propertyVec4Field));
        }

        void HandleColorProperty(PropertySheet propertySheet, ColorShaderProperty colorProperty)
        {
            var colorPropertyDrawer = new ColorPropertyDrawer();

            propertySheet.Add(colorPropertyDrawer.CreateGUI(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    this._changeValueCallback(newValue);
                    this._postChangeValueCallback();
                },
                colorProperty.value,
                "Default",
                out var propertyColorField));

            var colorField = (ColorField) propertyColorField;
            colorField.hdr = colorProperty.colorMode == ColorMode.HDR;

            if (!isSubGraph)
            {
                var enumPropertyDrawer = new EnumPropertyDrawer();

                propertySheet.Add(enumPropertyDrawer.CreateGUI(
                    newValue =>
                    {
                        this._preChangeValueCallback("Change Color Mode");
                        colorProperty.colorMode = (ColorMode)newValue;
                        this._postChangeValueCallback(true);
                    },
                    colorProperty.colorMode,
                    "Mode",
                    ColorMode.Default,
                    out var colorModeField));
            }
        }

        void HandleTexture2DProperty(PropertySheet propertySheet, Texture2DShaderProperty texture2DProperty)
        {
            var texture2DPropertyDrawer = new Texture2DPropertyDrawer();
            propertySheet.Add(texture2DPropertyDrawer.CreateGUI(
                newValue =>
            {
                this._preChangeValueCallback("Change property value");
                this._changeValueCallback(newValue);
                this._postChangeValueCallback();
            },
                texture2DProperty.value.texture,
                "Default",
                out var texture2DField
            ));

            if (!isSubGraph)
            {
                var enumPropertyDrawer = new EnumPropertyDrawer();
                propertySheet.Add(enumPropertyDrawer.CreateGUI(
                    newValue =>
                {
                    this._preChangeValueCallback("Change Texture mode");
                    if(texture2DProperty.defaultType == (Texture2DShaderProperty.DefaultType)newValue)
                        return;
                    texture2DProperty.defaultType = (Texture2DShaderProperty.DefaultType) newValue;
                    this._postChangeValueCallback(false, ModificationScope.Graph);
                },
                    texture2DProperty.defaultType,
                    "Mode",
                    Texture2DShaderProperty.DefaultType.White,
                    out var textureModeField));

                textureModeField.SetEnabled(texture2DProperty.generatePropertyBlock);
            }
        }

        void HandleTexture2DArrayProperty(PropertySheet propertySheet, Texture2DArrayShaderProperty texture2DArrayProperty)
        {
            var texture2DArrayPropertyDrawer = new Texture2DArrayPropertyDrawer();
            propertySheet.Add(texture2DArrayPropertyDrawer.CreateGUI(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    this._changeValueCallback(newValue);
                    this._postChangeValueCallback();
                },
                texture2DArrayProperty.value.textureArray,
                "Default",
                out var texture2DArrayField
            ));
        }

        void HandleTexture3DProperty(PropertySheet propertySheet, Texture3DShaderProperty texture3DShaderProperty)
        {
            var texture3DPropertyDrawer = new Texture3DPropertyDrawer();
            propertySheet.Add(texture3DPropertyDrawer.CreateGUI(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    this._changeValueCallback(newValue);
                    this._postChangeValueCallback();
                },
                texture3DShaderProperty.value.texture,
                "Default",
                out var texture3DField
            ));
        }

        void HandleCubemapProperty(PropertySheet propertySheet, CubemapShaderProperty cubemapProperty)
        {
            var cubemapPropertyDrawer = new CubemapPropertyDrawer();
            propertySheet.Add(cubemapPropertyDrawer.CreateGUI(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    this._changeValueCallback(newValue);
                    this._postChangeValueCallback();
                },
                cubemapProperty.value.cubemap,
                "Default",
                out var propertyCubemapField
                ));
        }

        void HandleBooleanProperty(PropertySheet propertySheet, BooleanShaderProperty booleanProperty)
        {
            var toggleDataPropertyDrawer = new ToggleDataPropertyDrawer();
            propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    this._changeValueCallback(newValue);
                    this._postChangeValueCallback();
                },
                new ToggleData(booleanProperty.value),
                "Default",
                out var propertyToggle));
        }

        void HandleMatrix2PropertyField(PropertySheet propertySheet, Matrix2ShaderProperty matrix2Property)
        {
            var matrixPropertyDrawer = new MatrixPropertyDrawer
            {
                dimension = MatrixPropertyDrawer.MatrixDimensions.Two,
                PreValueChangeCallback = () => this._preChangeValueCallback("Change property value"),
                PostValueChangeCallback = () => this._postChangeValueCallback(),
                MatrixRowFetchCallback = (rowNumber) => matrix2Property.value.GetRow(rowNumber)
            };

            propertySheet.Add(matrixPropertyDrawer.CreateGUI(
                newValue => { this._changeValueCallback(newValue); },
                matrix2Property.value,
                "Default",
                out var propertyMatrixField));
        }

        void HandleMatrix3PropertyField(PropertySheet propertySheet, Matrix3ShaderProperty matrix3Property)
        {
            var matrixPropertyDrawer = new MatrixPropertyDrawer
            {
                dimension = MatrixPropertyDrawer.MatrixDimensions.Three,
                PreValueChangeCallback = () => this._preChangeValueCallback("Change property value"),
                PostValueChangeCallback = () => this._postChangeValueCallback(),
                MatrixRowFetchCallback = (rowNumber) => matrix3Property.value.GetRow(rowNumber)
            };

            propertySheet.Add(matrixPropertyDrawer.CreateGUI(
                newValue => { this._changeValueCallback(newValue); },
                matrix3Property.value,
                "Default",
                out var propertyMatrixField));
        }

        void HandleMatrix4PropertyField(PropertySheet propertySheet, Matrix4ShaderProperty matrix4Property)
        {
            var matrixPropertyDrawer = new MatrixPropertyDrawer
            {
                dimension = MatrixPropertyDrawer.MatrixDimensions.Four,
                PreValueChangeCallback = () => this._preChangeValueCallback("Change property value"),
                PostValueChangeCallback = () => this._postChangeValueCallback(),
                MatrixRowFetchCallback = (rowNumber) => matrix4Property.value.GetRow(rowNumber)
            };

            propertySheet.Add(matrixPropertyDrawer.CreateGUI(
                newValue => { this._changeValueCallback(newValue); },
                matrix4Property.value,
                "Default",
                out var propertyMatrixField));
        }

        void HandleSamplerStatePropertyField(PropertySheet propertySheet, SamplerStateShaderProperty samplerStateShaderProperty)
        {
            var enumPropertyDrawer = new EnumPropertyDrawer();

            propertySheet.Add(enumPropertyDrawer.CreateGUI(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    TextureSamplerState state = samplerStateShaderProperty.value;
                    state.filter = (TextureSamplerState.FilterMode) newValue;
                    samplerStateShaderProperty.value = state;
                    this._postChangeValueCallback(false, ModificationScope.Graph);
                    this.inspectorUpdateDelegate();
                },
                samplerStateShaderProperty.value.filter,
                "Filter",
                TextureSamplerState.FilterMode.Linear,
                out var filterVisualElement));

            propertySheet.Add(enumPropertyDrawer.CreateGUI(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    TextureSamplerState state = samplerStateShaderProperty.value;
                    state.wrap = (TextureSamplerState.WrapMode) newValue;
                    samplerStateShaderProperty.value = state;
                    this._postChangeValueCallback(false, ModificationScope.Graph);
                    this.inspectorUpdateDelegate();
                },
                samplerStateShaderProperty.value.wrap,
                "Wrap",
                TextureSamplerState.WrapMode.Repeat,
                out var wrapVisualElement));
        }

        void HandleGradientPropertyField(PropertySheet propertySheet, GradientShaderProperty gradientShaderProperty)
        {
            var gradientPropertyDrawer = new GradientPropertyDrawer();
            propertySheet.Add(gradientPropertyDrawer.CreateGUI(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    this._changeValueCallback(newValue);
                    this._postChangeValueCallback();
                },
                gradientShaderProperty.value,
                "Default",
                out var propertyGradientField));
        }

        void BuildKeywordFields(PropertySheet propertySheet, ShaderInput shaderInput)
        {
            var keyword = shaderInput as ShaderKeyword;
            if(keyword == null)
                return;

            var enumPropertyDrawer = new EnumPropertyDrawer();
            propertySheet.Add(enumPropertyDrawer.CreateGUI(
                newValue =>
                {
                    this._preChangeValueCallback("Change Keyword type");
                    if (keyword.keywordDefinition == (KeywordDefinition) newValue)
                        return;
                    keyword.keywordDefinition = (KeywordDefinition) newValue;
                },
                keyword.keywordDefinition,
                "Definition",
                KeywordDefinition.ShaderFeature,
                out var typeField));

            typeField.SetEnabled(!keyword.isBuiltIn);

            if (keyword.keywordDefinition != KeywordDefinition.Predefined)
            {
                propertySheet.Add(enumPropertyDrawer.CreateGUI(
                    newValue =>
                    {
                        this._preChangeValueCallback("Change Keyword scope");
                        if (keyword.keywordScope == (KeywordScope) newValue)
                            return;
                        keyword.keywordScope = (KeywordScope) newValue;
                    },
                    keyword.keywordScope,
                    "Scope",
                    KeywordScope.Local,
                    out var scopeField));

                scopeField.SetEnabled(!keyword.isBuiltIn);
            }

            switch (keyword.keywordType)
            {
                case KeywordType.Boolean:
                    BuildBooleanKeywordField(propertySheet, keyword);
                    break;
                case KeywordType.Enum:
                    BuildEnumKeywordField(propertySheet, keyword);
                    break;
            }
        }

        void BuildBooleanKeywordField(PropertySheet propertySheet, ShaderKeyword keyword)
        {
            var toggleDataPropertyDrawer = new ToggleDataPropertyDrawer();
            propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    keyword.value = newValue.isOn ? 1 : 0;
                    this._postChangeValueCallback(false, ModificationScope.Graph);
                },
                new ToggleData(keyword.value == 1),
                "Default",
                out var boolKeywordField));
        }

        void BuildEnumKeywordField(PropertySheet propertySheet, ShaderKeyword keyword)
        {
            // Clamp value between entry list
            int value = Mathf.Clamp(keyword.value, 0, keyword.entries.Count - 1);

            // Default field
            var field = new PopupField<string>(keyword.entries.Select(x => x.displayName).ToList(), value);
            field.RegisterValueChangedCallback(evt =>
            {
                this._preChangeValueCallback("Change Keyword Value");
                keyword.value = field.index;
                this._postChangeValueCallback(false, ModificationScope.Graph);
            });

            AddPropertyRowToSheet(propertySheet, field, "Default");

            // Entries
            var container = new IMGUIContainer(() => OnGUIHandler()) {name = "ListContainer"};
            AddPropertyRowToSheet(propertySheet, container, "Entries");
            container.SetEnabled(!keyword.isBuiltIn);
        }

        static void AddPropertyRowToSheet(PropertySheet propertySheet, VisualElement control, string labelName)
        {
            propertySheet.Add(new PropertyRow(new Label(labelName)), (row) =>
            {
                row.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
                row.Add(control);
            });
        }

        void OnGUIHandler()
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
            if(!(shaderInput is ShaderKeyword keyword))
                return;

            // Create reorderable list from entries
            m_ReorderableList = new ReorderableList(keyword.entries, typeof(KeywordEntry), true, true, true, true);
        }

        void AddCallbacks()
        {
            if(!(shaderInput is ShaderKeyword keyword))
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

                    // Rebuild();
                    this._postChangeValueCallback(true);
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

        void SelectEntry(ReorderableList list)
        {
            m_SelectedIndex = list.index;
        }

        // Allowed indicies are 1-MAX_ENUM_ENTRIES
        int GetFirstUnusedID()
        {
            if(!(shaderInput is ShaderKeyword keyword))
                return 0;

            List<int> unusedIDs = new List<int>();

            foreach (KeywordEntry keywordEntry in keyword.entries)
            {
                unusedIDs.Add(keywordEntry.id);
            }

            for (int x = 1; x <= KeywordNode.k_MaxEnumEntries; x++)
            {
                if (!unusedIDs.Contains(x))
                    return x;
            }

            Debug.LogError("GetFirstUnusedID: Attempting to get unused ID when all IDs are used.");
            return -1;
        }

        void AddEntry(ReorderableList list)
        {
            if(!(shaderInput is ShaderKeyword keyword))
                return;

            this._preChangeValueCallback("Add Keyword Entry");

            int index = GetFirstUnusedID();
            if (index <= 0)
                return; // Error has already occured, don't attempt to add this entry.

            var displayName = GetDuplicateSafeDisplayName(index, "New");
            var referenceName = GetDuplicateSafeReferenceName(index, "NEW");

            // Add new entry
            keyword.entries.Add(new KeywordEntry(index, displayName, referenceName));

            // Update GUI
            this._postChangeValueCallback(true);
            this._keywordChangedCallback();
            m_SelectedIndex = list.list.Count - 1;
        }

        void RemoveEntry(ReorderableList list)
        {
            if(!(shaderInput is ShaderKeyword keyword))
                return;

            this._preChangeValueCallback("Remove Keyword Entry");

            // Remove entry
            m_SelectedIndex = list.index;
            var selectedEntry = (KeywordEntry)m_ReorderableList.list[list.index];
            keyword.entries.Remove(selectedEntry);

            // Clamp value within new entry range
            int value = Mathf.Clamp(keyword.value, 0, keyword.entries.Count - 1);
            keyword.value = value;

            // Rebuild();
            this._postChangeValueCallback(true);
            this._keywordChangedCallback();
            m_SelectedIndex = m_SelectedIndex >= list.list.Count - 1 ? list.list.Count - 1 : m_SelectedIndex;
        }

        void ReorderEntries(ReorderableList list)
        {
            this._postChangeValueCallback(true);
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
    }
}
