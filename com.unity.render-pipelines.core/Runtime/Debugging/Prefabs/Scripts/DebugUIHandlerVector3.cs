using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for vector3 widget.
    /// </summary>
    public class DebugUIHandlerVector3 : DebugUIHandlerWidget
    {
        /// <summary>Name of the Vector3 field.</summary>
        public Text nameLabel;
        /// <summary>Value of the Vector3 toggle.</summary>
        public UIFoldout valueToggle;

        /// <summary>X float field.</summary>
        public DebugUIHandlerIndirectFloatField fieldX;
        /// <summary>Y float field.</summary>
        public DebugUIHandlerIndirectFloatField fieldY;
        /// <summary>Z float field.</summary>
        public DebugUIHandlerIndirectFloatField fieldZ;

        DebugUI.Vector3Field m_Field;
        DebugUIHandlerContainer m_Container;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            m_Field = CastWidget<DebugUI.Vector3Field>();
            m_Container = GetComponent<DebugUIHandlerContainer>();
            nameLabel.text = m_Field.displayName;

            fieldX.getter = () => m_Field.GetValue().x;
            fieldX.setter = v => SetValue(v, x: true);
            fieldX.nextUIHandler = fieldY;
            SetupSettings(fieldX);

            fieldY.getter = () => m_Field.GetValue().y;
            fieldY.setter = v => SetValue(v, y: true);
            fieldY.previousUIHandler = fieldX;
            fieldY.nextUIHandler = fieldZ;
            SetupSettings(fieldY);

            fieldZ.getter = () => m_Field.GetValue().z;
            fieldZ.setter = v => SetValue(v, z: true);
            fieldZ.previousUIHandler = fieldY;
            SetupSettings(fieldZ);
        }

        void SetValue(float v, bool x = false, bool y = false, bool z = false)
        {
            var vec = m_Field.GetValue();
            if (x) vec.x = v;
            if (y) vec.y = v;
            if (z) vec.z = v;
            m_Field.SetValue(vec);
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
        /// <returns>True if the selection is allowed.</returns>
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

        /// <summary>
        /// Next implementation.
        /// </summary>
        /// <returns>Next widget UI handler, parent if there is none.</returns>
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
