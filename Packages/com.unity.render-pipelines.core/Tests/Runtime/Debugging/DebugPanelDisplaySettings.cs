#if ENABLE_UIELEMENTS_MODULE && (UNITY_EDITOR || DEVELOPMENT_BUILD)
#define ENABLE_RENDERING_DEBUGGER_UI
#endif

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

using NameAndTooltip = UnityEngine.Rendering.DebugUI.Widget.NameAndTooltip;

namespace UnityEngine.Rendering.Tests
{
    class DebugPanelDisplaySettings : DebugDisplaySettings<DebugPanelDisplaySettings>
    {
        const string k_PanelName = "Debug Panel";
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        public static void RegisterPanel()
        {
            var data = new DebugPanelDisplaySettings.DebugPanelDisplaySettingsData();
            IDebugDisplaySettingsPanelDisposable disposableSettingsPanel = data.CreatePanel();

            DebugUI.Widget[] panelWidgets = disposableSettingsPanel.Widgets;

            DebugUI.Panel panel = DebugManager.instance.GetPanel(
                displayName: disposableSettingsPanel.PanelName,
                createIfNull: true,
                groupIndex: (disposableSettingsPanel is DebugDisplaySettingsPanel debugDisplaySettingsPanel) ? debugDisplaySettingsPanel.Order : 0);

            if (DocumentationUtils.TryGetHelpURL(disposableSettingsPanel.GetType(), out var documentationUrl))
                panel.documentationUrl = documentationUrl;

            ObservableList<DebugUI.Widget> panelChildren = panel.children;

            panel.flags = disposableSettingsPanel.Flags;
            panelChildren.Add(panelWidgets);
        }
#endif
        public class CustomProgressBarField : DebugUI.Field<float>
        {
            public Func<float> min = () => 0f;
            public Func<float> max = () => 100f;
            public string unit = "%";
            public Color lowColor = Color.red;
            public Color highColor = Color.green;
            public bool isEditable = true;


#if ENABLE_RENDERING_DEBUGGER_UI
            protected override VisualElement Create()
            {
                var container = new VisualElement();
                container.AddToClassList("debug-progress-bar-container");
                container.AddToClassList(UIElements.BaseField<float>.ussClassName);
                container.AddToClassList("unity-inspector-element");
                container.AddToClassList("unity-base-field__inspector-field");
                container.style.flexDirection = FlexDirection.Row;

                var label = new Label(displayName);
                label.AddToClassList(UIElements.BaseField<float>.labelUssClassName);
                container.Add(label);

                var progressContainer = new VisualElement();
                progressContainer.style.height = 20;
                progressContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                progressContainer.style.borderBottomLeftRadius = 4;
                progressContainer.style.borderBottomRightRadius = 4;
                progressContainer.style.borderTopLeftRadius = 4;
                progressContainer.style.borderTopRightRadius = 4;
                progressContainer.style.flexGrow = 1;
                progressContainer.AddToClassList(UIElements.BaseField<float>.inputUssClassName);

                var progressBar = new VisualElement();
                progressBar.style.backgroundColor = Color.green;
                progressBar.style.height = Length.Percent(100);

                var valueLabel = new Label();
                valueLabel.style.position = Position.Absolute;
                valueLabel.style.alignSelf = Align.Center;
                valueLabel.style.color = Color.white;

                progressContainer.Add(progressBar);
                progressContainer.Add(valueLabel);
                container.Add(progressContainer);

                // Update every frame
                container.schedule.Execute(() =>
                {
                    float value = GetValue();
                    float normalized = Mathf.InverseLerp(min(), max(), value);
                    progressBar.style.width = Length.Percent(normalized * 100);
                    progressBar.style.backgroundColor = Color.Lerp(lowColor, highColor, normalized);
                    valueLabel.text = $"{value:F1}{unit}";
                }).Every(16); // ~60fps

                // Make the progress bar interactive
                if (isEditable && !m_Context.IsAnyRuntimeContext())
                {
                    progressContainer.style.cursor = StyleKeyword.Auto;

                    // Register click/drag handlers
                    progressContainer.RegisterCallback<PointerDownEvent>(evt =>
                    {
                        UpdateValueFromPosition(evt.localPosition.x, progressContainer.resolvedStyle.width);
                        progressContainer.CapturePointer(evt.pointerId);
                        evt.StopPropagation();
                    });

                    progressContainer.RegisterCallback<PointerMoveEvent>(evt =>
                    {
                        if (progressContainer.HasPointerCapture(evt.pointerId))
                        {
                            UpdateValueFromPosition(evt.localPosition.x, progressContainer.resolvedStyle.width);
                            evt.StopPropagation();
                        }
                    });

                    progressContainer.RegisterCallback<PointerUpEvent>(evt =>
                    {
                        if (progressContainer.HasPointerCapture(evt.pointerId))
                        {
                            progressContainer.ReleasePointer(evt.pointerId);
                            evt.StopPropagation();
                        }
                    });

                    progressContainer.RegisterCallback<PointerCancelEvent>(evt =>
                    {
                        if (progressContainer.HasPointerCapture(evt.pointerId))
                        {
                            progressContainer.ReleasePointer(evt.pointerId);
                        }
                    });
                }


                container.AddToClassList(UIElements.BaseField<float>.alignedFieldUssClassName);
                return container;
            }
#endif

