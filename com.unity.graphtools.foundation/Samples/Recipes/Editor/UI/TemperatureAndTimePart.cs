using System;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class TemperatureAndTimePart : BaseModelViewPart
    {
        public static readonly string ussClassName = "ge-sample-bake-node-part";
        public static readonly string temperatureLabelName = "temperature";
        public static readonly string durationLabelName = "duration";

        public static TemperatureAndTimePart Create(string name, IModel model, IModelView modelUI, string parentClassName)
        {
            if (model is INodeModel)
            {
                return new TemperatureAndTimePart(name, model, modelUI, parentClassName);
            }

            return null;
        }

        VisualElement TemperatureAndTimeContainer { get; set; }
        EditableLabel TemperatureLabel { get; set; }
        EditableLabel DurationLabel { get; set; }

        public override VisualElement Root => TemperatureAndTimeContainer;

        TemperatureAndTimePart(string name, IModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
        }

        protected override void BuildPartUI(VisualElement container)
        {
            if (!(m_Model is BakeNodeModel))
                return;

            TemperatureAndTimeContainer = new VisualElement { name = PartName };
            TemperatureAndTimeContainer.AddToClassList(ussClassName);
            TemperatureAndTimeContainer.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            TemperatureLabel = new EditableLabel { name = temperatureLabelName };
            TemperatureLabel.RegisterCallback<ChangeEvent<string>>(OnChangeTemperature);
            TemperatureLabel.AddToClassList(ussClassName.WithUssElement("temperature"));
            TemperatureLabel.AddToClassList(m_ParentClassName.WithUssElement("temperature"));
            TemperatureAndTimeContainer.Add(TemperatureLabel);

            DurationLabel = new EditableLabel { name = durationLabelName };
            DurationLabel.RegisterCallback<ChangeEvent<string>>(OnChangeTime);
            DurationLabel.AddToClassList(ussClassName.WithUssElement("temperature"));
            DurationLabel.AddToClassList(m_ParentClassName.WithUssElement("temperature"));
            TemperatureAndTimeContainer.Add(DurationLabel);

            container.Add(TemperatureAndTimeContainer);
        }

        void OnChangeTemperature(ChangeEvent<string> evt)
        {
            if (!(m_Model is BakeNodeModel bakeNodeModel))
                return;

            var regex = new Regex(@"(\d+)\s*(.*)");
            var match = regex.Match(evt.newValue);

            if (match.Groups.Count > 1 && int.TryParse(match.Groups[1].Value, out var v))
            {
                var unit = TemperatureUnit.Celsius;
                if (match.Groups.Count > 2)
                {
                    if (match.Groups[2].Value.Length == 1)
                    {
                        switch (match.Groups[2].Value.ToLower())
                        {
                            case "f":
                                unit = TemperatureUnit.Fahrenheit;
                                break;
                            case "k":
                                unit = TemperatureUnit.Kelvin;
                                break;
                            case "c":
                                unit = TemperatureUnit.Celsius;
                                break;
                        }
                    }
                    else
                    {
                        if (String.Equals(match.Groups[2].Value, TemperatureUnit.Celsius.ToString(), StringComparison.CurrentCultureIgnoreCase))
                            unit = TemperatureUnit.Celsius;
                        else if (String.Equals(match.Groups[2].Value, TemperatureUnit.Fahrenheit.ToString(), StringComparison.CurrentCultureIgnoreCase))
                            unit = TemperatureUnit.Celsius;
                        else if (String.Equals(match.Groups[2].Value, TemperatureUnit.Kelvin.ToString(), StringComparison.CurrentCultureIgnoreCase))
                            unit = TemperatureUnit.Celsius;
                    }
                }

                m_OwnerElement.RootView.Dispatch(new SetTemperatureCommand(
                        new Temperature { Value = v, Unit = unit }, bakeNodeModel));
            }
            else
            {
                UpdatePartFromModel();
            }
        }

        void OnChangeTime(ChangeEvent<string> evt)
        {
            if (!(m_Model is BakeNodeModel bakeNodeModel))
                return;

            if (int.TryParse(evt.newValue, out var v))
                m_OwnerElement.RootView.Dispatch(new SetDurationCommand(v, nodes: bakeNodeModel));
        }

        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();

            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.unity.graphtools.foundation/Samples/Recipes/Editor/UI/Stylesheets/BakeNodePart.uss");
            if (stylesheet != null)
            {
                TemperatureAndTimeContainer.styleSheets.Add(stylesheet);
            }
        }

        protected override void UpdatePartFromModel()
        {
            if (!(m_Model is BakeNodeModel bakeNodeModel))
                return;

            TemperatureLabel.SetValueWithoutNotify($"{bakeNodeModel.Temperature.Value} {bakeNodeModel.Temperature.Unit}");
            DurationLabel.SetValueWithoutNotify($"{bakeNodeModel.Duration} min.");
        }
    }
}
