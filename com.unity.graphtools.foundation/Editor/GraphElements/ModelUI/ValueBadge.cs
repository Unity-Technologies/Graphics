using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A <see cref="Badge"/> to display a value near a port.
    /// </summary>
    public class ValueBadge : Badge
    {
        public new static readonly string ussClassName = "ge-value-badge";
        static VisualTreeAsset s_ValueTemplate;

        protected Label m_TextElement;
        protected Image m_Image;

        protected void SetBadgeColor(Color color)
        {
            m_Image.tintColor = color;

            style.borderLeftColor = color;
            style.borderRightColor = color;
            style.borderTopColor = color;
            style.borderBottomColor = color;
        }

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            base.BuildElementUI();

            if (s_ValueTemplate == null)
                s_ValueTemplate = GraphElementHelper.LoadUXML("ValueBadge.uxml");

            s_ValueTemplate.CloneTree(this);
            m_TextElement = this.SafeQ<Label>("desc");
            m_Image = this.SafeQ<Image>();
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            var valueModel = BadgeModel as IValueBadgeModel;
            Assert.IsNotNull(valueModel);

            var portModel = BadgeModel.ParentModel;
            Assert.IsNotNull(portModel);
            var port = portModel.GetUI<Port>(View);
            Assert.IsNotNull(port);

            SetBadgeColor(port.PortColor);
            m_TextElement.text = valueModel.DisplayValue;
        }

        /// <inheritdoc />
        protected override void Attach()
        {
            var valueModel = Model as IValueBadgeModel;
            var portModel = valueModel?.ParentPortModel;
            var port = portModel?.GetUI<Port>(View);
            var cap = port?.SafeQ(className: "ge-port__cap") ?? port;
            if (cap != null)
            {
                var alignment = portModel.Direction == PortDirection.Output ? SpriteAlignment.BottomRight : SpriteAlignment.BottomLeft;
                AttachTo(cap, alignment);
            }
        }
    }
}