            private void UpdateValueFromPosition(float localX, float width)
            {
                if (width <= 0) return;

                float normalized = Mathf.Clamp01(localX / width);
                float minValue = min();
                float maxValue = max();
                float newValue = Mathf.Lerp(minValue, maxValue, normalized);

                SetValue(newValue);
            }
        }


        public class DebugPanelDisplaySettingsData : IDebugDisplaySettingsData, ISerializedDebugDisplaySettings
        {
            #region IDebugDisplaySettingsData
            public bool AreAnySettingsActive => false;

            public IDebugDisplaySettingsPanelDisposable CreatePanel()
            {
                return new SettingsPanel(this);
            }

            #endregion

            // Numbers
            public int intField { get; set; } = 13;
            public int intMinMaxField { get; set; } = 13;
            public uint uintField { get; set; } = 23;
            public uint uintMinMaxField { get; set; } = 23;
            public float floatField { get; set; } = 7.7f;
            public float floatMinMaxField { get; set; } = 7.7f;

            // Bools
            public bool boolField { get; set; } = true;
            public bool historyBoolField1 { get; set; } = true;
            public bool historyBoolField2 { get; set; } = true;
            public bool historyBoolField3 { get; set; } = true;

            // Enums
            public enum EnumValues
            {
                None = 0,
                TypeA, TypeB, TypeC,
            }
            public EnumValues enumField { get; set; }

            public EnumValues enumField1 { get; set; }
            public EnumValues enumField2 { get; set; }
            public EnumValues enumField3 { get; set; }

            [Flags]
            public enum BitFlags
            {
                None,
                FlagA = 0x1,
                FlagB = 0x2,
                FlagC = 0x4,
                FlagD = 0x8
            }

            public BitFlags bitField { get; set; }

            // Vectors
            public Color colorField { get; set; } = Color.darkMagenta;
            public Vector2 vector2Field { get; set; }
            public Vector3 vector3Field { get; set; }
            public Vector4 vector4Field { get; set; }

            // Object
            public Object objectField { get; set; }

            //public Object[] objectListField { get; set; }//= new Object[]{Camera.main, Camera.main, Camera.main };
            public Object objectPopupField { get; set; }

            public float value1 { get; set; } = 1.123456f;

            public float value2 { get; set; } = 2.123456f;

            public float value3 { get; set; } = 3.123456f;

            public Camera camerasPopupField { get; set; }

            public uint renderingLayersField { get; set; } = 0;
            public Vector4[] renderingLayersColors = new Vector4[]
            {
                new Vector4(230, 159, 0) / 255,
                new Vector4(86, 180, 233) / 255,
                new Vector4(255, 182, 291) / 255,
                new Vector4(0, 158, 115) / 255,
                new Vector4(240, 228, 66) / 255,
                new Vector4(0, 114, 178) / 255,
                new Vector4(213, 94, 0) / 255,
                new Vector4(170, 68, 170) / 255,
                new Vector4(1.0f, 0.5f, 0.5f),
                new Vector4(0.5f, 1.0f, 0.5f),
                new Vector4(0.5f, 0.5f, 1.0f),
                new Vector4(0.5f, 1.0f, 1.0f),
                new Vector4(0.75f, 0.25f, 1.0f),
                new Vector4(0.25f, 1.0f, 0.75f),
                new Vector4(0.25f, 0.25f, 0.75f),
                new Vector4(0.75f, 0.25f, 0.25f),
            };

