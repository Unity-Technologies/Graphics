using System;
using UnityEngine.UI;

namespace UnityEngine.Experimental.Rendering.UI
{
    public class DebugUIHandlerIndirectToggle : DebugUIHandlerWidget
    {
        public Text nameLabel;
        public Toggle valueToggle;
        public Image checkmarkImage;

        public Func<int, bool> getter;
        public Action<int, bool> setter;
        public int index;

        public void Init()
        {
            UpdateValueLabel();
        }

        public override bool OnSelection(bool fromNext, DebugUIHandlerWidget previous)
        {
            nameLabel.color = colorSelected;
            checkmarkImage.color = colorSelected;
            return true;
        }

        public override void OnDeselection()
        {
            nameLabel.color = colorDefault;
            checkmarkImage.color = colorDefault;
        }

        public override void OnAction()
        {
            bool value = !getter(index);
            setter(index, value);
            UpdateValueLabel();
        }

        void UpdateValueLabel()
        {
            if (valueToggle != null)
                valueToggle.isOn = getter(index);
        }
    }
}
