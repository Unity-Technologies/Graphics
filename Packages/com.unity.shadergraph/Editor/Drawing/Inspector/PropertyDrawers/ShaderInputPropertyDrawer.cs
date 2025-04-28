using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using FloatField = UnityEditor.ShaderGraph.Drawing.FloatField;
using ContextualMenuManipulator = UnityEngine.UIElements.ContextualMenuManipulator;

using GraphDataStore = UnityEditor.ShaderGraph.DataStore<UnityEditor.ShaderGraph.GraphData>;
using UnityEditor.Rendering;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(ShaderInput))]
    class ShaderInputPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ChangeExposedFieldCallback(bool newValue);
        internal delegate void ChangeValueCallback(object newValue);
        internal delegate void PreChangeValueCallback(string actionName);
        internal delegate void PostChangeValueCallback(bool bTriggerPropertyUpdate = false, ModificationScope modificationScope = ModificationScope.Node);

        // Keyword
        ReorderableList m_KeywordReorderableList;
        int m_KeywordSelectedIndex;

        // Dropdown
        ReorderableList m_DropdownReorderableList;
        ShaderDropdown m_Dropdown;
        int m_DropdownId;
        int m_DropdownSelectedIndex;

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

        TextField m_CustomSlotLabelField;

        // Reference Name
        TextPropertyDrawer m_ReferenceNameDrawer;
        TextField m_ReferenceNameField;

        ShaderInput shaderInput;

        Toggle exposedToggle;
        VisualElement keywordScopeField;
        // Should be provided by the Inspectable
        ShaderInputViewModel m_ViewModel;
        ShaderInputViewModel ViewModel => m_ViewModel;

        const string m_DisplayNameDisallowedPattern = "[^\\w_ ]";
        const string m_ReferenceNameDisallowedPattern = @"(?:[^A-Za-z_0-9_])";
        const string m_EnumRefDisallowedPattern = @"(?:[^A-Za-z_0-9_.])";
        const string m_AttributeValueDisallowedPattern = @"(?:[^A-Za-z_0-9._ ])";

        public ShaderInputPropertyDrawer()
        {
            greyLabel = new GUIStyle(EditorStyles.label);
            greyLabel.normal = new GUIStyleState { textColor = Color.grey };
            greyLabel.focused = new GUIStyleState { textColor = Color.grey };
            greyLabel.hover = new GUIStyleState { textColor = Color.grey };
        }

        GraphData graphData;
        bool isSubGraph { get; set; }
        ChangeExposedFieldCallback _exposedFieldChangedCallback;
        Action _precisionChangedCallback;
        Action _keywordChangedCallback;
        Action _dropdownChangedCallback;
        Action<string> _displayNameChangedCallback;
        Action<string> _referenceNameChangedCallback;
        ChangeValueCallback _changeValueCallback;
        PreChangeValueCallback _preChangeValueCallback;
        PostChangeValueCallback _postChangeValueCallback;

        internal void GetViewModel(ShaderInputViewModel shaderInputViewModel, GraphData inGraphData, PostChangeValueCallback postChangeValueCallback)
        {
            m_ViewModel = shaderInputViewModel;
            this.isSubGraph = m_ViewModel.isSubGraph;
            this.graphData = inGraphData;
            this._keywordChangedCallback = () => graphData.OnKeywordChanged();
            this._dropdownChangedCallback = () => graphData.OnDropdownChanged();
            this._precisionChangedCallback = () => graphData.ValidateGraph();

            this._exposedFieldChangedCallback = newValue =>
            {
                var changeExposedFlagAction = new ChangeExposedFlagAction(shaderInput, newValue);
                ViewModel.requestModelChangeAction(changeExposedFlagAction);
            };

            this._displayNameChangedCallback = newValue =>
            {
                var changeDisplayNameAction = new ChangeDisplayNameAction();
                changeDisplayNameAction.shaderInputReference = shaderInput;
                changeDisplayNameAction.newDisplayNameValue = newValue;
                ViewModel.requestModelChangeAction(changeDisplayNameAction);
            };

            this._changeValueCallback = newValue =>
            {
                var changeDisplayNameAction = new ChangePropertyValueAction();
                changeDisplayNameAction.shaderInputReference = shaderInput;
                changeDisplayNameAction.newShaderInputValue = newValue;
                ViewModel.requestModelChangeAction(changeDisplayNameAction);
            };

            this._referenceNameChangedCallback = newValue =>
            {
                var changeReferenceNameAction = new ChangeReferenceNameAction();
                changeReferenceNameAction.shaderInputReference = shaderInput;
                changeReferenceNameAction.newReferenceNameValue = newValue;
                ViewModel.requestModelChangeAction(changeReferenceNameAction);
            };

            this._preChangeValueCallback = (actionName) => this.graphData.owner.RegisterCompleteObjectUndo(actionName);

            if (shaderInput is AbstractShaderProperty abstractShaderProperty)
            {
                var changePropertyValueAction = new ChangePropertyValueAction();
                changePropertyValueAction.shaderInputReference = abstractShaderProperty;
                this._changeValueCallback = newValue =>
                {
                    changePropertyValueAction.newShaderInputValue = newValue;
                    ViewModel.requestModelChangeAction(changePropertyValueAction);
                };
            }

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
            BuildDropdownFields(propertySheet, shaderInput);
            BuildAttributesFields(propertySheet);
            UpdateEnableState();
            return propertySheet;
        }

        void IPropertyDrawer.DisposePropertyDrawer() { }

        void BuildPropertyNameLabel(PropertySheet propertySheet)
        {
            string prefix;
            if (shaderInput is ShaderKeyword)
                prefix = "Keyword";
            else if (shaderInput is ShaderDropdown)
                prefix = "Dropdown";
            else
                prefix = "Property";

            propertySheet.headerContainer.Add(PropertyDrawerUtils.CreateLabel($"{prefix}: {shaderInput.displayName}", 0, FontStyle.Bold));
        }

        void BuildPerRendererDataField(PropertySheet propertySheet)
        {
            if (!isSubGraph && shaderInput is AbstractShaderProperty property && property.isExposed)
            {
                var toggleDataPropertyDrawer = new ToggleDataPropertyDrawer();
                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    evt =>
                    {
                        this._preChangeValueCallback("Change PerRendererData Toggle");
                        property.PerRendererData = evt.isOn;
                        this._postChangeValueCallback(true, ModificationScope.Graph);
                    },
                    new ToggleData(property.PerRendererData),
                    "Read Only",
                    out var perRendererDataToggleVisualElement,
                    tooltip: "Mark this property with [PerRendererData] attribute.\nThis makes it Read-Only in the material inspector."));
                //perRendererDataToggleVisualElement.SetEnabled(property.isExposed);
            }
        }

        void BuildExposedField(PropertySheet propertySheet)
        {
            if (!isSubGraph)
            {
                var toggleDataPropertyDrawer = new ToggleDataPropertyDrawer();
                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    evt =>
                    {
                        if (shaderInput is AbstractShaderProperty property &&
                            (!property.overrideHLSLDeclaration || property.hlslDeclarationOverride == HLSLDeclaration.DoNotDeclare))
                        {
                            property.hlslDeclarationOverride = property.GetDefaultHLSLDeclaration();
                            property.overrideHLSLDeclaration = true;
                        }
                        this._preChangeValueCallback("Change Exposed Toggle");
                        this._exposedFieldChangedCallback(evt.isOn);
                        this._postChangeValueCallback(true, ModificationScope.Graph);
                    },
                    new ToggleData(shaderInput.isExposed),
                    shaderInput is ShaderKeyword ? "Generate Material Property" : "Show In Inspector",
                    out var exposedToggleVisualElement,
                    tooltip: shaderInput is ShaderKeyword ? "Generate a material property declaration to show this field in the material inspector." : "Hide or Show this property in the material inspector."));
                exposedToggle = exposedToggleVisualElement as Toggle;
            }
        }

        void BuildCustomBindingField(PropertySheet propertySheet, ShaderInput property)
        {
            if (isSubGraph && property.isCustomSlotAllowed)
            {
                var toggleDataPropertyDrawer = new ToggleDataPropertyDrawer();
                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue =>
                    {
                        if (property.useCustomSlotLabel == newValue.isOn)
                            return;
                        this._preChangeValueCallback("Change Custom Binding");
                        property.useCustomSlotLabel = newValue.isOn;
                        graphData.ValidateGraph();
                        this._postChangeValueCallback(true, ModificationScope.Topological);
                    },
                    new ToggleData(property.isConnectionTestable),
                    "Use Custom Binding",
                    out var exposedToggleVisualElement));
                exposedToggleVisualElement.SetEnabled(true);

                if (property.useCustomSlotLabel)
                {
                    var textPropertyDrawer = new TextPropertyDrawer();
                    var guiElement = textPropertyDrawer.CreateGUI(
                        null,
                        (string)shaderInput.customSlotLabel,
                        "Label",
                        1);

                    m_CustomSlotLabelField = textPropertyDrawer.textField;
                    m_CustomSlotLabelField.RegisterValueChangedCallback(
                        evt =>
                        {
                            if (evt.newValue != shaderInput.customSlotLabel)
                            {
                                this._preChangeValueCallback("Change Custom Binding Label");
                                shaderInput.customSlotLabel = evt.newValue;
                                m_CustomSlotLabelField.AddToClassList("modified");
                                this._postChangeValueCallback(true, ModificationScope.Topological);
                            }
                        });

                    if (!string.IsNullOrEmpty(shaderInput.customSlotLabel))
                        m_CustomSlotLabelField.AddToClassList("modified");
                    m_CustomSlotLabelField.styleSheets.Add(Resources.Load<StyleSheet>("Styles/CustomSlotLabelField"));

                    propertySheet.Add(guiElement);
                }
            }
        }

        void UpdateEnableState()
        {
            // some changes may change the exposed state
            exposedToggle?.SetValueWithoutNotify(shaderInput.isExposed);
            if (shaderInput is ShaderKeyword keyword)
            {
                keywordScopeField?.SetEnabled(!keyword.isBuiltIn && (keyword.keywordDefinition != KeywordDefinition.Predefined));
                exposedToggle?.SetEnabled((keyword.keywordDefinition != KeywordDefinition.Predefined));
                this._exposedFieldChangedCallback(keyword.generatePropertyBlock); // change exposed icon appropriately
            }
            else if (shaderInput is AbstractShaderProperty property)
            {
                // this is the best field to represent whether the toggle should be presented or not.
                exposedToggle?.SetEnabled(property.shouldForceExposed);
            }
            else
            {
                exposedToggle?.SetEnabled(shaderInput.isExposable && !shaderInput.isAlwaysExposed);
            }
        }

        void BuildDisplayNameField(PropertySheet propertySheet)
        {
            var textPropertyDrawer = new TextPropertyDrawer();
            propertySheet.Add(textPropertyDrawer.CreateGUI(
                null,
                (string)shaderInput.displayName,
                "Name",
                tooltip: "Display name used in the material inspector."));

            m_DisplayNameField = textPropertyDrawer.textField;
            m_DisplayNameField.RegisterValueChangedCallback(
                evt =>
                {
                    if (evt.newValue != shaderInput.displayName)
                    {
                        this._preChangeValueCallback("Change Display Name");
                        shaderInput.SetDisplayNameAndSanitizeForGraph(graphData, evt.newValue);
                        this._displayNameChangedCallback(evt.newValue);

                        if (string.IsNullOrEmpty(shaderInput.displayName))
                            m_DisplayNameField.RemoveFromClassList("modified");
                        else
                            m_DisplayNameField.AddToClassList("modified");

                        this._postChangeValueCallback(true, ModificationScope.Layout);
                    }
                });

            if (!string.IsNullOrEmpty(shaderInput.displayName))
                m_DisplayNameField.AddToClassList("modified");
            m_DisplayNameField.SetEnabled(shaderInput.isRenamable);
            m_DisplayNameField.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyNameReferenceField"));
        }

        void BuildReferenceNameField(PropertySheet propertySheet)
        {
            if (!isSubGraph || shaderInput is ShaderKeyword)
            {
                m_ReferenceNameDrawer = new TextPropertyDrawer();
                propertySheet.Add(m_ReferenceNameDrawer.CreateGUI(
                    null,
                    (string)shaderInput.referenceNameForEditing,
                    "Reference",
                    tooltip: "HLSL identifier used in the generated shader code. Use this with the material scripting API."));

                m_ReferenceNameField = m_ReferenceNameDrawer.textField;
                m_ReferenceNameField.RegisterValueChangedCallback(
                    evt =>
                    {
                        this._preChangeValueCallback("Change Reference Name");

                        if (evt.newValue != shaderInput.referenceName)
                        {
                            shaderInput.SetReferenceNameAndSanitizeForGraph(graphData, evt.newValue);
                            this._referenceNameChangedCallback(evt.newValue);
                        }

                        if (string.IsNullOrEmpty(shaderInput.overrideReferenceName))
                        {
                            m_ReferenceNameField.RemoveFromClassList("modified");
                            m_ReferenceNameDrawer.label.RemoveFromClassList("modified");
                        }
                        else
                        {
                            m_ReferenceNameField.AddToClassList("modified");
                            m_ReferenceNameDrawer.label.AddToClassList("modified");
                        }

                        this._postChangeValueCallback(true, ModificationScope.Graph);
                    });

                if (!string.IsNullOrEmpty(shaderInput.overrideReferenceName))
                {
                    m_ReferenceNameDrawer.textField.AddToClassList("modified");
                    m_ReferenceNameDrawer.label.AddToClassList("modified");
                }
                m_ReferenceNameDrawer.textField.SetEnabled(shaderInput.isReferenceRenamable);

                // add the right click context menu to the label
                IManipulator contextMenuManipulator = new ContextualMenuManipulator((evt) => AddShaderInputOptionsToContextMenu(shaderInput, evt));
                m_ReferenceNameDrawer.label.AddManipulator(contextMenuManipulator);
            }
        }

        void AddShaderInputOptionsToContextMenu(ShaderInput shaderInput, ContextualMenuPopulateEvent evt)
        {
            if (shaderInput.isRenamable && !string.IsNullOrEmpty(shaderInput.overrideReferenceName))
                evt.menu.AppendAction(
                    "Reset Reference",
                    e => { ResetReferenceName(); },
                    DropdownMenuAction.AlwaysEnabled);

            if (shaderInput.IsUsingOldDefaultRefName())
                evt.menu.AppendAction(
                    "Upgrade To New Reference Name",
                    e => { UpgradeDefaultReferenceName(); },
                    DropdownMenuAction.AlwaysEnabled);
        }

        public void ResetReferenceName()
        {
            this._preChangeValueCallback("Reset Reference Name");
            var refName = shaderInput.ResetReferenceName(graphData);
            m_ReferenceNameField.value = refName;
            this._referenceNameChangedCallback(refName);
            this._postChangeValueCallback(true, ModificationScope.Graph);
        }

        public void UpgradeDefaultReferenceName()
        {
            this._preChangeValueCallback("Upgrade Reference Name");
            var refName = shaderInput.UpgradeDefaultReferenceName(graphData);
            m_ReferenceNameField.value = refName;
            this._referenceNameChangedCallback(refName);
            this._postChangeValueCallback(true, ModificationScope.Graph);
        }

        bool isCurrentPropertyGlobal;
        void BuildPropertyFields(PropertySheet propertySheet)
        {
            if (shaderInput is AbstractShaderProperty property)
            {
                if (property.sgVersion < property.latestVersion)
                {
                    var typeString = property.propertyType.ToString();

                    Action dismissAction = null;
                    if (property.dismissedUpdateVersion < property.latestVersion)
                    {
                        dismissAction = () =>
                        {
                            _preChangeValueCallback("Dismiss Property Update");
                            property.dismissedUpdateVersion = property.latestVersion;
                            _postChangeValueCallback();
                            inspectorUpdateDelegate?.Invoke();
                        };
                    }

                    var help = HelpBoxRow.CreateUpgradePrompt($"{typeString} Property",
                        () => property.ChangeVersion(property.latestVersion),
                        dismissAction);
                    if (help != null)
                    {
                        propertySheet.Insert(0, help);
                    }
                }

                isCurrentPropertyGlobal = property.GetDefaultHLSLDeclaration() == HLSLDeclaration.Global;

                BuildPrecisionField(propertySheet, property);
                BuildHLSLDeclarationOverrideFields(propertySheet, property);
                BuildExposedField(propertySheet);
                BuildPerRendererDataField(propertySheet);

                switch (property)
                {
                    case IShaderPropertyDrawer propDrawer:
                        propDrawer.HandlePropertyField(propertySheet, _preChangeValueCallback, _postChangeValueCallback);
                        break;
                    case UnityEditor.ShaderGraph.Serialization.MultiJsonInternal.UnknownShaderPropertyType:
                        var helpBox = new HelpBoxRow("Cannot find the code for this Property, a package may be missing.", MessageType.Warning);
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
            }

            BuildCustomBindingField(propertySheet, shaderInput);
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
            if (isSubGraph)
                return;

            var hlslDecls = Enum.GetValues(typeof(HLSLDeclaration));
            var allowedDecls = new List<HLSLDeclaration>();

            bool anyAllowed = false;
            for (int i = 0; i < hlslDecls.Length; i++)
            {
                HLSLDeclaration decl = (HLSLDeclaration)hlslDecls.GetValue(i);
                var allowed = property.AllowHLSLDeclaration(decl);
                anyAllowed = anyAllowed || allowed;
                if (allowed)
                    allowedDecls.Add(decl);
            }

            const string tooltip = "Indicate where the property is expected to be changed.";
            if (anyAllowed)
            {
                var propRow = new PropertyRow(PropertyDrawerUtils.CreateLabel("Scope", 0));
                propRow.tooltip = tooltip;
                var popupField = new PopupField<HLSLDeclaration>(
                    allowedDecls,
                    property.GetDefaultHLSLDeclaration(),
                    (h => allHLSLDeclarationStrings[(int)h]),
                    (h => allHLSLDeclarationStrings[(int)h]));

                popupField.RegisterValueChangedCallback(
                    evt =>
                    {
                        this._preChangeValueCallback("Change Scope");
                        if (property.hlslDeclarationOverride == evt.newValue)
                            return;
                        property.hlslDeclarationOverride = evt.newValue;
                        property.overrideHLSLDeclaration = true;
                        UpdateEnableState();
                        if ((evt.newValue == HLSLDeclaration.Global) ^ (evt.previousValue == HLSLDeclaration.Global))
                        {
                            this._exposedFieldChangedCallback(evt.newValue != HLSLDeclaration.Global);
                            property.generatePropertyBlock = evt.newValue != HLSLDeclaration.Global;
                        }
                        this._postChangeValueCallback(true, ModificationScope.Graph);
                    });

                propRow.Add(popupField);
                propertySheet.Add(propRow);
            }
        }

        void BuildPrecisionField(PropertySheet propertySheet, AbstractShaderProperty property)
        {
            var enumPropertyDrawer = new EnumPropertyDrawer();
            propertySheet.Add(enumPropertyDrawer.CreateGUI(newValue =>
                {
                    this._preChangeValueCallback("Change Precision");
                    if (property.precision == (Precision)newValue)
                        return;
                    property.precision = (Precision)newValue;
                    this._precisionChangedCallback();
                    this._postChangeValueCallback();
                },
                (PropertyDrawerUtils.UIPrecisionForShaderGraphs)property.precision,
                "Precision",
                PropertyDrawerUtils.UIPrecisionForShaderGraphs.Inherit,
                out var precisionField,
                tooltip: "Request that the property uses Single 32-bit or Half 16-bit floating point precision."));
            if (property is Serialization.MultiJsonInternal.UnknownShaderPropertyType)
                precisionField.SetEnabled(false);
        }

        enum EnumTypeForUI { ExplicitValues = EnumType.Enum, TypeReference = EnumType.CSharpEnum }

        void HandleVector1ShaderProperty(PropertySheet propertySheet, Vector1ShaderProperty vector1ShaderProperty)
        {
            if (shaderInput.isExposed && !isSubGraph && !isCurrentPropertyGlobal)
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
                    out var modePropertyEnumField,
                    tooltip: "Indicate how this float property should appear in the material inspector UI."));
            }

            var floatType = (!shaderInput.isExposed || isSubGraph || isCurrentPropertyGlobal) ? FloatType.Default : vector1ShaderProperty.floatType;
            // Handle vector 1 mode parameters
            switch (floatType)
            {
                case FloatType.Slider:
                    var sliderTypePropertyDrawer = new EnumPropertyDrawer();
                    propertySheet.Add(sliderTypePropertyDrawer.CreateGUI(
                        newValue =>
                        {
                            this._preChangeValueCallback("Change Slider Type");
                            vector1ShaderProperty.sliderType = (SliderType)newValue;
                            this._postChangeValueCallback(true);
                        },
                        vector1ShaderProperty.sliderType,
                    "Slider Type",
                        SliderType.Default,
                        out var sliderTypePropertyEnumField,
                        tooltip: "Set the Slider type."));

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
                        isCurrentPropertyGlobal ? "Preview Value" : "Default Value",
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

                    var defaultField = (FloatField)propertyFloatField;
                    var minField = (FloatField)minFloatField;
                    var maxField = (FloatField)maxFloatField;

                    defaultField.RegisterValueChangedCallback(v =>
                    {
                        if (vector1ShaderProperty.sliderType == SliderType.Integer)
                            defaultField.value = MathF.Round((float)v.newValue);
                    });

                    minField.RegisterValueChangedCallback(v =>
                    {
                        if (vector1ShaderProperty.sliderType == SliderType.Integer)
                            minField.value = MathF.Round((float)v.newValue);
                    });

                    maxField.RegisterValueChangedCallback(v =>
                    {
                        if (vector1ShaderProperty.sliderType == SliderType.Integer)
                            maxField.value = MathF.Round((float)v.newValue);
                    });

                    minField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
                    {
                        propertySheet.warningContainer.Q<Label>().text = "";
                        vector1ShaderProperty.value = Mathf.Max(Mathf.Min(vector1ShaderProperty.value, vector1ShaderProperty.rangeValues.y), vector1ShaderProperty.rangeValues.x);
                        defaultField.value = vector1ShaderProperty.value;
                        _postChangeValueCallback();
                    }, TrickleDown.TrickleDown);

                    maxField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
                    {
                        propertySheet.warningContainer.Q<Label>().text = "";
                        vector1ShaderProperty.value = Mathf.Max(Mathf.Min(vector1ShaderProperty.value, vector1ShaderProperty.rangeValues.y), vector1ShaderProperty.rangeValues.x);
                        defaultField.value = vector1ShaderProperty.value;
                        _postChangeValueCallback();
                    }, TrickleDown.TrickleDown);

                    if (vector1ShaderProperty.sliderType == SliderType.Power)
                    {
                        propertySheet.Add(floatPropertyDrawer.CreateGUI(
                        newValue =>
                        {
                            _preChangeValueCallback("Change Slider Power Value");
                            vector1ShaderProperty.sliderPower = newValue;
                            _postChangeValueCallback();
                        },
                        vector1ShaderProperty.sliderPower,
                        "Power",
                        out var propertySliderPowerField));
                        propertySliderPowerField.tooltip = "The Power factor applied to the Slider";
                    }

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
                        isCurrentPropertyGlobal ? "Preview Value" : "Default Value",
                        out var integerPropertyField));
                    break;

                case FloatType.Enum:
                    var enumValuePropertyDrawer = new IntegerPropertyDrawer();
                    // Default field
                    propertySheet.Add(enumValuePropertyDrawer.CreateGUI(
                        newValue =>
                        {
                            this._preChangeValueCallback("Change property value");
                            this._changeValueCallback((float)newValue);
                            this._postChangeValueCallback();
                        },
                        (int)vector1ShaderProperty.value,
                        isCurrentPropertyGlobal ? "Preview Value" : "Default Value",
                        out var enumValuePropertyField));

                    var enumPropertyDrawer = new EnumPropertyDrawer();
                    propertySheet.Add(enumPropertyDrawer.CreateGUI(
                        newValue =>
                        {
                            this._preChangeValueCallback("Change Enum Type");
                            vector1ShaderProperty.enumType = (EnumType)newValue;
                            this._postChangeValueCallback(true);
                        },
                        (EnumTypeForUI)vector1ShaderProperty.enumType,
                        "Enum Type",
                        EnumTypeForUI.ExplicitValues,
                        out var modePropertyEnumField,
                        tooltip: "Set the Enum value type."));

                    switch (vector1ShaderProperty.enumType)
                    {
                        case EnumType.Enum:
                            var container = new IMGUIContainer(() => OnEnumGUIHandler()) { name = "ListContainer" };
                            AddPropertyRowToSheet(propertySheet, container, "Entries");
                            break;
                        case EnumType.CSharpEnum:
                            var textPropertyDrawer = new TextPropertyDrawer();
                            propertySheet.Add(textPropertyDrawer.CreateGUI(
                                newValue =>
                                {
                                    this._preChangeValueCallback("Change C# Enum Type");
                                    newValue = GetSanitizedEnumRefName(newValue);
                                    vector1ShaderProperty.cSharpEnumString = newValue;
                                    this._postChangeValueCallback(true);
                                },
                                vector1ShaderProperty.cSharpEnumString,
                                "C# Enum Type",
                                tooltip: "Enter an Enum type."));
                            break;
                        case EnumType.KeywordEnum:
                        default:
                            break;
                    }
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
                        isCurrentPropertyGlobal ? "Preview Value" : "Default Value",
                        out var defaultFloatPropertyField));
                    break;
            }
        }

        void HandleVector2ShaderProperty(PropertySheet propertySheet, Vector2ShaderProperty vector2ShaderProperty)
        {
            var vector2PropertyDrawer = new Vector2PropertyDrawer();
            vector2PropertyDrawer.preValueChangeCallback = () => this._preChangeValueCallback("Change property value");
            vector2PropertyDrawer.postValueChangeCallback = () => this._postChangeValueCallback();

            propertySheet.Add(vector2PropertyDrawer.CreateGUI(
                newValue => _changeValueCallback(newValue),
                vector2ShaderProperty.value,
                isCurrentPropertyGlobal ? "Preview Value" : "Default Value",
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
                isCurrentPropertyGlobal ? "Preview Value" : "Default Value",
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
                isCurrentPropertyGlobal ? "Preview Value" : "Default Value",
                out var propertyVec4Field));
        }

        void HandleColorProperty(PropertySheet propertySheet, ColorShaderProperty colorProperty)
        {
            var colorPropertyDrawer = new ColorPropertyDrawer();

            if (!isSubGraph)
            {
                if (colorProperty.isMainColor)
                {
                    var mainColorLabel = new IMGUIContainer(() =>
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField("Main Color", EditorStyles.largeLabel);
                        EditorGUILayout.Space();
                        EditorGUI.indentLevel--;
                    });
                    propertySheet.Insert(2, mainColorLabel);
                }
            }

            if (/*shaderInput.isExposed && */!isSubGraph && !isCurrentPropertyGlobal)
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

            propertySheet.Add(colorPropertyDrawer.CreateGUI(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    this._changeValueCallback(newValue);
                    this._postChangeValueCallback();
                },
                colorProperty.value,
                isCurrentPropertyGlobal ? "Preview Value" : "Default Value",
                out var propertyColorField));

            var colorField = (ColorField)propertyColorField;
            colorField.hdr = colorProperty.colorMode == ColorMode.HDR;
        }

        void HandleTexture2DProperty(PropertySheet propertySheet, Texture2DShaderProperty texture2DProperty)
        {
            var texture2DPropertyDrawer = new Texture2DPropertyDrawer();

            if (!isSubGraph)
            {
                if (texture2DProperty.isMainTexture)
                {
                    var mainTextureLabel = new IMGUIContainer(() =>
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField("Main Texture", EditorStyles.largeLabel);
                        EditorGUILayout.Space();
                        EditorGUI.indentLevel--;
                    });
                    propertySheet.Insert(2, mainTextureLabel);
                }
            }


            propertySheet.Add(texture2DPropertyDrawer.CreateGUI(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    this._changeValueCallback(newValue);
                    this._postChangeValueCallback();
                },
                texture2DProperty.value.texture,
                isCurrentPropertyGlobal ? "Preview Value" : "Default Value",
                out var texture2DField
            ));

            if (!isSubGraph && !isCurrentPropertyGlobal)
            {
                var enumPropertyDrawer = new EnumPropertyDrawer();
                propertySheet.Add(enumPropertyDrawer.CreateGUI(
                    newValue =>
                    {
                        this._preChangeValueCallback("Change Texture mode");
                        if (texture2DProperty.defaultType == (Texture2DShaderProperty.DefaultType)newValue)
                            return;
                        texture2DProperty.defaultType = (Texture2DShaderProperty.DefaultType)newValue;
                        this._postChangeValueCallback(false, ModificationScope.Graph);
                    },
                    texture2DProperty.defaultType,
                    "Mode",
                    Texture2DShaderProperty.DefaultType.White,
                    out var textureModeField,
                    tooltip: "Fallback texture if none is provided."));

                textureModeField.SetEnabled(texture2DProperty.generatePropertyBlock);
            }
            if (!isSubGraph)
            {
                var togglePropertyDrawer = new ToggleDataPropertyDrawer();
                propertySheet.Add(togglePropertyDrawer.CreateGUI(
                    newValue =>
                    {
                        this._preChangeValueCallback("Change Use Tilling and Offset");
                        if (texture2DProperty.useTilingAndOffset == newValue.isOn)
                            return;
                        texture2DProperty.useTilingAndOffset = newValue.isOn;
                        this._postChangeValueCallback();
                    },
                    new ToggleData(texture2DProperty.useTilingAndOffset, true),
                    "Use Tiling and Offset",
                    out var tilingAndOffsetToggle));
                propertySheet.Add(togglePropertyDrawer.CreateGUI(
                    newValue =>
                    {
                        this._preChangeValueCallback("Change Use TexelSize");
                        if (texture2DProperty.useTexelSize == newValue.isOn)
                            return;
                        texture2DProperty.useTexelSize = newValue.isOn;
                        this._postChangeValueCallback();
                    },
                    new ToggleData(texture2DProperty.useTexelSize, true),
                    "Use TexelSize",
                    out var texelSizeToggle));
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
                isCurrentPropertyGlobal ? "Preview Value" : "Default Value",
                out var texture2DArrayField
            ));
        }

        #region VT reorderable list handler
        void HandleVirtualTextureProperty(PropertySheet propertySheet, VirtualTextureShaderProperty virtualTextureProperty)
        {
            var container = new IMGUIContainer(() => OnVTGUIHandler(virtualTextureProperty)) { name = "ListContainer" };
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
                        var otherPropertyRefNames = graphData.BuildPropertyReferenceNameList(virtualTextureProperty, svt.layerRefName);
                        var newName = NodeUtils.ConvertToValidHLSLIdentifier(evt.newValue);
                        var newLayerRefName = GraphUtil.SanitizeName(otherPropertyRefNames, "{0}_{1}", newName);
                        if (newLayerRefName != svt.layerRefName)
                        {
                            this._preChangeValueCallback("Change Layer Ref Name");
                            svt.layerRefName = newLayerRefName;
                            this._postChangeValueCallback(false, ModificationScope.Graph);
                        }
                        // Always update the display name to the sanitized name. If an invalid name was entered that ended up being sanitized to the old value,
                        // the text box still needs to be updated to display the sanitized name.
                        m_VTLayer_RefName.SetValueWithoutNotify(newLayerRefName);
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
                        (m_VTReorderableList.list[index] as SerializableVirtualTextureLayer).layerTextureType = (LayerTextureType)evt.newValue;

                    this._postChangeValueCallback(false, ModificationScope.Graph);
                });
            AddPropertyRowToSheet(propertySheet, m_VTLayer_TextureType, "  Layer Texture Type");
        }

        private void OnVTGUIHandler(VirtualTextureShaderProperty property)
        {
            if (m_VTReorderableList == null)
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
                isCurrentPropertyGlobal ? "Preview Value" : "Default Value",
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
                isCurrentPropertyGlobal ? "Preview Value" : "Default Value",
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
                isCurrentPropertyGlobal ? "Preview Value" : "Default Value",
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
                "Preview Value",
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
                "Preview Value",
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
                "Preview Value",
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
                    state.filter = (TextureSamplerState.FilterMode)newValue;
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
                    state.wrap = (TextureSamplerState.WrapMode)newValue;
                    samplerStateShaderProperty.value = state;
                    this._postChangeValueCallback(false, ModificationScope.Graph);
                    this.inspectorUpdateDelegate();
                },
                samplerStateShaderProperty.value.wrap,
                "Wrap",
                TextureSamplerState.WrapMode.Repeat,
                out var wrapVisualElement));

            propertySheet.Add(enumPropertyDrawer.CreateGUI(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    TextureSamplerState state = samplerStateShaderProperty.value;
                    state.anisotropic = (TextureSamplerState.Anisotropic)newValue;
                    samplerStateShaderProperty.value = state;
                    this._postChangeValueCallback(false, ModificationScope.Graph);
                    this.inspectorUpdateDelegate();
                },
                samplerStateShaderProperty.value.anisotropic,
                "Aniso",
                TextureSamplerState.Anisotropic.None,
                out var anisoVisualElement));
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
                "Default Value",
                out var propertyGradientField));
        }

        void BuildAttributesFields(PropertySheet propertySheet)
        {
            if (isSubGraph || isCurrentPropertyGlobal)
                return;

            if (shaderInput.isExposed && shaderInput is AbstractShaderProperty property)
            {
                var container = new IMGUIContainer(() => OnCustomAttributesGUIHandler()) { name = "ListContainer" };
                AddPropertyRowToSheet(propertySheet, container, "Custom Attributes", "Add Custom Attributes to be used with Custom Property Drawers.");
            }
        }

        ReorderableList m_CustomAttributesReorderableList;
        int m_CustomAttributesSelectedIndex;

        void OnCustomAttributesGUIHandler()
        {
            if (m_CustomAttributesReorderableList == null)
            {
                CustomAttributesRecreateList();
                CustomAttributesAddCallbacks();
            }

            m_CustomAttributesReorderableList.index = m_CustomAttributesSelectedIndex;
            m_CustomAttributesReorderableList.DoLayoutList();
        }

        internal void CustomAttributesRecreateList()
        {
            if (shaderInput is AbstractShaderProperty property)
            {
                // Create reorderable list from entries
                m_CustomAttributesReorderableList = new ReorderableList(property.customAttributes, typeof(Tuple<string, int>), true, true, true, true);
            }
        }

        void CustomAttributesAddCallbacks()
        {
            if (shaderInput is AbstractShaderProperty property)
            {
                // Draw Header
                m_CustomAttributesReorderableList.drawHeaderCallback = (Rect rect) =>
                {
                    int indent = 14;
                    var displayRect = new Rect(rect.x + indent, rect.y, (rect.width - indent) / 2, rect.height);
                    EditorGUI.LabelField(displayRect, "Name");
                    var referenceRect = new Rect((rect.x + indent) + (rect.width - indent) / 2, rect.y, (rect.width - indent) / 2, rect.height);
                    EditorGUI.LabelField(referenceRect, "Value");
                };

                // Draw Element
                m_CustomAttributesReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    var entry = (AbstractShaderProperty.PropertyAttribute)m_CustomAttributesReorderableList.list[index];
                    Rect displayRect = new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight);

                    EditorGUI.BeginChangeCheck();
                    var name = EditorGUI.DelayedTextField(displayRect, entry.name, EditorStyles.label);
                    var value = EditorGUI.DelayedTextField(new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight), entry.value);

                    if (EditorGUI.EndChangeCheck())
                    {
                        this._preChangeValueCallback("Edit Custom Attribute Entry");
                        name = GetSanitizedReferenceName(name);
                        if (string.IsNullOrWhiteSpace(name))
                            Debug.LogWarning("Invalid Attribute name. Attribute names cannot be empty or all whitespace.");
                        else if (int.TryParse(name, out int intVal) || float.TryParse(name, out float floatVal))
                            Debug.LogWarning("Invalid Attribute name. Attribute names cannot be valid integer or floating point numbers.");

                        value = GetSanitizedAttributeValue(value);
                        property.customAttributes[index].name = name;
                        property.customAttributes[index].value = value;

                        this._postChangeValueCallback(true);
                    }
                };

                // Add
                m_CustomAttributesReorderableList.onAddCallback = (ReorderableList list) =>
                {
                    this._preChangeValueCallback("Add Custom Attribute");

                    // Add new entry
                    property.customAttributes.Add(new("MyCustomAttribute", string.Empty));

                    // Update GUI
                    this._postChangeValueCallback(true);
                    m_CustomAttributesSelectedIndex = list.list.Count - 1;
                };

                // Remove
                m_CustomAttributesReorderableList.onRemoveCallback = (ReorderableList list) =>
                {
                    this._preChangeValueCallback("Remove Custom Attribute");

                    // Remove entry
                    property.customAttributes.RemoveAt(list.index);

                    // Rebuild();
                    this._postChangeValueCallback(true);
                    m_CustomAttributesSelectedIndex = m_CustomAttributesSelectedIndex >= list.list.Count - 1 ? list.list.Count - 1 : m_CustomAttributesSelectedIndex;
                };

                // Element height
                m_CustomAttributesReorderableList.elementHeightCallback = (int indexer) =>
                {
                    return m_CustomAttributesReorderableList.elementHeight;
                };

                // Can add
                m_CustomAttributesReorderableList.onCanAddCallback = (ReorderableList list) =>
                {
                    return list.count < 8;
                };

                // Can remove
                m_CustomAttributesReorderableList.onCanRemoveCallback = (ReorderableList list) => true;

                // Select
                m_CustomAttributesReorderableList.onSelectCallback = (ReorderableList list) =>
                {
                    m_CustomAttributesSelectedIndex = list.index;
                };

                // Reorder
                m_CustomAttributesReorderableList.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) =>
                {
                    this._preChangeValueCallback("Reordered Custom Attributes");
                    this._postChangeValueCallback(true);
                };
            }
        }

        enum KeywordShaderStageDropdownUI    // maps to KeywordShaderStage, this enum ONLY used for the UI dropdown menu
        {
            All = KeywordShaderStage.All,
            Vertex = KeywordShaderStage.Vertex,
            Fragment = KeywordShaderStage.Fragment,
        }

        void BuildKeywordFields(PropertySheet propertySheet, ShaderInput shaderInput)
        {
            var keyword = shaderInput as ShaderKeyword;
            if (keyword == null)
                return;

            var enumPropertyDrawer = new EnumPropertyDrawer();
            propertySheet.Add(enumPropertyDrawer.CreateGUI(
                newValue =>
                {
                    this._preChangeValueCallback("Change Keyword type");
                    if (keyword.keywordDefinition == (KeywordDefinition)newValue)
                        return;
                    keyword.keywordDefinition = (KeywordDefinition)newValue;
                    UpdateEnableState();
                    this._postChangeValueCallback(true, ModificationScope.Nothing);
                },
                keyword.keywordDefinition,
                "Definition",
                KeywordDefinition.ShaderFeature,
                out var typeField,
                tooltip: "Indicate how the keyword is defined and under what circumstances its permutations will be compiled."));

            if (keyword.keywordDefinition == KeywordDefinition.ShaderFeature && isSubGraph)
            {
                var help = new HelpBoxRow("Shader Feature Keywords in SubGraphs do not generate variant permutations.", MessageType.Info);
                propertySheet.Add(help);
            }

            typeField.SetEnabled(!keyword.isBuiltIn);
            {
                var isOverridablePropertyDrawer = new ToggleDataPropertyDrawer();
                bool enabledState = keyword.keywordDefinition != KeywordDefinition.Predefined;
                bool toggleState = keyword.keywordScope == KeywordScope.Global || !enabledState;
                propertySheet.Add(isOverridablePropertyDrawer.CreateGUI(
                    newValue =>
                    {
                        this._preChangeValueCallback("Change Keyword Is Overridable");
                        keyword.keywordScope = newValue.isOn
                            ? KeywordScope.Global
                            : KeywordScope.Local;
                    },
                    new ToggleData(toggleState),
                    "Is Overridable",
                    out keywordScopeField,
                    tooltip: "Indicate whether this keyword's state can be overridden through the Shader.SetKeyword scripting interface."));
                keywordScopeField.SetEnabled(enabledState);
            }
            BuildExposedField(propertySheet);
            {
                propertySheet.Add(enumPropertyDrawer.CreateGUI(
                    newValue =>
                    {
                        this._preChangeValueCallback("Change Keyword stage");
                        if (keyword.keywordStages == (KeywordShaderStage)newValue)
                            return;
                        keyword.keywordStages = (KeywordShaderStage)newValue;
                    },
                    (KeywordShaderStageDropdownUI)keyword.keywordStages,
                    "Stages",
                    KeywordShaderStageDropdownUI.All,
                    out keywordScopeField,
                    tooltip: "Indicates which shader stages this keyword is relevant for."));
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
                    if (graphData.owner.materialArtifact)
                    {
                        graphData.owner.materialArtifact.SetFloat(keyword.referenceName, keyword.value);
                        MaterialEditor.ApplyMaterialPropertyDrawers(graphData.owner.materialArtifact);
                    }
                    this._postChangeValueCallback(false, ModificationScope.Graph);
                },
                new ToggleData(keyword.value == 1),
                keyword.keywordDefinition == KeywordDefinition.Predefined ? "Preview Value" : "Default Value",
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
                if (graphData.owner.materialArtifact)
                {
                    graphData.owner.materialArtifact.SetFloat(keyword.referenceName, field.index);
                    MaterialEditor.ApplyMaterialPropertyDrawers(graphData.owner.materialArtifact);
                }
                this._postChangeValueCallback(false, ModificationScope.Graph);
            });

            AddPropertyRowToSheet(propertySheet, field, keyword.keywordDefinition == KeywordDefinition.Predefined ? "Preview Value" : "Default Value");

            var container = new IMGUIContainer(() => OnKeywordGUIHandler()) { name = "ListContainer" };
            AddPropertyRowToSheet(propertySheet, container, "Entries");
            container.SetEnabled(!keyword.isBuiltIn);
        }

        static void AddPropertyRowToSheet(PropertySheet propertySheet, VisualElement control, string labelName, string tooltip = default)
        {
            propertySheet.Add(new PropertyRow(new Label(labelName)), (row) =>
            {
                row.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
                row.Add(control);
                row.tooltip = tooltip;
            });
        }

        ReorderableList m_EnumReorderableList;
        int m_EnumSelectedIndex;

        void OnEnumGUIHandler()
        {
            if (m_EnumReorderableList == null)
            {
                EnumRecreateList();
                EnumAddCallbacks();
            }

            m_EnumReorderableList.index = m_EnumSelectedIndex;
            m_EnumReorderableList.DoLayoutList();
        }

        void CheckEnumNamesAndValuesListLength(Vector1ShaderProperty vector1ShaderProperty)
        {
            var diff = vector1ShaderProperty.enumNames.Count - vector1ShaderProperty.enumValues.Count;
            if (diff == 0)
                return;
            if (diff > 0) // missing values
            {
                for (int i = 0; i < diff; i++)
                    vector1ShaderProperty.enumValues.Add(0);
            }
            else // missing names
            {
                for (int i = 0; i < -diff; i++)
                    vector1ShaderProperty.enumNames.Add("_");
            }
        }

        internal void EnumRecreateList()
        {
            if (shaderInput is Vector1ShaderProperty vector1Property && vector1Property.floatType == FloatType.Enum && vector1Property.enumType == EnumType.Enum)
            {
                // Create a virtual list of names and values
                List<(string name, int value)> list = new();
                CheckEnumNamesAndValuesListLength(vector1Property);
                for (int i = 0; i < vector1Property.enumNames.Count; i++)
                {
                    var name = vector1Property.enumNames[i];
                    var value = vector1Property.enumValues[i];
                    list.Add(new(name, value));
                }
                // Create reorderable list from entries
                m_EnumReorderableList = new ReorderableList(list, typeof(Tuple<string, int>), true, true, true, true);
            }
        }

        void EnumAddCallbacks()
        {
            if (shaderInput is Vector1ShaderProperty vector1Property && vector1Property.floatType == FloatType.Enum && vector1Property.enumType == EnumType.Enum)
            {
                // Draw Header
                m_EnumReorderableList.drawHeaderCallback = (Rect rect) =>
                {
                    int indent = 14;
                    var displayRect = new Rect(rect.x + indent, rect.y, (rect.width - indent) / 2, rect.height);
                    EditorGUI.LabelField(displayRect, "Name");
                    var referenceRect = new Rect((rect.x + indent) + (rect.width - indent) / 2, rect.y, (rect.width - indent) / 2, rect.height);
                    EditorGUI.LabelField(referenceRect, "Value");
                };

                // Draw Element
                m_EnumReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    (string name, int value) entry = ((string name, int value))m_EnumReorderableList.list[index];
                    Rect displayRect = new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight);

                    EditorGUI.BeginChangeCheck();
                    var name = EditorGUI.DelayedTextField(displayRect, entry.name, EditorStyles.label);
                    var value = EditorGUI.IntField(new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight), entry.value);

                    if (EditorGUI.EndChangeCheck())
                    {
                        this._preChangeValueCallback("Edit Enum Entry");
                        name = GraphUtil.SanitizeName(vector1Property.enumNames, "{0} {1}", name, m_DisplayNameDisallowedPattern);
                        if (string.IsNullOrWhiteSpace(name))
                            Debug.LogWarning("Invalid display name. Display names cannot be empty or all whitespace.");
                        else if (int.TryParse(name, out int intVal) || float.TryParse(name, out float floatVal))
                            Debug.LogWarning("Invalid display name. Display names cannot be valid integer or floating point numbers.");

                        vector1Property.enumNames[index] = name;
                        vector1Property.enumValues[index] = value;

                        this._postChangeValueCallback(true);
                    }
                };

                // Add
                m_EnumReorderableList.onAddCallback = (ReorderableList list) =>
                {
                    this._preChangeValueCallback("Add Enum Entry");

                    var name = "New";
                    name = GraphUtil.SanitizeName(vector1Property.enumNames, "{0} {1}", name, m_DisplayNameDisallowedPattern);
                    var value = FindSmallestMissingPositive(vector1Property.enumValues);

                    // Add new entry
                    vector1Property.enumNames.Add(name);
                    vector1Property.enumValues.Add(value);

                    // Update GUI
                    this._postChangeValueCallback(true);
                    m_EnumSelectedIndex = list.count - 1;
                };

                // Remove
                m_EnumReorderableList.onRemoveCallback = (ReorderableList list) =>
                {
                    this._preChangeValueCallback("Remove Keyword Entry");

                    // Remove entry
                    vector1Property.enumNames.RemoveAt(list.index);
                    vector1Property.enumValues.RemoveAt(list.index);

                    // Rebuild();
                    this._postChangeValueCallback(true);
                    m_EnumSelectedIndex = m_EnumSelectedIndex >= list.list.Count - 1 ? list.list.Count - 1 : m_EnumSelectedIndex;
                };

                // Element height
                m_EnumReorderableList.elementHeightCallback = (int indexer) =>
                {
                    return m_EnumReorderableList.elementHeight;
                };

                // Can add
                m_EnumReorderableList.onCanAddCallback = (ReorderableList list) =>
                {
                    return list.count < 7;
                };

                // Can remove
                m_EnumReorderableList.onCanRemoveCallback = (ReorderableList list) =>
                {
                    return list.count > 1;
                };

                // Select
                m_EnumReorderableList.onSelectCallback = (ReorderableList list) =>
                {
                    m_EnumSelectedIndex = list.index;
                };

                // Reorder
                m_EnumReorderableList.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) =>
                {
                    this._preChangeValueCallback("Reordered Enum Entries");
                    var name = vector1Property.enumNames[oldIndex];
                    var value = vector1Property.enumValues[oldIndex];
                    vector1Property.enumNames.RemoveAt(oldIndex);
                    vector1Property.enumValues.RemoveAt(oldIndex);
                    vector1Property.enumNames.Insert(newIndex, name);
                    vector1Property.enumValues.Insert(newIndex, value);
                    this._postChangeValueCallback(true);
                };
            }
        }

        static int FindSmallestMissingPositive(List<int> nums)
        {
            HashSet<int> positiveNumbers = new HashSet<int>(nums.Where(x => x >= 0));

            int smallestMissing = 0;
            while (positiveNumbers.Contains(smallestMissing))
            {
                smallestMissing++;
            }

            return smallestMissing;
        }

        void OnKeywordGUIHandler()
        {
            if (m_KeywordReorderableList == null)
            {
                KeywordRecreateList();
                KeywordAddCallbacks();
            }

            m_KeywordReorderableList.index = m_KeywordSelectedIndex;
            m_KeywordReorderableList.DoLayoutList();
        }

        internal void KeywordRecreateList()
        {
            if (!(shaderInput is ShaderKeyword keyword))
                return;

            // Create reorderable list from entries
            m_KeywordReorderableList = new ReorderableList(keyword.entries, typeof(KeywordEntry), true, true, true, true);
        }

        void KeywordAddCallbacks()
        {
            if (!(shaderInput is ShaderKeyword keyword))
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
                EditorGUI.LabelField(displayRect, new GUIContent("", "Enum keyword display names can only use alphanumeric characters, whitespace and `_`"));
                var referenceName = EditorGUI.TextField(new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight), entry.referenceName,
                    keyword.isBuiltIn ? EditorStyles.label : greyLabel);

                if (EditorGUI.EndChangeCheck())
                {
                    this._preChangeValueCallback("Edit Enum Keyword Entry");

                    displayName = GetSanitizedDisplayName(displayName);
                    referenceName = GetSanitizedReferenceName(displayName.ToUpper());
                    var duplicateIndex = FindDuplicateKeywordReferenceNameIndex(entry.id, referenceName);
                    if (duplicateIndex != -1)
                    {
                        var duplicateEntry = ((KeywordEntry)m_KeywordReorderableList.list[duplicateIndex]);
                        Debug.LogWarning($"Display name '{displayName}' will create the same reference name '{referenceName}' as entry {duplicateIndex + 1} with display name '{duplicateEntry.displayName}'.");
                    }
                    else if (string.IsNullOrWhiteSpace(displayName))
                        Debug.LogWarning("Invalid display name. Display names cannot be empty or all whitespace.");
                    else if (int.TryParse(displayName, out int intVal) || float.TryParse(displayName, out float floatVal))
                        Debug.LogWarning("Invalid display name. Display names cannot be valid integer or floating point numbers.");
                    else
                        keyword.entries[index] = new KeywordEntry(GetFirstUnusedKeywordID(), displayName, referenceName);

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
        int GetFirstUnusedKeywordID()
        {
            if (!(shaderInput is ShaderKeyword keyword))
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
            if (!(shaderInput is ShaderKeyword keyword))
                return;

            this._preChangeValueCallback("Add Keyword Entry");

            int index = GetFirstUnusedKeywordID();
            if (index <= 0)
                return; // Error has already occured, don't attempt to add this entry.

            var displayName = "New";
            var referenceName = "NEW";
            GetDuplicateSafeEnumNames(index, "New", out displayName, out referenceName);

            // Add new entry
            keyword.entries.Add(new KeywordEntry(index, displayName, referenceName));

            // Update GUI
            this._postChangeValueCallback(true);
            this._keywordChangedCallback();
            m_KeywordSelectedIndex = list.list.Count - 1;
        }

        void KeywordRemoveEntry(ReorderableList list)
        {
            if (!(shaderInput is ShaderKeyword keyword))
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
            this._preChangeValueCallback("Reorder Keyword Entry");
            this._postChangeValueCallback(true);
        }

        public string GetDuplicateSafeEnumDisplayName(int id, string name)
        {
            name = name.Trim();
            var entryList = m_KeywordReorderableList.list as List<KeywordEntry>;
            return GraphUtil.SanitizeName(entryList.Where(p => p.id != id).Select(p => p.displayName), "{0} {1}", name, m_DisplayNameDisallowedPattern);
        }

        void GetDuplicateSafeEnumNames(int id, string name, out string displayName, out string referenceName)
        {
            name = name.Trim();
            // Get de-duplicated display and reference names
            displayName = GetDuplicateSafeEnumDisplayName(id, name);
            referenceName = GetDuplicateSafeReferenceName(id, displayName.ToUpper());
            // Check when the simple reference name should be for the display name.
            // If these don't match then there will be a desync which causes the enum entry to not work.
            // An example where this happens is ["new 1", "NEW_1"] already exists.
            // The display name "New_1" is added.
            // This new display name doesn't exist, but it finds the reference name of "NEW_1" already exists so we get the pair ["New_1", "NEW_2"] which is invalid.
            // The easiest fix in this case is to just use the safe reference name as the new display name which is guaranteed to be unique.
            var simpleReferenceName = Regex.Replace(displayName.ToUpper(), m_ReferenceNameDisallowedPattern, "_");
            if (referenceName != simpleReferenceName)
                displayName = referenceName;
        }

        string GetSanitizedDisplayName(string name)
        {
            name = name.Trim();
            return Regex.Replace(name, m_DisplayNameDisallowedPattern, "_");
        }

        public string GetDuplicateSafeReferenceName(int id, string name)
        {
            name = name.Trim();
            var entryList = m_KeywordReorderableList.list as List<KeywordEntry>;
            return GraphUtil.SanitizeName(entryList.Where(p => p.id != id).Select(p => p.referenceName), "{0}_{1}", name, m_ReferenceNameDisallowedPattern);
        }

        string GetSanitizedReferenceName(string name)
        {
            name = name.Trim();
            return Regex.Replace(name, m_ReferenceNameDisallowedPattern, "_");
        }

        string GetSanitizedEnumRefName(string value)
        {
            value = value.Trim();
            return Regex.Replace(value, m_EnumRefDisallowedPattern, "_");
        }

        string GetSanitizedAttributeValue(string value)
        {
            var values = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (values.Length == 0)
            {
                return string.Empty;
            }
            else
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = Regex.Replace(values[i], m_AttributeValueDisallowedPattern, "_").Trim();
                }
                return string.Join(", ", values);
            }
        }

        int FindDuplicateKeywordReferenceNameIndex(int id, string referenceName)
        {
            var entryList = m_KeywordReorderableList.list as List<KeywordEntry>;
            return entryList.FindIndex(entry => entry.id != id && entry.referenceName == referenceName);
        }

        void BuildDropdownFields(PropertySheet propertySheet, ShaderInput shaderInput)
        {
            var dropdown = shaderInput as ShaderDropdown;
            if (dropdown == null)
                return;

            BuildDropdownField(propertySheet, dropdown);
        }

        void BuildDropdownField(PropertySheet propertySheet, ShaderDropdown dropdown)
        {
            // Clamp value between entry list
            int value = Mathf.Clamp(dropdown.value, 0, dropdown.entries.Count - 1);

            // Default field
            var field = new PopupField<string>(dropdown.entries.Select(x => x.displayName).ToList(), value);
            field.RegisterValueChangedCallback(evt =>
            {
                this._preChangeValueCallback("Change Dropdown Value");
                dropdown.value = field.index;
                m_DropdownId = dropdown.entryId;
                this._postChangeValueCallback(false, ModificationScope.Graph);
            });

            AddPropertyRowToSheet(propertySheet, field, "Default");

            var container = new IMGUIContainer(() => OnDropdownGUIHandler()) { name = "ListContainer" };
            AddPropertyRowToSheet(propertySheet, container, "Entries");
            container.SetEnabled(true);
        }

        void OnDropdownGUIHandler()
        {
            if (m_DropdownReorderableList == null)
            {
                DropdownRecreateList();
                DropdownAddCallbacks();
            }

            m_DropdownReorderableList.index = m_DropdownSelectedIndex;
            m_DropdownReorderableList.DoLayoutList();
        }

        internal void DropdownRecreateList()
        {
            if (!(shaderInput is ShaderDropdown dropdown))
                return;

            // Create reorderable list from entries
            m_DropdownReorderableList = new ReorderableList(dropdown.entries, typeof(DropdownEntry), true, true, true, true);
            m_Dropdown = dropdown;
            m_DropdownId = dropdown.entryId;
        }

        void DropdownAddCallbacks()
        {
            if (!(shaderInput is ShaderDropdown dropdown))
                return;

            // Draw Header
            m_DropdownReorderableList.drawHeaderCallback = (Rect rect) =>
            {
                int indent = 14;
                var displayRect = new Rect(rect.x + indent, rect.y, (rect.width - indent) / 2, rect.height);
                EditorGUI.LabelField(displayRect, "Entry Name");
            };

            // Draw Element
            m_DropdownReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                DropdownEntry entry = ((DropdownEntry)m_DropdownReorderableList.list[index]);
                EditorGUI.BeginChangeCheck();

                Rect displayRect = new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight);
                var displayName = EditorGUI.DelayedTextField(displayRect, entry.displayName, EditorStyles.label);

                if (EditorGUI.EndChangeCheck())
                {
                    displayName = GetSanitizedDisplayName(displayName);
                    var duplicateIndex = FindDuplicateDropdownDisplayNameIndex(entry.id, displayName);
                    if (duplicateIndex != -1)
                    {
                        var duplicateEntry = ((DropdownEntry)m_DropdownReorderableList.list[duplicateIndex]);
                        Debug.LogWarning($"Display name '{displayName}' will create the same display name as entry {duplicateIndex + 1}.");
                    }
                    else if (string.IsNullOrWhiteSpace(displayName))
                        Debug.LogWarning("Invalid display name. Display names cannot be empty or all whitespace.");
                    else if (int.TryParse(displayName, out int intVal) || float.TryParse(displayName, out float floatVal))
                        Debug.LogWarning("Invalid display name. Display names cannot be valid integer or floating point numbers.");
                    else
                        dropdown.entries[index] = new DropdownEntry(GetFirstUnusedDropdownID(), displayName);

                    this._postChangeValueCallback(true);
                }
            };

            // Element height
            m_DropdownReorderableList.elementHeightCallback = (int indexer) =>
            {
                return m_DropdownReorderableList.elementHeight;
            };

            // Can add
            m_DropdownReorderableList.onCanAddCallback = (ReorderableList list) =>
            {
                return true;
            };

            // Can remove
            m_DropdownReorderableList.onCanRemoveCallback = (ReorderableList list) =>
            {
                return list.count > DropdownNode.k_MinEnumEntries;
            };

            // Add callback delegates
            m_DropdownReorderableList.onSelectCallback += DropdownSelectEntry;
            m_DropdownReorderableList.onAddCallback += DropdownAddEntry;
            m_DropdownReorderableList.onRemoveCallback += DropdownRemoveEntry;
            m_DropdownReorderableList.onReorderCallback += DropdownReorderEntries;
        }

        void DropdownSelectEntry(ReorderableList list)
        {
            m_DropdownSelectedIndex = list.index;
        }

        int GetFirstUnusedDropdownID()
        {
            if (!(shaderInput is ShaderDropdown dropdown))
                return 0;

            List<int> ids = new List<int>();

            foreach (DropdownEntry dropdownEntry in dropdown.entries)
            {
                ids.Add(dropdownEntry.id);
            }

            for (int x = 1; ; x++)
            {
                if (!ids.Contains(x))
                    return x;
            }
        }

        void DropdownAddEntry(ReorderableList list)
        {
            if (!(shaderInput is ShaderDropdown dropdown))
                return;

            this._preChangeValueCallback("Add Dropdown Entry");

            int index = GetFirstUnusedDropdownID();

            var displayName = GetDuplicateSafeDropdownDisplayName(index, "New");

            // Add new entry
            dropdown.entries.Add(new DropdownEntry(index, displayName));

            // Update GUI
            this._postChangeValueCallback(true);
            this._dropdownChangedCallback();
            m_DropdownSelectedIndex = list.list.Count - 1;
        }

        void DropdownRemoveEntry(ReorderableList list)
        {
            if (!(shaderInput is ShaderDropdown dropdown))
                return;

            this._preChangeValueCallback("Remove Dropdown Entry");

            // Remove entry
            m_DropdownSelectedIndex = list.index;
            var selectedEntry = (DropdownEntry)m_DropdownReorderableList.list[list.index];
            dropdown.entries.Remove(selectedEntry);

            // Clamp value within new entry range
            int value = Mathf.Clamp(dropdown.value, 0, dropdown.entries.Count - 1);
            dropdown.value = value;

            this._postChangeValueCallback(true);
            this._dropdownChangedCallback();
            m_DropdownSelectedIndex = m_DropdownSelectedIndex >= list.list.Count - 1 ? list.list.Count - 1 : m_DropdownSelectedIndex;
        }

        void DropdownReorderEntries(ReorderableList list)
        {
            var index = m_Dropdown.IndexOfId(m_DropdownId);
            if (index != m_Dropdown.value)
                m_Dropdown.value = index;
            this._postChangeValueCallback(true);
        }

        public string GetDuplicateSafeDropdownDisplayName(int id, string name)
        {
            var entryList = m_DropdownReorderableList.list as List<DropdownEntry>;
            return GraphUtil.SanitizeName(entryList.Where(p => p.id != id).Select(p => p.displayName), "{0} {1}", name, m_DisplayNameDisallowedPattern);
        }

        int FindDuplicateDropdownDisplayNameIndex(int id, string displayName)
        {
            var entryList = m_DropdownReorderableList.list as List<DropdownEntry>;
            return entryList.FindIndex(entry => entry.id != id && entry.displayName == displayName);
        }
    }
}