            const int k_NumRows = 10;
            const int k_NumColumns = 3;

            public float customProgress { get; set; } = 13;

            static class Strings
            {
                public static readonly NameAndTooltip IntField = new() { name = "Int Field", tooltip = "Debug Field for number int type." };
                public static readonly NameAndTooltip IntMinMaxField = new() { name = "Int Min Max Field", tooltip = "Debug Field for number int type with min-max value." };
                public static readonly NameAndTooltip UIntField = new() { name = "UInt Field", tooltip = "Debug Field for number uint type." };
                public static readonly NameAndTooltip UIntMinMaxField = new() { name = "UInt Min Max Field", tooltip = "Debug Field for number uint type with min-max value." };
                public static readonly NameAndTooltip FloatField = new() { name = "Float Field", tooltip = "Debug Field for number float type." };
                public static readonly NameAndTooltip FloatMinMaxField = new() { name = "Float Min Max Field", tooltip = "Debug Field for number float type with min-max value." };
                public static readonly NameAndTooltip BoolField = new() { name = "Bool Field", tooltip = "Debug Field for bool value." };
                public static readonly NameAndTooltip HistoryBoolField = new() { name = "History Bool Field", tooltip = "Debug Field for history bool value." };
                public static readonly NameAndTooltip EnumField = new() { name = "Enum Field", tooltip = "Debug Field for enum value." };
                public static readonly NameAndTooltip HistoryEnumField = new() { name = "History Enum Field", tooltip = "Debug Field for history enum value." };
                public static readonly NameAndTooltip BitField = new() { name = "Bit Field", tooltip = "Debug Field for bit field value." };
                public static readonly NameAndTooltip ColorField = new() { name = "Color Field", tooltip = "Debug Field for Color field value." };
                public static readonly NameAndTooltip Vector2Field = new() { name = "Vector2 Field", tooltip = "Debug Field for Vector2 field value." };
                public static readonly NameAndTooltip Vector3Field = new() { name = "Vector3 Field", tooltip = "Debug Field for Vector3 field value." };
                public static readonly NameAndTooltip Vector4Field = new() { name = "Vector4 Field", tooltip = "Debug Field for Vector4 field value." };
                public static readonly NameAndTooltip ObjectField = new() { name = "Object Field", tooltip = "Debug Field for Object field value." };
                public static readonly NameAndTooltip ObjectListField = new() { name = "Object List Field", tooltip = "Debug Field for Object List field value." };
                public static readonly NameAndTooltip ObjectPopupField = new() { name = "Object Popup Field", tooltip = "Debug Field for Object Popup field value." };
                public static readonly NameAndTooltip Value = new() { name = "Value", tooltip = "Value." };
                public static readonly NameAndTooltip ValueTuple = new() { name = "Value Tuple", tooltip = "Value Tuple." };
                public static readonly NameAndTooltip CamerasPopupField = new() { name = "Cameras Popup Field", tooltip = "Debug Field for Cameras Popup field value." };
                public static readonly NameAndTooltip RenderingLayersField = new() { name = "Rendering Layers Field", tooltip = "Debug Field for Rendering Layers field value." };
                public static readonly NameAndTooltip MessageBox = new() { name = "Message Box", tooltip = "Message Box." };
                public static readonly NameAndTooltip DebugTable = new() { name = "Pollitos", tooltip = "Table." };
                public static readonly NameAndTooltip Button = new() { name = "Button", tooltip = "Button." };
                public static readonly NameAndTooltip ProgressBar = new() { name = "ProgressBar", tooltip = "ProgressBar." };
                public static readonly NameAndTooltip NumberFields = new() { name = "Hidden", tooltip = "Hidden fields." };
                public static readonly string TableRow = "Row";
            }

            internal static class WidgetFactory
            {
                internal static DebugUI.Widget CreateIntField(DebugPanelDisplaySettingsData data) => new DebugUI.IntField
                {
                    nameAndTooltip = Strings.IntField,
                    getter = () => (int)data.intField,
                    setter = (value) => data.intField = value,
                    incStep = 1,
                    incStepMult = 10
                };

                internal static DebugUI.Widget CreateIntMinMaxField(DebugPanelDisplaySettingsData data) => new DebugUI.IntField
                {
                    nameAndTooltip = Strings.IntMinMaxField,
                    getter = () => (int)data.intMinMaxField,
                    setter = (value) => data.intMinMaxField = value,
                    incStep = 1,
                    incStepMult = 10,
                    min = () => -100,
                    max = () => 100
                };

