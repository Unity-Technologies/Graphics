using UnityEngine.UI;

namespace UnityEngine.Experimental.Rendering.UI
{
    public class DebugUIHandlerFoldout : DebugUIHandlerWidget
    {
        public Text nameLabel;
        public UIFoldout valueToggle;

        DebugUI.Foldout m_Field;
        DebugUIHandlerContainer m_Container;

        const float xDecal = 60f;
        const float xDecalInit = 230f;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            m_Field = CastWidget<DebugUI.Foldout>();
            m_Container = GetComponent<DebugUIHandlerContainer>();
            nameLabel.text = m_Field.displayName;

            int columnNumber = m_Field.columnLabels?.Length ?? 0;
            for (int index = 0; index < columnNumber; ++index)
            {
                var column = Instantiate(nameLabel.gameObject, GetComponent<DebugUIHandlerContainer>().contentHolder);
                column.AddComponent<LayoutElement>().ignoreLayout = true;
                var rectTransform = column.transform as RectTransform;
                var originalTransform = nameLabel.transform as RectTransform;
                rectTransform.anchorMax = rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.sizeDelta = new Vector2(100, 26);
                Vector3 pos = originalTransform.anchoredPosition;
                pos.x += (index + 1) * xDecal + xDecalInit;
                rectTransform.anchoredPosition = pos;
                rectTransform.pivot = new Vector2(0, 0.5f);
                rectTransform.eulerAngles = new Vector3(0, 0, 13);
                var text = column.GetComponent<Text>();
                text.fontSize = 15;
                text.text = m_Field.columnLabels[index];
            }
            
            UpdateValue();
        }

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

        public override void OnDeselection()
        {
            nameLabel.color = colorDefault;
        }

        public override void OnIncrement(bool fast)
        {
            m_Field.SetValue(true);
            UpdateValue();
        }

        public override void OnDecrement(bool fast)
        {
            m_Field.SetValue(false);
            UpdateValue();
        }

        public override void OnAction()
        {
            bool value = !m_Field.GetValue();
            m_Field.SetValue(value);
            UpdateValue();
        }

        void UpdateValue()
        {
            valueToggle.isOn = m_Field.GetValue();
        }

        public override DebugUIHandlerWidget Next()
        {
            if (!m_Field.GetValue() || m_Container == null)
                return base.Next();

            var firstChild = m_Container.GetFirstItem();

            if (firstChild == null)
                return base.Next();

            return firstChild;
        }
    }
}
