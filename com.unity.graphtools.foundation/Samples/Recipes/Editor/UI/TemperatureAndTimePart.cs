using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class TemperatureAndTimePart : BaseModelUIPart
    {
        public static readonly string ussClassName = "ge-sample-bake-node-part";
        public static readonly string temperatureLabelName = "temperature";
        public static readonly string durationLabelName = "duration";

        public static TemperatureAndTimePart Create(string name, IGraphElementModel model, IModelUI modelUI, string parentClassName)
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

        TemperatureAndTimePart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
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

            if (int.TryParse(evt.newValue, out var v))
                m_OwnerElement.View.Dispatch(new SetTemperatureCommand(v, bakeNodeModel));
        }

        void OnChangeTime(ChangeEvent<string> evt)
        {
            if (!(m_Model is BakeNodeModel bakeNodeModel))
                return;

            if (int.TryParse(evt.newValue, out var v))
                m_OwnerElement.View.Dispatch(new SetDurationCommand(v, nodes: bakeNodeModel));
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

            TemperatureLabel.SetValueWithoutNotify($"{bakeNodeModel.Temperature} C");
            DurationLabel.SetValueWithoutNotify($"{bakeNodeModel.Duration} min.");
        }
    }
}