                internal static DebugUI.Widget CreateUIntField(DebugPanelDisplaySettingsData data) => new DebugUI.UIntField
                {
                    nameAndTooltip = Strings.UIntField,
                    getter = () => (uint)data.uintField,
                    setter = (value) => data.uintField = value,
                    incStep = 1u,
                    incStepMult = 10u
                };

                internal static DebugUI.Widget CreateUIntMinMaxField(DebugPanelDisplaySettingsData data) => new DebugUI.UIntField
                {
                    nameAndTooltip = Strings.UIntMinMaxField,
                    getter = () => (uint)data.uintMinMaxField,
                    setter = (value) => data.uintMinMaxField = value,
                    incStep = 1u,
                    incStepMult = 10u,
                    min = () => 1u,
                    max = () => 100u
                };

                internal static DebugUI.Widget CreateFloatField(DebugPanelDisplaySettingsData data) => new DebugUI.FloatField
                {
                    nameAndTooltip = Strings.FloatField,
                    getter = () => (float)data.floatField,
                    setter = (value) => data.floatField = value,
                    incStep = 1.0f,
                    incStepMult = 10.0f
                };

                internal static DebugUI.Widget CreateFloatMinMaxField(DebugPanelDisplaySettingsData data) => new DebugUI.FloatField
                {
                    nameAndTooltip = Strings.FloatMinMaxField,
                    getter = () => (float)data.floatMinMaxField,
                    setter = (value) => data.floatMinMaxField = value,
                    incStep = 1.0f,
                    incStepMult = 10.0f,
                    min = () => -100.0f,
                    max = () => 100.0f
                };

                internal static DebugUI.Widget CreateNumberHiddenFields(DebugPanelDisplaySettingsData data) => new DebugUI.Container()
                {
                    isHiddenCallback = () => data.intMinMaxField < 0 && data.floatMinMaxField < 0,
                    displayName = Strings.NumberFields.name,
                    children =
                    {
                        WidgetFactory.CreateIntField(data),
                    }
                };

                internal static DebugUI.Widget CreateBoolField(DebugPanelDisplaySettingsData data) => new DebugUI.BoolField
                {
                    nameAndTooltip = Strings.BoolField,
                    getter = () => (bool)data.boolField,
                    setter = (value) => data.boolField = value,
                };

                internal static DebugUI.Widget CreateBoolHiddenFields(DebugPanelDisplaySettingsData data) => new DebugUI.Container()
                {
                    isHiddenCallback = () => data.boolField,
                    displayName = Strings.NumberFields.name,
                    children =
                    {
                        WidgetFactory.CreateBoolField(data),
                    }
                };

                internal static DebugUI.Widget CreateHistoryBoolField(DebugPanelDisplaySettingsData data) => new DebugUI.HistoryBoolField
                {
                    nameAndTooltip = Strings.HistoryBoolField,
                    getter = () => (bool)data.historyBoolField1,
                    setter = (value) =>
                    {
                        data.historyBoolField1 = value;
                        data.historyBoolField2 = value;
                        data.historyBoolField3 = value;
                    },
                    historyGetter = new Func<bool>[]
                    {
                        () => data.historyBoolField1,
                        () => data.historyBoolField2,
                        () => data.historyBoolField3
                    }
                };

                internal static DebugUI.Widget CreateEnumField(DebugPanelDisplaySettingsData data) => new DebugUI.EnumField
                {
                    nameAndTooltip = Strings.EnumField,
                    autoEnum = typeof(EnumValues),
                    getter = () => (int)data.enumField,
                    setter = (value) => data.enumField = (EnumValues)value,
                    getIndex = () => (int)data.enumField,
                    setIndex = (value) => data.enumField = (EnumValues)value
                };

                internal static DebugUI.Widget CreateEnumHiddenFields(DebugPanelDisplaySettingsData data) => new DebugUI.Container()
                {
                    isHiddenCallback = () => data.enumField == EnumValues.None,
                    displayName = Strings.NumberFields.name,
                    children =
                    {
                        WidgetFactory.CreateEnumField(data),
                    }
                };

