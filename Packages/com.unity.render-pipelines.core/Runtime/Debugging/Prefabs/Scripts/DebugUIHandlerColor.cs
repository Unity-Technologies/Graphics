using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for color widget.
    /// </summary>
    public class DebugUIHandlerColor : DebugUIHandlerWidget
    {
        /// <summary>Name of the widget.</summary>
        public Text nameLabel;
        /// <summary>Toggles a visibility in the user interface.</summary>
        public UIFoldout valueToggle;
        /// <summary>Color image.</summary>
        public Image colorImage;

        /// <summary>Red float field.</summary>
        public DebugUIHandlerIndirectFloatField fieldR;
        /// <summary>Green float field.</summary>
        public DebugUIHandlerIndirectFloatField fieldG;
        /// <summary>Blue float field.</summary>
        public DebugUIHandlerIndirectFloatField fieldB;
        /// <summary>Alpha float field.</summary>
        public DebugUIHandlerIndirectFloatField fieldA;

        DebugUI.ColorField m_Field;
        DebugUIHandlerContainer m_Container;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            m_Field = CastWidget<DebugUI.ColorField>();
            m_Container = GetComponent<DebugUIHandlerContainer>();
            nameLabel.text = m_Field.displayName;

            fieldR.getter = () => m_Field.GetValue().r;
            fieldR.setter = x => SetValue(x, r: true);
            fieldR.nextUIHandler = fieldG;
            SetupSettings(fieldR);

            fieldG.getter = () => m_Field.GetValue().g;
            fieldG.setter = x => SetValue(x, g: true);
            fieldG.previousUIHandler = fieldR;
            fieldG.nextUIHandler = fieldB;
            SetupSettings(fieldG);

            fieldB.getter = () => m_Field.GetValue().b;
            fieldB.setter = x => SetValue(x, b: true);
            fieldB.previousUIHandler = fieldG;
            fieldB.nextUIHandler = m_Field.showAlpha ? fieldA : null;
            SetupSettings(fieldB);

            fieldA.gameObject.SetActive(m_Field.showAlpha);
            fieldA.getter = () => m_Field.GetValue().a;
            fieldA.setter = x => SetValue(x, a: true);
            fieldA.previousUIHandler = fieldB;
            SetupSettings(fieldA);

            UpdateColor();
        }

        void SetValue(float x, bool r = false, bool g = false, bool b = false, bool a = false)
        {
            var color = m_Field.GetValue();
            if (r) color.r = x;
            if (g) color.g = x;
            if (b) color.b = x;
            if (a) color.a = x;
            m_Field.SetValue(color);
            UpdateColor();
        }

        void SetupSettings(DebugUIHandlerIndirectFloatField field)
        {
            field.parentUIHandler = this;
            field.incStepGetter = () => m_Field.incStep;
            field.incStepMultGetter = () => m_Field.incStepMult;
            field.decimalsGetter = () => m_Field.decimals;
            field.Init();
        }

        /// <summary>
        /// OnSelection implementation.
        /// </summary>
        /// <param name="fromNext">True if the selection wrapped around.</param>
        /// <param name="previous">Previous widget.</param>
        /// <returns>State of the widget.</returns>
        public override bool OnSelection(bool fromNext, DebugUIHandlerWidget previous)
        {
            if (fromNext || valueToggle.isOn == false)
            {
                nameLabel.color = colorSelected;
            }
            else if (valueToggle.isOn)
            {
                if (m_Container.IsDirectChild(previous))
                {
                    nameLabel.color = colorSelected;
                }
                else
                {
                    var lastItem = m_Container.GetLastItem();
                    DebugManager.instance.ChangeSelection(lastItem, false);
                }
            }

            return true;
        }

        /// <summary>
        /// OnDeselection implementation.
        /// </summary>
        public override void OnDeselection()
        {
            nameLabel.color = colorDefault;
        }

        /// <summary>
        /// OnIncrement implementation.
        /// </summary>
        /// <param name="fast">True if incrementing fast.</param>
        public override void OnIncrement(bool fast)
        {
            valueToggle.isOn = true;
        }

        /// <summary>
        /// OnDecrement implementation.
        /// </summary>
        /// <param name="fast">Trye if decrementing fast.</param>
        public override void OnDecrement(bool fast)
        {
            valueToggle.isOn = false;
        }

        /// <summary>
        /// OnAction implementation.
        /// </summary>
        public override void OnAction()
        {
            valueToggle.isOn = !valueToggle.isOn;
        }

        internal void UpdateColor()
        {
            if (colorImage != null)
                colorImage.color = m_Field.GetValue();
        }

        /// <summary>
        /// Next implementation.
        /// </summary>
        /// <returns>Next child.</returns>
        public override DebugUIHandlerWidget Next()
        {
            if (!valueToggle.isOn || m_Container == null)
                return base.Next();

            var firstChild = m_Container.GetFirstItem();

            if (firstChild == null)
                return base.Next();

            return firstChild;
        }
    }
}
