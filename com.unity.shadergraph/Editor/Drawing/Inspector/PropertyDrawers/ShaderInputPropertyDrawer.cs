using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
        internal delegate  void ChangeDisplayNameCallback(string newValue);
        internal delegate void ChangeReferenceNameCallback(string newValue);
        internal delegate void ChangeValueCallback(object newValue);
        internal delegate void PreChangeValueCallback(string actionName);
        internal delegate void PostChangeValueCallback(bool bTriggerPropertyUpdate = false, ModificationScope modificationScope = ModificationScope.Node);

        // Keyword
        ReorderableList m_KeywordReorderableList;
        int m_KeywordSelectedIndex;

        //Virtual Texture
        ReorderableList m_VTReorderableList;
        int m_VTSelectedIndex;
        private static GUIStyle greyLabel;
        TextField m_VTLayer_Name;
        IdentifierField m_VTLayer_RefName;
        ObjectField m_VTLayer_Texture;
        EnumField m_VTLayer_TextureType;

        // Display Name
        TextField m_DisplayNameField;

        // Reference Name
        TextField m_ReferenceNameField;
        public ChangeReferenceNameCallback _resetReferenceNameCallback;

        ShaderInput shaderInput;

        public ShaderInputPropertyDrawer()
        {
            greyLabel = new GUIStyle(EditorStyles.label);
            greyLabel.normal = new GUIStyleState { textColor = Color.grey };
            greyLabel.focused = new GUIStyleState { textColor = Color.grey };
            greyLabel.hover = new GUIStyleState { textColor = Color.grey };

            // Initializing this callback early on as it is needed by the BlackboardFieldView and PropertyNodeView
            // for binding to the menu action that triggers the reset
            _resetReferenceNameCallback = newValue =>
            {
                m_ReferenceNameField.value = newValue;
                m_ReferenceNameField.RemoveFromClassList("modified");
            };
        }

        GraphData graphData;
        bool isSubGraph { get ; set;  }
        ChangeExposedFieldCallback _exposedFieldChangedCallback;
        ChangeDisplayNameCallback _displayNameChangedCallback;
        ChangeReferenceNameCallback _referenceNameChangedCallback;
        Action _precisionChangedCallback;
        Action _keywordChangedCallback;
        ChangeValueCallback _changeValueCallback;
        PreChangeValueCallback _preChangeValueCallback;
        PostChangeValueCallback _postChangeValueCallback;
        public void GetPropertyData(
            bool isSubGraph,
            GraphData graphData,
            ChangeExposedFieldCallback exposedFieldCallback,
            ChangeDisplayNameCallback displayNameCallback,
            ChangeReferenceNameCallback referenceNameCallback,
            Action precisionChangedCallback,
            Action keywordChangedCallback,
            ChangeValueCallback changeValueCallback,
            PreChangeValueCallback preChangeValueCallback,
            PostChangeValueCallback postChangeValueCallback)
        {
            this.isSubGraph = isSubGraph;
            this.graphData = graphData;
            this._exposedFieldChangedCallback = exposedFieldCallback;
            this._displayNameChangedCallback = displayNameCallback;
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
            BuildPropertyNameLabel(propertySheet);
            BuildDisplayNameField(propertySheet);
            BuildReferenceNameField(propertySheet);
            BuildPropertyFields(propertySheet);
            BuildKeywordFields(propertySheet, shaderInput);
            return propertySheet;
        }

        void BuildPropertyNameLabel(PropertySheet propertySheet)
        {
            if (shaderInput is ShaderKeyword)
                propertySheet.Add(PropertyDrawerUtils.CreateLabel($"Keyword: {shaderInput.displayName}", 0, FontStyle.Bold));
            else
                propertySheet.Add(PropertyDrawerUtils.CreateLabel($"Property: {shaderInput.displayName}", 0, FontStyle.Bold));
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
                propertyToggle.SetEnabled(shaderInput.isExposable && !shaderInput.isAlwaysExposed);
            }
        }

        void BuildDisplayNameField(PropertySheet propertySheet)
        {
            var textPropertyDrawer = new TextPropertyDrawer();
            propertySheet.Add(textPropertyDrawer.CreateGUI(
                null,
                (string)shaderInput.displayName,
                "Name",
                out var propertyVisualElement));

            m_DisplayNameField = (TextField) propertyVisualElement;
            m_DisplayNameField.RegisterValueChangedCallback(
                evt =>
                {
                    this._preChangeValueCallback("Change Display Name");
                    this._displayNameChangedCallback(evt.newValue);

                    if (string.IsNullOrEmpty(shaderInput.displayName))
                        m_DisplayNameField.RemoveFromClassList("modified");
                    else
                        m_DisplayNameField.AddToClassList("modified");

                    this._postChangeValueCallback(true, ModificationScope.Topological);
                });

            if(!string.IsNullOrEmpty(shaderInput.displayName))
                propertyVisualElement.AddToClassList("modified");
            propertyVisualElement.SetEnabled(shaderInput.isRenamable);
            propertyVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyNameReferenceField"));
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

            if (property.sgVersion < property.latestVersion)
            {
                var typeString = property.propertyType.ToString();
                var help = HelpBoxRow.TryGetDeprecatedHelpBoxRow($"{typeString} Property", () => property.ChangeVersion(property.latestVersion));
                if (help != null)
                {
                    propertySheet.Insert(0, help);
                }
            }

            switch (property)
            {
            case IShaderPropertyDrawer propDrawer:
                propDrawer.HandlePropertyField(propertySheet, _preChangeValueCallback, _postChangeValueCallback);
                break;
            case UnityEditor.ShaderGraph.Serialization.MultiJsonInternal.UnknownShaderPropertyType unknownProperty:
                var helpBox = new HelpBoxRow(MessageType.Warning);
                helpBox.Add(new Label("Cannot find the code for this Property, a package may be missing."));
                propertySheet.Add(helpBox);
                break;
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
            case VirtualTextureShaderProperty virtualTextureProperty:
                HandleVirtualTextureProperty(propertySheet, virtualTextureProperty);
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

            BuildExposedField(propertySheet);

            BuildHLSLDeclarationOverrideFields(propertySheet, property);
        }

        static string[] allHLSLDeclarationStrings = new string[]
        {
            "Do Not Declare",       // HLSLDeclaration.DoNotDeclare
            "Global",               // HLSLDeclaration.Global
            "Per Material",         // HLSLDeclaration.UnityPerMaterial
            "Hybrid Per Instance",  // HLSLDeclaration.HybridPerInstance
        };

        void BuildHLSLDeclarationOverrideFields(PropertySheet propertySheet, AbstractShaderProperty property)
        {
            var hlslDecls = Enum.GetValues(typeof(HLSLDeclaration));
            var allowedDecls = new List<HLSLDeclaration>();

            bool anyAllowed = false;
            for (int i = 0; i < hlslDecls.Length; i++)
            {
                HLSLDeclaration decl = (HLSLDeclaration) hlslDecls.GetValue(i);
                var allowed = property.AllowHLSLDeclaration(decl);
                anyAllowed = anyAllowed || allowed;
                if (allowed)
                    allowedDecls.Add(decl);
            }

            if (anyAllowed)
            {
                var propRow = new PropertyRow(PropertyDrawerUtils.CreateLabel("Shader Declaration", 1));
                var popupField = new PopupField<HLSLDeclaration>(
                    allowedDecls,
                    property.GetDefaultHLSLDeclaration(),
                    (h => allHLSLDeclarationStrings[(int) h]),
                    (h => allHLSLDeclarationStrings[(int) h]));

                popupField.RegisterValueChangedCallback(
                    evt =>
                    {
                        this._preChangeValueCallback("Change Override");
                        if (property.hlslDeclarationOverride == evt.newValue)
                            return;
                        property.hlslDeclarationOverride = evt.newValue;
                        this._postChangeValueCallback();
                    });

                propRow.Add(popupField);

                var toggleOverride = new ToggleDataPropertyDrawer();
                propertySheet.Add(toggleOverride.CreateGUI(
                    newValue =>
                    {
                        if (property.overrideHLSLDeclaration == newValue.isOn)
                            return;

                        this._preChangeValueCallback("Override Property Declaration");

                        // add or remove the sub field based on what the toggle is
                        if (newValue.isOn)
                        {
                            // setup initial state based on current state
                            property.hlslDeclarationOverride = property.GetDefaultHLSLDeclaration();
                            property.overrideHLSLDeclaration = newValue.isOn;
                            popupField.value = property.hlslDeclarationOverride;
                            propertySheet.Add(propRow);
                        }
                        else
                        {
                            property.overrideHLSLDeclaration = newValue.isOn;
                            propRow.RemoveFromHierarchy();
                        }

                        this._postChangeValueCallback(false, ModificationScope.Graph);
                    },
                    new ToggleData(property.overrideHLSLDeclaration),
                    "Override Property Declaration", out var overrideToggle));

                // set up initial state
                overrideToggle.SetEnabled(anyAllowed);
                if (property.overrideHLSLDeclaration)
                    propertySheet.Add(propRow);
            }
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
            if (property is Serialization.MultiJsonInternal.UnknownShaderPropertyType)
                precisionField.SetEnabled(false);
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
                        newValue =>
                        {
                            this._preChangeValueCallback("Change property value");
                            this._changeValueCallback((float)newValue);
                            this._postChangeValueCallback();
                        },
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
                        this._postChangeValueCallback(true, ModificationScope.Graph);
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
#region VT reorderable list handler
        void HandleVirtualTextureProperty(PropertySheet propertySheet, VirtualTextureShaderProperty virtualTextureProperty)
        {
            var container = new IMGUIContainer(() => OnVTGUIHandler(virtualTextureProperty)) {name = "ListContainer"};
            AddPropertyRowToSheet(propertySheet, container, "Layers");

            m_VTLayer_Name = new TextField();
            m_VTLayer_Name.isDelayed = true;
            m_VTLayer_Name.RegisterValueChangedCallback(
                evt =>
                {
                    int index = m_VTReorderableList.index;
                    if (index >= 0 && index < m_VTReorderableList.list.Count)
                    {
                        var svt = m_VTReorderableList.list[index] as SerializableVirtualTextureLayer;
                        var otherPropertyNames = graphData.BuildPropertyDisplayNameList(virtualTextureProperty, svt.layerName);
                        var newLayerName = GraphUtil.SanitizeName(otherPropertyNames, "{0} ({1})", evt.newValue);
                        if (newLayerName != svt.layerName)
                        {
                            this._preChangeValueCallback("Change Layer Name");
                            svt.layerName = newLayerName;
                            this._postChangeValueCallback(false, ModificationScope.Graph);
                            m_VTLayer_Name.SetValueWithoutNotify(newLayerName);
                        }
                    }
                });
            AddPropertyRowToSheet(propertySheet, m_VTLayer_Name, "  Layer Name");

            m_VTLayer_RefName = new IdentifierField();
            m_VTLayer_RefName.isDelayed = true;
            m_VTLayer_RefName.RegisterValueChangedCallback(
                evt =>
                {
                    int index = m_VTReorderableList.index;
                    if (index >= 0 && index < m_VTReorderableList.list.Count)
                    {
                        var svt = m_VTReorderableList.list[index] as SerializableVirtualTextureLayer;
                        var otherPropertyRefNames = graphData.BuildPropertyReferenceNameList(virtualTextureProperty, svt.layerName);
                        var newLayerRefName = GraphUtil.SanitizeName(otherPropertyRefNames, "{0}_{1}", evt.newValue);
                        if (newLayerRefName != svt.layerRefName)
                        {
                            this._preChangeValueCallback("Change Layer Ref Name");
                            svt.layerRefName = newLayerRefName;
                            this._postChangeValueCallback(false, ModificationScope.Graph);
                            m_VTLayer_RefName.SetValueWithoutNotify(newLayerRefName);
                        }
                    }
                });
            AddPropertyRowToSheet(propertySheet, m_VTLayer_RefName, "  Layer Reference");

            m_VTLayer_Texture = new ObjectField();
            m_VTLayer_Texture.objectType = typeof(Texture);
            m_VTLayer_Texture.allowSceneObjects = false;
            m_VTLayer_Texture.RegisterValueChangedCallback(
                evt =>
                {
                    this._preChangeValueCallback("Change Layer Texture");

                    int index = m_VTReorderableList.index;
                    if (index >= 0 && index < m_VTReorderableList.list.Count)
                        (m_VTReorderableList.list[index] as SerializableVirtualTextureLayer).layerTexture.texture = (evt.newValue as Texture);

                    this._postChangeValueCallback(false, ModificationScope.Graph);
                });
            AddPropertyRowToSheet(propertySheet, m_VTLayer_Texture, "  Layer Texture");


            m_VTLayer_TextureType = new EnumField();
            m_VTLayer_TextureType.Init(LayerTextureType.Default);
            m_VTLayer_TextureType.RegisterValueChangedCallback(
                evt =>
                {
                    this._preChangeValueCallback("Change Layer Texture Type");

                    int index = m_VTReorderableList.index;
                    if (index >= 0 && index < m_VTReorderableList.list.Count)
                        (m_VTReorderableList.list[index] as SerializableVirtualTextureLayer).layerTextureType = (LayerTextureType) evt.newValue;

                    this._postChangeValueCallback(false, ModificationScope.Graph);
                });
            AddPropertyRowToSheet(propertySheet, m_VTLayer_TextureType, "  Layer Texture Type");
        }

        private void OnVTGUIHandler(VirtualTextureShaderProperty property)
        {
            if(m_VTReorderableList == null)
            {
                VTRecreateList(property);
                VTAddCallbacks(property);

                // update selected entry to reflect default selection
                VTSelectEntry(m_VTReorderableList);
            }

            m_VTReorderableList.index = m_VTSelectedIndex;
            m_VTReorderableList.DoLayoutList();
        }

        internal void VTRecreateList(VirtualTextureShaderProperty property)
        {
            // Create reorderable list from entries
            m_VTReorderableList = new ReorderableList(property.value.layers, typeof(SerializableVirtualTextureLayer), true, true, true, true);
        }

        private void VTAddCallbacks(VirtualTextureShaderProperty property)
        {
            // Draw Header
            m_VTReorderableList.drawHeaderCallback = (Rect rect) =>
            {
                int indent = 14;
                var displayRect = new Rect(rect.x + indent, rect.y, rect.width, rect.height);
                EditorGUI.LabelField(displayRect, "Layer Name");
            };

            // Draw Element
            m_VTReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializableVirtualTextureLayer entry = ((SerializableVirtualTextureLayer)m_VTReorderableList.list[index]);
                EditorGUI.BeginChangeCheck();

                EditorGUI.LabelField(rect, entry.layerName);
            };

            // Element height
            m_VTReorderableList.elementHeightCallback = (int indexer) =>
            {
                return m_VTReorderableList.elementHeight;
            };

            // Can add
            m_VTReorderableList.onCanAddCallback = (ReorderableList list) =>
            {
                return list.count < 4;
            };

            // Can remove
            m_VTReorderableList.onCanRemoveCallback = (ReorderableList list) =>
            {
                return list.count > 1;
            };

            void AddEntryLamda(ReorderableList list) => VTAddEntry(list, property);
            void RemoveEntryLamda(ReorderableList list) => VTRemoveEntry(list, property);
            // Add callback delegates
            m_VTReorderableList.onSelectCallback += VTSelectEntry;
            m_VTReorderableList.onAddCallback += AddEntryLamda;
            m_VTReorderableList.onRemoveCallback += RemoveEntryLamda;
            m_VTReorderableList.onReorderCallback += VTReorderEntries;
        }

        private void VTSelectEntry(ReorderableList list)
        {
            m_VTSelectedIndex = list.index;
            if (m_VTSelectedIndex >= 0 && m_VTSelectedIndex < list.count)
            {
                m_VTLayer_Name.SetEnabled(true);
                m_VTLayer_RefName.SetEnabled(true);
                m_VTLayer_Texture.SetEnabled(true);
                m_VTLayer_TextureType.SetEnabled(true);
                m_VTLayer_Name.SetValueWithoutNotify((list.list[m_VTSelectedIndex] as SerializableVirtualTextureLayer).layerName);
                m_VTLayer_RefName.SetValueWithoutNotify((list.list[m_VTSelectedIndex] as SerializableVirtualTextureLayer).layerRefName);
                m_VTLayer_Texture.SetValueWithoutNotify((list.list[m_VTSelectedIndex] as SerializableVirtualTextureLayer).layerTexture.texture);
                m_VTLayer_TextureType.SetValueWithoutNotify((list.list[m_VTSelectedIndex] as SerializableVirtualTextureLayer).layerTextureType);
            }
            else
            {
                m_VTLayer_Name.SetEnabled(false);
                m_VTLayer_RefName.SetEnabled(false);
                m_VTLayer_Texture.SetEnabled(false);
                m_VTLayer_TextureType.SetEnabled(false);
                m_VTLayer_Name.SetValueWithoutNotify("");
                m_VTLayer_RefName.SetValueWithoutNotify("");
                m_VTLayer_Texture.SetValueWithoutNotify(null);
                m_VTLayer_TextureType.SetValueWithoutNotify(LayerTextureType.Default);
            }
        }

        private void VTAddEntry(ReorderableList list, VirtualTextureShaderProperty property)
        {
            this._preChangeValueCallback("Add Virtual Texture Entry");

            int index = VTGetFirstUnusedID(property);
            if (index <= 0)
                return; // Error has already occured, don't attempt to add this entry.

            var layerName = "Layer" + index.ToString();
            // Add new entry
            property.value.layers.Add(new SerializableVirtualTextureLayer(layerName, new SerializableTexture()));

            // Update Blackboard & Nodes
            //DirtyNodes();
            this._postChangeValueCallback(true);
            m_VTSelectedIndex = list.list.Count - 1;
            //Hack to handle downstream SampleVirtualTextureNodes
            graphData.ValidateGraph();
        }

        // Allowed indicies are 1-MAX_ENUM_ENTRIES
        private int VTGetFirstUnusedID(VirtualTextureShaderProperty property)
        {
            List<int> ususedIDs = new List<int>();

            foreach (SerializableVirtualTextureLayer virtualTextureEntry in property.value.layers)
            {
                ususedIDs.Add(property.value.layers.IndexOf(virtualTextureEntry));
            }

            for (int x = 1; x <= 4; x++)
            {
                if (!ususedIDs.Contains(x))
                    return x;
            }

            Debug.LogError("GetFirstUnusedID: Attempting to get unused ID when all IDs are used.");
            return -1;
        }

        private void VTRemoveEntry(ReorderableList list, VirtualTextureShaderProperty property)
        {
            this._preChangeValueCallback("Remove Virtual Texture Entry");

            // Remove entry
            m_VTSelectedIndex = list.index;
            var selectedEntry = (SerializableVirtualTextureLayer)m_VTReorderableList.list[list.index];
            property.value.layers.Remove(selectedEntry);

            // Update Blackboard & Nodes
            //DirtyNodes();
            this._postChangeValueCallback(true);
            m_VTSelectedIndex = m_VTSelectedIndex >= list.list.Count - 1 ? list.list.Count - 1 : m_VTSelectedIndex;
            //Hack to handle downstream SampleVirtualTextureNodes
            graphData.ValidateGraph();
        }

        private void VTReorderEntries(ReorderableList list)
        {
            this._postChangeValueCallback(true);
        }
#endregion
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

            BuildExposedField(propertySheet);
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

            var container = new IMGUIContainer(() => OnKeywordGUIHandler()) {name = "ListContainer"};
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

        void OnKeywordGUIHandler()
        {
            if(m_KeywordReorderableList == null)
            {
                KeywordRecreateList();
                KeywordAddCallbacks();
            }

            m_KeywordReorderableList.index = m_KeywordSelectedIndex;
            m_KeywordReorderableList.DoLayoutList();
        }

        internal void KeywordRecreateList()
        {
            if(!(shaderInput is ShaderKeyword keyword))
                return;

            // Create reorderable list from entries
            m_KeywordReorderableList = new ReorderableList(keyword.entries, typeof(KeywordEntry), true, true, true, true);
        }

        void KeywordAddCallbacks()
        {
            if(!(shaderInput is ShaderKeyword keyword))
                return;

            // Draw Header
            m_KeywordReorderableList.drawHeaderCallback = (Rect rect) =>
            {
                int indent = 14;
                var displayRect = new Rect(rect.x + indent, rect.y, (rect.width - indent) / 2, rect.height);
                EditorGUI.LabelField(displayRect, "Entry Name");
                var referenceRect = new Rect((rect.x + indent) + (rect.width - indent) / 2, rect.y, (rect.width - indent) / 2, rect.height);
                EditorGUI.LabelField(referenceRect, "Reference Suffix", keyword.isBuiltIn ? EditorStyles.label : greyLabel);
            };

            // Draw Element
            m_KeywordReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                KeywordEntry entry = ((KeywordEntry)m_KeywordReorderableList.list[index]);
                EditorGUI.BeginChangeCheck();

                Rect displayRect = new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight);
                var displayName = EditorGUI.DelayedTextField(displayRect, entry.displayName, EditorStyles.label);
                //This is gross but I cant find any other way to make a DelayedTextField have a tooltip (tried doing the empty label on the field itself and it didnt work either)
                EditorGUI.LabelField(displayRect, new GUIContent("", "Enum keyword display names can only use alphanumeric characters and `_`"));

                var referenceName = EditorGUI.TextField( new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight), entry.referenceName,
                    keyword.isBuiltIn ? EditorStyles.label : greyLabel);

                displayName = GetDuplicateSafeDisplayName(entry.id, displayName);
                referenceName = GetDuplicateSafeReferenceName(entry.id, displayName.ToUpper());

                if(EditorGUI.EndChangeCheck())
                {
                    keyword.entries[index] = new KeywordEntry(index + 1, displayName, referenceName);

                    // Rebuild();
                    this._postChangeValueCallback(true);
                }
            };

            // Element height
            m_KeywordReorderableList.elementHeightCallback = (int indexer) =>
            {
                return m_KeywordReorderableList.elementHeight;
            };

            // Can add
            m_KeywordReorderableList.onCanAddCallback = (ReorderableList list) =>
            {
                return list.count < 8;
            };

            // Can remove
            m_KeywordReorderableList.onCanRemoveCallback = (ReorderableList list) =>
            {
                return list.count > 2;
            };

            // Add callback delegates
            m_KeywordReorderableList.onSelectCallback += KeywordSelectEntry;
            m_KeywordReorderableList.onAddCallback += KeywordAddEntry;
            m_KeywordReorderableList.onRemoveCallback += KeywordRemoveEntry;
            m_KeywordReorderableList.onReorderCallback += KeywordReorderEntries;
        }

        void KeywordSelectEntry(ReorderableList list)
        {
            m_KeywordSelectedIndex = list.index;
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

        void KeywordAddEntry(ReorderableList list)
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
            m_KeywordSelectedIndex = list.list.Count - 1;
        }

        void KeywordRemoveEntry(ReorderableList list)
        {
            if(!(shaderInput is ShaderKeyword keyword))
                return;

            this._preChangeValueCallback("Remove Keyword Entry");

            // Remove entry
            m_KeywordSelectedIndex = list.index;
            var selectedEntry = (KeywordEntry)m_KeywordReorderableList.list[list.index];
            keyword.entries.Remove(selectedEntry);

            // Clamp value within new entry range
            int value = Mathf.Clamp(keyword.value, 0, keyword.entries.Count - 1);
            keyword.value = value;

            // Rebuild();
            this._postChangeValueCallback(true);
            this._keywordChangedCallback();
            m_KeywordSelectedIndex = m_KeywordSelectedIndex >= list.list.Count - 1 ? list.list.Count - 1 : m_KeywordSelectedIndex;
        }

        void KeywordReorderEntries(ReorderableList list)
        {
            this._postChangeValueCallback(true);
        }

        public string GetDuplicateSafeDisplayName(int id, string name)
        {
            name = name.Trim();
            var entryList = m_KeywordReorderableList.list as List<KeywordEntry>;
            return GraphUtil.SanitizeName(entryList.Where(p => p.id != id).Select(p => p.displayName), "{0} ({1})", name, "[^\\w_#() .]");
        }

        public string GetDuplicateSafeReferenceName(int id, string name)
        {
            name = name.Trim();
            var entryList = m_KeywordReorderableList.list as List<KeywordEntry>;
            return GraphUtil.SanitizeName(entryList.Where(p => p.id != id).Select(p => p.referenceName), "{0}_{1}", name, @"(?:[^A-Za-z_0-9_])");
        }
    }
}