                internal static DebugUI.Widget CreateHistoryEnumField(DebugPanelDisplaySettingsData data) => new DebugUI.HistoryEnumField
                {
                    nameAndTooltip = Strings.HistoryEnumField,
                    autoEnum = typeof(EnumValues),
                    getter = () => (int)data.enumField1,
                    setter = (value) =>
                    {
                        data.enumField1 = (EnumValues)value;
                        data.enumField1 = (EnumValues)value;
                        data.enumField1 = (EnumValues)value;
                    },
                    getIndex = () => (int)data.enumField1,
                    setIndex = (int a) => { },
                    historyIndexGetter = new Func<int>[]
                    {
                        () => (int)data.enumField1,
                        () => (int)data.enumField2,
                        () => (int)data.enumField3
                    }
                };

                internal static DebugUI.Widget CreateBitField(DebugPanelDisplaySettingsData data) => new DebugUI.BitField
                {
                    nameAndTooltip = Strings.BitField,
                    getter = () => data.bitField,
                    setter = (value) => data.bitField = (BitFlags)value,
                    enumType = typeof(BitFlags),
                };

                internal static DebugUI.Widget CreateColorField(DebugPanelDisplaySettingsData data) => new DebugUI.ColorField
                {
                    nameAndTooltip = Strings.ColorField,
                    getter = () => data.colorField,
                    setter = (value) => data.colorField = value,
                };

                internal static DebugUI.Widget CreateVector2Field(DebugPanelDisplaySettingsData data) => new DebugUI.Vector2Field
                {
                    nameAndTooltip = Strings.Vector2Field,
                    getter = () => data.vector2Field,
                    setter = (value) => data.vector2Field = value,
                };

                internal static DebugUI.Widget CreateVector3Field(DebugPanelDisplaySettingsData data) => new DebugUI.Vector3Field
                {
                    nameAndTooltip = Strings.Vector3Field,
                    getter = () => data.vector3Field,
                    setter = (value) => data.vector3Field = value,
                };

                internal static DebugUI.Widget CreateVector4Field(DebugPanelDisplaySettingsData data) => new DebugUI.Vector4Field
                {
                    nameAndTooltip = Strings.Vector4Field,
                    getter = () => data.vector4Field,
                    setter = (value) => data.vector4Field = value,
                };

                internal static DebugUI.Widget CreateObjectField(DebugPanelDisplaySettingsData data) => new DebugUI.ObjectField
                {
                    nameAndTooltip = Strings.ObjectField,
                    getter = () => data.objectField,
                    setter = (value) => data.objectField = value
                };

                internal static DebugUI.Widget CreateObjectPopupField(DebugPanelDisplaySettingsData data) => new DebugUI.ObjectPopupField
                {
                    nameAndTooltip = Strings.ObjectPopupField,
                    getter = () => data.objectPopupField,
                    setter = (value) => data.objectPopupField = value,
                    getObjects = () =>
                    {
                        List<Object> objects = new List<Object>();
                        return objects;
                    }
                };

                internal static DebugUI.Widget CreateValue(DebugPanelDisplaySettingsData data) => new DebugUI.Value
                {
                    nameAndTooltip = Strings.Value,
                    getter = () => data.value1
                };

                internal static DebugUI.Widget CreateValueTuple(DebugPanelDisplaySettingsData data) => new DebugUI.ValueTuple
                {
                    nameAndTooltip = Strings.ValueTuple,
                    values = new[]
                    {
                        new DebugUI.Value { refreshRate = 0.1f, formatString = null, getter = () => data.value1 },
                        new DebugUI.Value { refreshRate = 0.5f, formatString = "{0:F1}", getter = () => data.value2 },
                        new DebugUI.Value { refreshRate = 1.0f, formatString = "{0:F4}", getter = () => data.value3 },
                    }
                };

                internal static DebugUI.Widget CreateCamerasField(DebugPanelDisplaySettingsData data) => new DebugUI.CameraSelector
                {
                    nameAndTooltip = Strings.CamerasPopupField,
                    getter = () => data.camerasPopupField,
                    setter = (value) => data.camerasPopupField = value as Camera,
                };

                internal static DebugUI.Widget CreateRenderingLayersField(DebugPanelDisplaySettingsData data) => new DebugUI.RenderingLayerField
                {
                    nameAndTooltip = Strings.RenderingLayersField,
                    getter = () => data.renderingLayersField,
                    setter = (value) => data.renderingLayersField = value,
                    getRenderingLayerColor = index => data.renderingLayersColors[index],
                    setRenderingLayerColor = (value, index) => data.renderingLayersColors[index] = value,
                };

