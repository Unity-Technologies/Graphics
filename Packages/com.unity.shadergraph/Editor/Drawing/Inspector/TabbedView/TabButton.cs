using System;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class TabButton : VisualElement
{
    [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
    internal new class UxmlFactory : UxmlFactory<TabButton, UxmlTraits> { }

    [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
    internal new class UxmlTraits : VisualElement.UxmlTraits
    {
        private readonly UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };
        private readonly UxmlStringAttributeDescription m_Target = new UxmlStringAttributeDescription { name = "target" };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            TabButton item = ve as TabButton;

            item.m_Label.text = m_Text.GetValueFromBag(bag, cc);
            item.TargetId = m_Target.GetValueFromBag(bag, cc);
        }
    }

    static readonly string styleName = "TabButtonStyles";
    static readonly string UxmlName = "TabButton";
    static readonly string s_UssClassName = "unity-tab-button";
    static readonly string s_UssActiveClassName = s_UssClassName + "--active";

    private Label m_Label;

    public bool IsCloseable { get; set; }

    [UxmlAttribute("target")]
    public string TargetId { get; private set; }
    public VisualElement Target { get; set; }

    [UxmlAttribute]
    internal string text
    { 
        get => m_Label.text;
        set => m_Label.text = value;
    }

    public event Action<TabButton> OnSelect;
    public event Action<TabButton> OnClose;

    public TabButton()
    {
        Init();
    }

    public TabButton(string text, VisualElement target)
    {
        Init();
        m_Label.text = text;
        Target = target;
    }

    private void PopulateContextMenu(ContextualMenuPopulateEvent populateEvent)
    {
        DropdownMenu dropdownMenu = populateEvent.menu;

        if (IsCloseable)
        {
            dropdownMenu.AppendAction("Close Tab", e => OnClose(this));
        }
    }

    private void CreateContextMenu(VisualElement visualElement)
    {
        ContextualMenuManipulator menuManipulator = new ContextualMenuManipulator(PopulateContextMenu);

        visualElement.focusable = true;
        visualElement.pickingMode = PickingMode.Position;
        visualElement.AddManipulator(menuManipulator);

        visualElement.AddManipulator(menuManipulator);
    }

    private void Init()
    {
        AddToClassList(s_UssClassName);
        styleSheets.Add(Resources.Load<StyleSheet>($"Styles/{styleName}"));

        VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>($"UXML/{UxmlName}");
        visualTree.CloneTree(this);

        m_Label = this.Q<Label>("Label");

        CreateContextMenu(this);

        RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
    }

    public void Select()
    {
        AddToClassList(s_UssActiveClassName);

        if (Target != null)
        {
            Target.style.display = DisplayStyle.Flex;
            Target.style.flexGrow = 1;
        }
    }

    public void Deselect()
    {
        RemoveFromClassList(s_UssActiveClassName);
        MarkDirtyRepaint();

        if (Target != null)
        {
            Target.style.display = DisplayStyle.None;
            Target.style.flexGrow = 0;
        }
    }

    private void OnMouseDownEvent(MouseDownEvent e)
    {
        switch (e.button)
        {
            case 0:
            {
                OnSelect?.Invoke(this);
                break;
            }

            case 2 when IsCloseable:
            {
                OnClose?.Invoke(this);
                break;
            }
        }        // End of switch.

        e.StopImmediatePropagation();
    }
}
