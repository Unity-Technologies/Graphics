using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A <see cref="BlackboardVariableGroup"/> Part containing all the group's items.
    /// </summary>
    public class BlackboardVariableGroupItemsPart : BaseModelUIPart
    {
        /// <summary>
        /// Creates a new instance of the <see cref="BlackboardSectionListPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="BlackboardSectionListPart"/>.</returns>
        public static BlackboardVariableGroupItemsPart Create(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
        {
            if (model is IGroupModel)
            {
                return new BlackboardVariableGroupItemsPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        IGroupModel GroupModel => m_Model as IGroupModel;

        BlackboardVariableGroupItemsPart(string name, IGraphElementModel model, IModelUI ownerElement,
                                         string parentClassName) : base(name, model, ownerElement, parentClassName)
        {
        }

        VisualElement m_Root;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement();
            m_Root.AddToClassList(m_ParentClassName.WithUssElement("items"));
            parent.Add(m_Root);
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            var existingItems = Root.Children().OfType<GraphElement>().ToList();
            var existingItemModels = new HashSet<IGraphElementModel>(existingItems.Select(t => t.Model));

            foreach (var ui in existingItems)
            {
                if (GroupModel.Items.Contains(ui.Model))
                    continue;

                ui.RemoveFromView();
                ((VisualElement)ui).RemoveFromHierarchy();
            }

            foreach (var vgm in GroupModel.Items)
            {
                if (existingItemModels.Contains(vgm))
                    continue;

                var ui = GraphElementFactory.CreateUI<ModelUI>(m_OwnerElement.View, vgm);
                if (ui == null)
                    continue;

                ui.AddToView(m_OwnerElement.View);
                Root.Add(ui);
            }

            //Sort the ui in the same order as in the model.
            List<ModelUI> items = Root.Children().OfType<ModelUI>().ToList();

            if (items.Count == 0)
                return;

            List<IGroupItemModel> itemModels = GroupModel.Items.ToList();
            ModelUI firstItem = items.FirstOrDefault();
            IGroupItemModel firstModel = itemModels[0];
            if (firstItem == null || firstItem.Model != firstModel)
            {
                firstItem = items.First(t => t.Model == firstModel);
                Root.Insert(0, firstItem);
                items.Remove(firstItem);
                items.Insert(0, firstItem);
            }

            ModelUI prevItem = firstItem;
            for (int i = 1; i < itemModels.Count; ++i)
            {
                ModelUI currentItem = items.First(t => t.Model == itemModels[i]);
                if (items[i] != currentItem)
                {
                    currentItem.PlaceInFront(prevItem);
                    items.Remove(currentItem);
                    items.Insert(i, currentItem);
                }

                prevItem = currentItem;
            }

            foreach (var item in items)
                item.UpdateFromModel();
        }
    }
}
