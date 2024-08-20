using UnityEditor.Experimental.GraphView;
using UnityEditor.VFX.Block;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace UnityEditor.VFX.UI
{
    class VFXBlackboardAttributeRow : BlackboardRow
    {
        private readonly VFXBlackboardAttributeField m_Field;
        private readonly VFXBlackboardAttributeView m_View;

        public VFXBlackboardAttributeRow(AttributeItem attribute) : this(attribute, new VFXBlackboardAttributeField(attribute), new VFXBlackboardAttributeView(attribute))
        {
        }

        private VFXBlackboardAttributeRow(AttributeItem attribute, VFXBlackboardAttributeField field, VFXBlackboardAttributeView attributeView) : base(field, attributeView)
        {
            this.attribute = attribute;
            this.m_Field = field;
            this.m_View = attributeView;
            this.Q<TemplateContainer>().pickingMode = PickingMode.Ignore;
            this.Q<Pill>().tooltip = this.attribute.description + $"\nType: {this.attribute.type}";

            if (attribute.isEditable)
            {
                this.Q<Button>("expandButton").RegisterCallback<ClickEvent>(this.OnToggleExpand);
            }
            else
            {
                m_Field.capabilities &= ~Capabilities.Renamable;
                var specialIcon = new VisualElement { name = "SpecialIcon", tooltip = attribute.subgraphUse == null ? "This is a built-in attribute, it cannot be deleted or edited" : "This attribute is imported by a subgraph, it cannot be deleted or edited" };
                Add(specialIcon);
            }

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
            this.expanded = this.attribute.isExpanded;
        }

        public VFXBlackboardAttributeField field => m_Field;
        public AttributeItem attribute { get; }


        private DropdownMenuAction.Status IsMenuVisible(DropdownMenuAction action) => this.attribute.isEditable ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;

        private void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target == this)
            {
                evt.menu.AppendAction("Rename", (a) => m_Field.OpenTextEditor(), IsMenuVisible);
                evt.menu.AppendAction("Duplicate %d", (a) => Duplicate());
                evt.menu.AppendAction("Delete", (a) => Delete(), IsMenuVisible);
                evt.menu.AppendAction("Copy Name", (a) => CopyName(), DropdownMenuAction.AlwaysEnabled);

                evt.StopPropagation();
            }
        }

        private void CopyName()
        {
            EditorGUIUtility.systemCopyBuffer = attribute.title;
        }

        private void Duplicate()
        {
            GetFirstAncestorOfType<VFXView>().DuplicateSelectionCallback();
        }

        private void Delete()
        {
            this.GetFirstAncestorOfType<VFXView>().Delete();
        }

        private void OnToggleExpand(ClickEvent evt)
        {
            attribute.isExpanded = this.expanded;
            this.GetFirstAncestorOfType<VFXView>().controller.graph.SetCustomAttributeExpanded(this.attribute.title, this.expanded);
        }

        public void Update(string newName, VFXValueType newType, string newDescription)
        {
            this.attribute.title = newName;
            this.attribute.type = CustomAttributeUtility.GetSignature(newType);
            this.attribute.description = newDescription;

            this.field.text = newName;
            this.field.UpdateType(attribute.type);
            this.m_View.Update(attribute.type, attribute.description);
        }
    }
}