                internal static DebugUI.Widget CreateMessageBox(DebugPanelDisplaySettingsData data, DebugUI.MessageBox.Style style) => new DebugUI.MessageBox
                {
                    displayName = Strings.MessageBox.name + " " + style.ToString(),
                    style = style
                };

                internal static DebugUI.Table CreateTable(DebugPanelDisplaySettingsData data)
                {
                    var table = new DebugUI.Table()
                    {
                        displayName = Strings.DebugTable.name,
                        alternateRowColors = true,
                        isReadOnly = true
                    };

                    DebugUI.Table.Row[] rows = new DebugUI.Table.Row[k_NumRows];
                    for (int i = 0; i < k_NumRows; ++i)
                    {
                        rows[i] = new DebugUI.Table.Row { displayName = $"{Strings.TableRow} {i}" };
                    }

                    for (int i = 0; i < k_NumColumns; ++i)
                    {
                        for (int j = 0; j < k_NumRows; ++j)
                        {
                            DebugUI.Widget value = null;
                            switch (j)
                            {
                                case 0:
                                    value = new DebugUI.Value(){ getter = () => "text" };
                                    break;
                                case 1:
                                    value = new DebugUI.BoolField(){
                                        displayName = "",
                                        getter = () => (bool)data.boolField,
                                    };
                                    break;
                                case 2:
                                    value = new DebugUI.Value(){
                                        displayName = "",
                                        getter = () => data.boolField.ToString(),
                                    };
                                    break;
                                case 3:
                                    value = new DebugUI.Value(){
                                        displayName = "",
                                        getter = () => data.intField.ToString(),
                                    };
                                    break;
                                case 4:
                                    value = new DebugUI.Value(){
                                        displayName = "",
                                        getter = () => data.enumField.ToString(),
                                    };
                                    break;
                                case 5:
                                    value = new DebugUI.Value(){
                                        displayName = "",
                                        getter = () => data.vector3Field.ToString(),
                                    };
                                    break;
                                case 6:
                                    value = new DebugUI.Value(){
                                        displayName = "",
                                        getter = () => data.floatField.ToString(),
                                    };
                                    break;
                                case 7:
                                    value = new DebugUI.ColorField(){
                                        displayName = "color",
                                        getter = () => data.colorField,
                                        showPicker = false, };
                                    break;
                                case 8:
                                    value = new DebugUI.ObjectField(){
                                        displayName = "",
                                        getter = () => data.objectField, };
                                    break;
                                default:
                                    value = new DebugUI.Value(){ getter = () => "text" };
                                    break;
                            }

                            rows[j].children.Add(value);
                        }
                    }

                    for (int i = 0; i < k_NumRows; ++i)
                    {
                        table.children.Add(rows[i]);
                    }

                    return table;
                }

                internal static DebugUI.Widget CreateButton(DebugPanelDisplaySettingsData data) => new DebugUI.Button
                {
                    nameAndTooltip = Strings.Button,
                    action = () => { }
                };

                internal static DebugUI.Widget CreateProgressBar(DebugPanelDisplaySettingsData data) => new DebugUI.ProgressBarValue
                {
                    displayName = Strings.ProgressBar.name,
                    getter = () => data.floatField,
                    min = 0f,
                    max = 100f,
                };
            }

