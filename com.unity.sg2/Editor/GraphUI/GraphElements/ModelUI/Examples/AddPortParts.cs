using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    //public class AddPortPart : BaseModelViewPart
    //{
    //    public AddPortPart(string name, IGraphElementModel model, IModelView ownerElement, string parentClassName)
    //        : base(name, model, ownerElement, parentClassName) { }

    //    protected override void BuildPartUI(VisualElement parent)
    //    {
    //        m_Root = new Foldout { name = PartName, text = "Add Port", value = false };

    //        m_NameField = new TextField("Name");
    //        m_Root.Add(m_NameField);

    //        m_TypeDropdown = new DropdownField("Type", ShaderGraphExampleTypes.TypeHandleNames.ToList(), 0);
    //        m_TypeDropdown.RegisterValueChangedCallback(e =>
    //        {
    //            if (string.IsNullOrEmpty(m_NameField.value) || m_NameField.value == e.previousValue) m_NameField.value = e.newValue;
    //        });

    //        m_Root.Add(m_TypeDropdown);

    //        m_OutputToggle = new Toggle("Output");
    //        m_Root.Add(m_OutputToggle);

    //        m_AddButton = new Button(OnAddPressed) { text = "Add" };
    //        m_Root.Add(m_AddButton);

    //        parent.Add(m_Root);
    //    }

    //    void OnAddPressed()
    //    {
    //        m_OwnerElement.View.Dispatch(new AddPortCommand(
    //            m_OutputToggle.value,
    //            m_NameField.value,
    //            ShaderGraphExampleTypes.TypeHandlesByName[m_TypeDropdown.value],
    //            new [] { (CustomizableNodeModel) m_Model }));
    //    }

    //    protected override void UpdatePartFromModel() { }

    //    VisualElement m_Root;
    //    TextField m_NameField;
    //    DropdownField m_TypeDropdown;
    //    Toggle m_OutputToggle;
    //    Button m_AddButton;

    //    public override VisualElement Root => m_Root;
    //}
}
