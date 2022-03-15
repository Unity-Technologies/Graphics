using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A <see cref="BlackboardGroup"/> Part containing all the group's items.
    /// </summary>
    public class BlackboardGroupItemsPart : BaseModelViewPart
    {
        /// <summary>
        /// Creates a new instance of the <see cref="BlackboardSectionListPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="BlackboardSectionListPart"/>.</returns>
        public static BlackboardGroupItemsPart Create(string name, IGraphElementModel model, IModelView ownerElement, string parentClassName)
        {
            if (model is IGroupModel)
            {
                return new BlackboardGroupItemsPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        IGroupModel GroupModel => m_Model as IGroupModel;

        BlackboardGroupItemsPart(string name, IGraphElementModel model, IModelView ownerElement,
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
            var existingItems = Root.Children().OfType<BlackboardElement>().ToList();
            var existingItemModels = new HashSet<IModel>(existingItems.Select(t => t.Model));

            foreach (var ui in existingItems)
            {
                if (GroupModel.Items.Contains(ui.Model))
                    continue;

                ui.RemoveFromRootView();
                ui.RemoveFromHierarchy();
            }

            foreach (var vgm in GroupModel.Items)
            {
                if (existingItemModels.Contains(vgm))
                    continue;

                var ui = ModelViewFactory.CreateUI<ModelView>(m_OwnerElement.RootView, vgm);
                if (ui == null)
                    continue;

                ui.AddToRootView(m_OwnerElement.RootView);
                Root.Add(ui);
            }

            //Sort the ui in the same order as in the model.
            List<ModelView> items = Root.Children().OfType<ModelView>().ToList();

            if (items.Count == 0)
                return;

            List<IGroupItemModel> itemModels = GroupModel.Items.ToList();
            ModelView firstItem = items.FirstOrDefault();
            IGroupItemModel firstModel = itemModels[0];
            if (firstItem == null || !ReferenceEquals(firstItem.Model, firstModel))
            {
                firstItem = items.First(t => ReferenceEquals(t.Model, firstModel));
                Root.Insert(0, firstItem);
                items.Remove(firstItem);
                items.Insert(0, firstItem);
            }

            ModelView prevItem = firstItem;
            for (int i = 1; i < itemModels.Count; ++i)
            {
                ModelView currentItem = items.First(t => ReferenceEquals(t.Model, itemModels[i]));
                if (items[i] != currentItem)
                {
                    currentItem.PlaceInFront(prevItem);
                    items.Remove(currentItem);
                    items.Insert(i, currentItem);
                }

                prevItem = currentItem;
            }
        }
    }
}