            [DisplayInfo(name = k_PanelName, order = int.MinValue)]
            internal class SettingsPanel : DebugDisplaySettingsPanel<DebugPanelDisplaySettingsData>
            {
                public SettingsPanel(DebugPanelDisplaySettingsData data)
                    : base(data)
                {
                    AddWidget(new DebugUI.Foldout
                    {
                        displayName = "Number Fields",
                        opened = true,
                        children =
                        {
                            WidgetFactory.CreateIntField(data),
                            WidgetFactory.CreateIntMinMaxField(data),
                            WidgetFactory.CreateUIntField(data),
                            WidgetFactory.CreateUIntMinMaxField(data),
                            WidgetFactory.CreateFloatField(data),
                            WidgetFactory.CreateFloatMinMaxField(data),
                            WidgetFactory.CreateNumberHiddenFields(data),
                        }
                    });

                    AddWidget(new DebugUI.Foldout
                    {
                        displayName = "Bool Fields",
                        opened = true,
                        children =
                        {
                            WidgetFactory.CreateBoolField(data),
                            WidgetFactory.CreateHistoryBoolField(data),
                            WidgetFactory.CreateBoolHiddenFields(data),
                        }
                    });

                    AddWidget(new DebugUI.Foldout
                    {
                        displayName = "Enum Fields",
                        opened = true,
                        children =
                        {
                            WidgetFactory.CreateEnumField(data),
                            WidgetFactory.CreateHistoryEnumField(data),
                            WidgetFactory.CreateBitField(data),
                            WidgetFactory.CreateEnumHiddenFields(data),
                        }
                    });

                    AddWidget(new DebugUI.Foldout
                    {
                        displayName = "Vector Fields",
                        opened = true,
                        children =
                        {
                            WidgetFactory.CreateColorField(data),
                            WidgetFactory.CreateVector2Field(data),
                            WidgetFactory.CreateVector3Field(data),
                            WidgetFactory.CreateVector4Field(data),
                        }
                    });

                    AddWidget(new DebugUI.Foldout
                    {
                        displayName = "Object Fields",
                        opened = true,
                        children =
                        {
                            WidgetFactory.CreateObjectField(data),
                            //WidgetFactory.CreateObjectListField(data),
                            WidgetFactory.CreateObjectPopupField(data),
                            WidgetFactory.CreateCamerasField(data),
                            WidgetFactory.CreateRenderingLayersField(data),
                        }
                    });

                    AddWidget(new DebugUI.Foldout
                    {
                        displayName = "Values",
                        opened = true,
                        children =
                        {
                            WidgetFactory.CreateValue(data),
                            WidgetFactory.CreateValueTuple(data)
                        }
                    });

                    AddWidget(new DebugUI.Foldout
                    {
                        displayName = "Table",
                        opened = true,
                        children =
                        {
                            WidgetFactory.CreateTable(data)
                        }
                    });

                    AddWidget(new DebugUI.Foldout
                    {
                        displayName = "Documentation URL",
                        opened = true,
                        children =
                        {
                        },
                        documentationUrl = "test"
                    });

                    AddWidget(new DebugUI.Foldout
                    {
                        displayName = "Context Menu",
                        opened = true,
                        children =
                        {
                        },
                        contextMenuItems = new List<DebugUI.Foldout.ContextMenuItem> { new DebugUI.Foldout.ContextMenuItem { displayName = "Option" } }
                    });

                    AddWidget(new DebugUI.Foldout
                    {
                        displayName = "Documentation + Context Menu",
                        opened = true,
                        children =
                        {
                        },
                        documentationUrl = "test",
                        contextMenuItems = new List<DebugUI.Foldout.ContextMenuItem> { new DebugUI.Foldout.ContextMenuItem { displayName = "Option" } }
                    });

                    AddWidget(new DebugUI.Foldout
                    {
                        displayName = "Messages",
                        opened = true,
                        children =
                        {
                            WidgetFactory.CreateMessageBox(data, DebugUI.MessageBox.Style.None),
                            WidgetFactory.CreateMessageBox(data, DebugUI.MessageBox.Style.Info),
                            WidgetFactory.CreateMessageBox(data, DebugUI.MessageBox.Style.Warning),
                            WidgetFactory.CreateMessageBox(data, DebugUI.MessageBox.Style.Error),
                        }
                    });

                    AddWidget(new DebugUI.Foldout
                    {
                        displayName = "Buttons",
                        opened = true,
                        children =
                        {
                            WidgetFactory.CreateButton(data),
                        }
                    });

                    AddWidget(new DebugUI.Foldout
                    {
                        displayName = "Custom Fields/Widgets",
                        opened = true,
                        children =
                        {
                             WidgetFactory.CreateProgressBar(data),
                             new CustomProgressBarField()
                             {
                                 displayName = "Progress Bar Editable",
                                 getter = () => data.customProgress,
                                 setter = (value) => data.customProgress = value,
                                 isEditable = true
                             },
                             new CustomProgressBarField()
                             {
                                 displayName = "Progress Bar",
                                 getter = () => data.customProgress,
                                 setter = (value) => data.customProgress = value,
                                 lowColor = Color.lightYellow, highColor = Color.black,
                                 isEditable = false

                             },
                             WidgetFactory.CreateFloatField(data)
                        }
                    });
                }
            }
        }

        public DebugPanelDisplaySettings()
        {
            Reset();
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            m_Settings.Clear();

            // Add them in an unsorted way
            Add(new DebugPanelDisplaySettingsData());
        }
    }
}

