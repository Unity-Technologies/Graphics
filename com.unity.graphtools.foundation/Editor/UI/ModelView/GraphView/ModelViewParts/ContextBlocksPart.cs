using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The part to build the UI  for blocks containers.
    /// </summary>
    public class ContextBlocksPart : BaseModelViewPart
    {
        /// <summary>
        /// The <see cref="ContextNodeModel"/> displayed in this part.
        /// </summary>
        public IContextNodeModel ContextNodeModel => m_OwnerElement.Model as IContextNodeModel;

        /// <summary>
        /// The class name for the add block name element
        /// </summary>
        public string AddBlockNameClassName => m_ParentClassName.WithUssElement(addBlockName);

        /// <summary>
        /// The class name for the add block label element
        /// </summary>
        public string AddBlockLabelClassName => m_ParentClassName.WithUssElement(addBlockLabelName);

        /// <summary>
        /// The class name for the add block plus element
        /// </summary>
        public string AddBlockPlusClassName => m_ParentClassName.WithUssElement(addBlockPlusName);

        /// <summary>
        /// The class name for the etch
        /// </summary>
        public string EtchClassName => m_ParentClassName.WithUssElement(etchName);

        VisualElement m_Root;
        VisualElement m_Etch;
        VisualElement m_AddBlock;

        /// <inheritdoc/>
        public override VisualElement Root => m_Root;

        static readonly string etchName = "etch";
        static readonly string addBlockName = "add-block";
        static readonly string addBlockPlusName = "add-block-plus";
        static readonly string addBlockLabelName = "add-block-label";

        /// <summary>
        /// Creates a new <see cref="ContextBlocksPart"/>.
        /// </summary>
        /// <param name="name">The name of the part to create.</param>
        /// <param name="model">The model which the part represents.</param>
        /// <param name="ownerElement">The owner of the part to create.</param>
        /// <param name="parentClassName">The class name of the parent UI.</param>
        /// <returns>A new instance of <see cref="ContextBlocksPart"/>.</returns>
        public static ContextBlocksPart Create(string name, IModel model, IModelView ownerElement, string parentClassName)
        {
            if (model is IContextNodeModel contextModel)
            {
                return new ContextBlocksPart(name, contextModel, ownerElement, parentClassName);
            }

            return null;
        }

        /// <summary>
        /// Creates a new ContextBlocksPart.
        /// </summary>
        /// <param name="name">The name of the part to create.</param>
        /// <param name="nodeModel">The model which the part represents.</param>
        /// <param name="ownerElement">The owner of the part to create.</param>
        /// <param name="parentClassName">The class name of the parent UI.</param>
        /// <returns>A newly created <see cref="ContextBlocksPart"/>.</returns>
        protected ContextBlocksPart(string name, IContextNodeModel nodeModel, IModelView ownerElement, string parentClassName)
            : base(name, nodeModel, ownerElement, parentClassName) {}


        /// <inheritdoc/>
        protected override void BuildPartUI(VisualElement container)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));
            container.Add(m_Root);

            m_Etch = new VisualElement { name = etchName };

            m_Etch.AddToClassList(EtchClassName);
            m_Root.Add(m_Etch);

            m_AddBlock = new VisualElement();
            m_AddBlock.AddToClassList(AddBlockNameClassName);
            m_AddBlock.RegisterCallback<MouseDownEvent>(MouseDownOnAddBlock);

            var label = new Label { name = addBlockLabelName, text = "Add Block" };
            label.AddToClassList(AddBlockLabelClassName);
            var plus = new Label { name = addBlockPlusName, text = "+" };
            plus.AddToClassList(AddBlockPlusClassName);
            m_AddBlock.Add(plus);
            m_AddBlock.Add(label);

            m_Root.Insert(0, m_AddBlock);
        }

        void MouseDownOnAddBlock(MouseDownEvent e)
        {
            if (e.button == (int) MouseButton.LeftMouse)
            {
                ((ContextNode)m_OwnerElement).DisplaySmartSearch(e.mousePosition);
                e.StopPropagation();
            }
        }

        /// <inheritdoc/>
        protected override void UpdatePartFromModel()
        {
            Dictionary<int, IBlockNodeModel> blockModels = ContextNodeModel.GraphElementModels.Cast<IBlockNodeModel>().Select((t, i) => new { index = i, value = t }).ToDictionary(t => t.index, t => t.value);

            List<ModelView> blocks = Root.Children().OfType<ModelView>().ToList();
            // Delete blocks that are no longer in the model
            foreach (var block in blocks)
            {
                if (!(block.Model is IBlockNodeModel blockModel) || blockModels.ContainsValue(blockModel))
                    continue;
                block.RemoveFromRootView();
                block.RemoveFromHierarchy();
            }
            if (blockModels.Count == 0)
                return;

            var orderBlockModels = new List<KeyValuePair<int, IBlockNodeModel>>(blockModels.Count);
            var exisitingModels = new HashSet<IModel>(blocks.Select(u => u.Model));

            foreach (var blockModel in blockModels)
            {
                if (exisitingModels.Contains(blockModel.Value))
                    continue;
                orderBlockModels.Add(blockModel);
            }

            orderBlockModels.Sort((a, b) => Comparer<int>.Default.Compare(a.Key, b.Key));

            // Add blocks that are new in the model
            foreach (var blockModel in orderBlockModels)
            {
                int index = blockModel.Key + 1;
                ModelView newBlockNode = ModelViewFactory.CreateUI<ModelView>(m_OwnerElement.RootView, blockModel.Value);

                if (newBlockNode != null)
                {
                    newBlockNode.AddToRootView(m_OwnerElement.RootView);
                    if (index < Root.childCount)
                        Root.Insert(index, newBlockNode);
                    else
                        Root.Add(newBlockNode);
                }
            }

            // Sort blocks through the models order the idea is to minimize change since most of the time block order will still be valid
            // they are sorted as reverse order in the ui and then flex-direction: reverse-column is used so that the top blocks are closer than the bottom blocks
            blocks = Root.Children().OfType<ModelView>().ToList();
            ModelView firstBlockNode = blocks.LastOrDefault();
            IBlockNodeModel firstModel = blockModels[0];
            if (firstBlockNode == null || !ReferenceEquals(firstBlockNode.Model, firstModel))
            {
                firstBlockNode = blocks.Last(t => ReferenceEquals(t.Model, firstModel));
                firstBlockNode.PlaceBehind(m_Etch);
                blocks.Remove(firstBlockNode);
                blocks.Insert(blocks.Count - 1, firstBlockNode);
            }

            ModelView prevBlockNode = firstBlockNode;
            for (int i = 1; i < blockModels.Count; ++i)
            {
                ModelView currentBlockNode = blocks.First(t => ReferenceEquals(t.Model, blockModels[i]));
                if (blocks[blockModels.Count - 1 - i] != currentBlockNode)
                {
                    currentBlockNode.PlaceBehind(prevBlockNode);
                    blocks.Remove(currentBlockNode);
                    blocks.Insert(blockModels.Count - 1 - i, currentBlockNode);
                }

                prevBlockNode = currentBlockNode;
            }

            foreach (ModelView block in blocks)
                block.UpdateFromModel();
        }
    }
}
