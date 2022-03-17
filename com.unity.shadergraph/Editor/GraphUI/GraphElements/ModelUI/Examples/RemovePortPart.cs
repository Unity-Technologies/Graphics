using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    //public class RemovePortPart : BaseModelViewPart
    //{
    //    public RemovePortPart(string name, IGraphElementModel model, IModelView ownerElement, string parentClassName) : base(name, model, ownerElement, parentClassName)
    //    {
    //    }

    //    protected override void BuildPartUI(VisualElement parent)
    //    {
    //        m_Root = new Foldout { name = PartName, text = "Remove Port", value = false };

    //        m_OutputToggle = new Toggle("Output") { value = false };
    //        m_OutputToggle.RegisterValueChangedCallback(_ => UpdatePartFromModel());
    //        m_Root.Add(m_OutputToggle);

    //        m_PortSelector = new DropdownField("Port");
    //        m_Root.Add(m_PortSelector);

    //        m_DeleteButton = new Button(OnRemovePressed) { text = "Remove" };
    //        m_Root.Add(m_DeleteButton);

    //        parent.Add(m_Root);
    //    }

    //    void OnRemovePressed()
    //    {
    //        m_OwnerElement.View.Dispatch(new RemovePortCommand(
    //            m_OutputToggle.value,
    //            m_PortSelector.value,
    //            new [] { (CustomizableNodeModel) m_Model }));
    //    }

    //    protected override void UpdatePartFromModel()
    //    {
    //        if (m_Model is not CustomizableNodeModel model) return;

    //        var ports = m_OutputToggle.value ? model.GetOutputPorts() : model.GetInputPorts();
    //        m_PortSelector.choices = ports.Select(p => p.UniqueName).ToList();
    //        m_PortSelector.index = Mathf.Clamp(m_PortSelector.index, 0, m_PortSelector.choices.Count);
    //    }

    //    VisualElement m_Root;
    //    Toggle m_OutputToggle;
    //    DropdownField m_PortSelector;
    //    Button m_DeleteButton;

    //    public override VisualElement Root { get; }
    //}
}
