using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class BlockContainer : VisualElement
    {
        // ISelection implementation
        public List<ISelectable> selection { get; private set; }


        public BlockContainer()
        {
            selection = new List<ISelectable>();
        }
    }

    class VFXContextUI : GraphElement, IDropTarget, IEdgeDrawerContainer
    {
        // TODO: Unused except for debugging
        const string RectColorProperty = "rect-color";

        VisualElement     m_Header;
        VisualElement     m_HeaderContainer;
        VisualElement       m_HeaderIcon;
        VisualElement       m_HeaderTitle;

        VisualElement       m_HeaderSpace;

        VisualElement     m_Footer;
        VisualElement       m_FooterIcon;
        VisualElement       m_FooterTitle;

        VisualElement     m_FlowInputConnectorContainer;
        VisualElement     m_FlowOutputConnectorContainer;
        VisualElement     m_NodeContainer;
        BlockContainer      m_BlockContainer;
        VisualElement     m_InsideContainer;

        VisualElement       m_DragDisplay;

        VFXContextSlotContainerUI  m_OwnData;

        EdgeDrawer m_EdgeDrawer;

        protected GraphViewTypeFactory typeFactory { get; set; }

        public VFXContextSlotContainerUI ownData { get { return m_OwnData; }}

        public VFXContextUI()
        {
            forceNotififcationOnAdd = true;
            pickingMode = PickingMode.Ignore;

            m_FlowInputConnectorContainer = new VisualElement()
            {
                name = "FlowInputs",
                pickingMode = PickingMode.Ignore,
            };
            m_FlowInputConnectorContainer.ClearClassList();
            m_FlowInputConnectorContainer.AddToClassList("FlowContainer");
            m_FlowInputConnectorContainer.AddToClassList("Input");

            m_FlowOutputConnectorContainer = new VisualElement()
            {
                name = "FlowOutputs",
                pickingMode = PickingMode.Ignore
            };
            m_FlowOutputConnectorContainer.ClearClassList();
            m_FlowOutputConnectorContainer.AddToClassList("FlowContainer");
            m_FlowOutputConnectorContainer.AddToClassList("Output");

            m_NodeContainer = new VisualElement()
            {
                name = "NodeContents"
            };

            m_InsideContainer = new VisualElement()
            {
                name = "Inside"
            };
            m_InsideContainer.clippingOptions = ClippingOptions.ClipAndCacheContents;

            shadow.Add(m_NodeContainer);

            m_Header = new VisualElement() {
                name = "Header"
            };
            m_HeaderContainer = new VisualElement()
            {
                name = "HeaderContainer"
            };
            m_HeaderContainer.AddToClassList("Extremity");
            m_HeaderTitle = new VisualElement() { name = "HeaderTitle" , text = "Title" };
            m_HeaderTitle.AddToClassList("title");
            m_HeaderIcon = new VisualElement() { name = "HeaderIcon"};
            m_HeaderIcon.AddToClassList("icon");
            m_HeaderContainer.Add(m_HeaderIcon);
            m_HeaderContainer.Add(m_HeaderTitle);

            Add(m_FlowInputConnectorContainer);

            m_HeaderSpace = new VisualElement();
            m_HeaderSpace.name = "HeaderSpace";
            m_HeaderSpace.AddManipulator(new Clickable(OnSpace));

            m_HeaderContainer.Add(m_HeaderSpace);

            m_InsideContainer.Add(m_Header);


            m_OwnData = new VFXContextSlotContainerUI();
            m_OwnData.RemoveFromClassList("node");
            m_Header.Add(m_HeaderContainer);
            m_Header.Add(m_OwnData);

            m_BlockContainer = new BlockContainer()
            {
                pickingMode = PickingMode.Ignore
            };

            m_BlockContainer.AddManipulator(new ClickSelector());

            m_InsideContainer.Add(m_BlockContainer);


            m_Footer = new VisualElement() {
                name = "Footer"
            };
            m_FooterTitle = new VisualElement() { name = "FooterTitle", text = "footer" };
            m_FooterTitle.AddToClassList("title");
            m_FooterIcon = new VisualElement() { name = "FooterIcon"};
            m_FooterIcon.AddToClassList("icon");
            m_Footer.Add(m_FooterIcon);
            m_Footer.Add(m_FooterTitle);
            m_Footer.AddToClassList("Extremity");

            Add(m_FlowOutputConnectorContainer);

            m_InsideContainer.Add(m_Footer);
            /*
            m_NodeContainer.AddManipulator(new ContextualMenu((evt, customData) =>
            {
                var menu = new GenericMenu();

                // Needs to have the model here to filter compatible node blocks
                var contextType = GetPresenter<VFXContextPresenter>().Model.ContextType;
                foreach (var desc in VFXLibrary.GetBlocks())
                    if ((desc.CompatibleContexts & contextType) != 0)
                        menu.AddItem(new GUIContent(desc.Name), false,
                                     contentView => AddBlock(-1, desc),
                                     this);

                menu.ShowAsContext();
                return EventPropagation.Continue;
            }));
            */

            m_NodeContainer.Add(m_InsideContainer);
            typeFactory = new GraphViewTypeFactory();
            typeFactory[typeof(VFXBlockPresenter)] = typeof(VFXBlockUI);
            typeFactory[typeof(VFXContextDataInputAnchorPresenter)] = typeof(VFXBlockDataAnchor);
            typeFactory[typeof(VFXContextDataOutputAnchorPresenter)] = typeof(VFXBlockDataAnchor);

            ClearClassList();
            AddToClassList("VFXContext");

            m_DragDisplay = new VisualElement();
            m_DragDisplay.AddToClassList("dragdisplay");

            Add(new VisualElement() { name = "icon" });

            m_EdgeDrawer = new VFXContextEdgeDrawer();

            m_EdgeDrawer.style.positionType = PositionType.Absolute;
            m_EdgeDrawer.style.positionLeft = 0;
            m_EdgeDrawer.style.positionRight = 0;
            m_EdgeDrawer.style.positionBottom = 0;
            m_EdgeDrawer.style.positionTop = 0;
            m_InsideContainer.Add(m_EdgeDrawer);
            m_EdgeDrawer.element = this;

            clippingOptions = VisualElement.ClippingOptions.NoClipping;
        }

        void IEdgeDrawerContainer.EdgeDirty()
        {
            m_EdgeDrawer.Dirty(ChangeType.Repaint);
        }

        void OnSpace()
        {
            var presenter = GetPresenter<VFXContextPresenter>();
            presenter.context.space = (CoordinateSpace)(((int)presenter.context.space + 1) % (CoordinateSpaceInfo.SpaceCount));
        }

        public bool CanDrop(IEnumerable<VFXBlockUI> blocks, VFXBlockUI target)
        {
            bool accept = true;
            if (blocks.Count() == 0) return false;
            foreach (var block in blocks)
            {
                if (block == target)
                {
                    accept = false;
                    break;
                }
                if (!GetPresenter<VFXContextPresenter>().model.AcceptChild(block.GetPresenter<VFXBlockPresenter>().block))
                {
                    accept = false;
                    break;
                }
            }
            return accept;
        }

        public override bool HitTest(Vector2 localPoint)
        {
            // needed so that if we click on a block we won't select the context as well.
            if (m_BlockContainer.ContainsPoint(this.ChangeCoordinatesTo(m_BlockContainer, localPoint)))
            {
                return false;
            }
            return ContainsPoint(localPoint);
        }

        public void DraggingBlocks(IEnumerable<VFXBlockUI> blocks, VFXBlockUI target, bool after)
        {
            m_DragDisplay.RemoveFromHierarchy();

            if (!CanDrop(blocks, target))
            {
                return;
            }

            float y = 0;
            if (target != null)
            {
                y = target.layout.position.y;
                if (after)
                {
                    y += target.layout.height;
                }
            }
            else
            {
                if (after)
                {
                    y = m_BlockContainer.layout.height - 2;
                }
                else
                {
                    y = 0;
                }
            }
            Debug.Log("m_DragDisplay.y" + y);
            m_DragDisplay.style.positionTop = y;

            m_BlockContainer.Add(m_DragDisplay);
        }

        public void DragFinished()
        {
            if (m_DragDisplay.parent != null)
                m_BlockContainer.Remove(m_DragDisplay);
        }

        bool m_DragStarted;


        public bool CanAcceptDrop(List<ISelectable> selection)
        {
            IEnumerable<VFXBlockUI> blocksUI = selection.Select(t => t as VFXBlockUI).Where(t => t != null);

            return CanDrop(blocksUI, null);
        }

        EventPropagation IDropTarget.DragUpdated(IMGUIEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            IEnumerable<VFXBlockUI> blocksUI = selection.Select(t => t as VFXBlockUI).Where(t => t != null);

            Vector2 mousePosition = evt.imguiEvent.mousePosition;

            bool after = mousePosition.y > layout.height * 0.5f;

            DraggingBlocks(blocksUI, null, after);
            if (!m_DragStarted)
            {
                // TODO: Do something on first DragUpdated event (initiate drag)
                m_DragStarted = true;
                AddToClassList("dropping");
            }
            else
            {
                // TODO: Do something on subsequent DragUpdated events
            }

            return EventPropagation.Stop;
        }

        EventPropagation IDropTarget.DragPerform(IMGUIEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            DragFinished();

            Vector2 mousePosition = evt.imguiEvent.mousePosition;

            bool after = mousePosition.y > layout.height * 0.5f;

            IEnumerable<VFXBlockUI> blocksUI = selection.OfType<VFXBlockUI>();
            if (!CanDrop(blocksUI, null))
                return EventPropagation.Stop;

            VFXContextPresenter presenter = GetPresenter<VFXContextPresenter>();

            BlocksDropped(null, after, blocksUI, evt.imguiEvent.control);

            DragAndDrop.AcceptDrag();

            m_DragStarted = false;
            RemoveFromClassList("dropping");

            return EventPropagation.Stop;
        }

        public void BlocksDropped(VFXBlockPresenter blockPresenter, bool after, IEnumerable<VFXBlockUI> draggedBlocks, bool copy)
        {
            VFXContextPresenter presenter = GetPresenter<VFXContextPresenter>();

            HashSet<VFXContextUI> contexts = new HashSet<VFXContextUI>();
            foreach (var draggedBlock in draggedBlocks)
            {
                contexts.Add(draggedBlock.context);
            }

            using (var growContext = new GrowContext(this))
            {
                presenter.BlocksDropped(blockPresenter, after, draggedBlocks.Select(t => t.GetPresenter<VFXBlockPresenter>()), copy);

                foreach (var context in contexts)
                {
                    context.OnDataChanged();
                }
            }
        }

        EventPropagation IDropTarget.DragExited()
        {
            // TODO: Do something when current drag is canceled
            DragFinished();
            m_DragStarted = false;

            return EventPropagation.Stop;
        }

        public EventPropagation DeleteSelection()
        {
            var elementsToRemove = m_BlockContainer.selection.OfType<VFXBlockUI>().ToList();
            foreach (var block in elementsToRemove)
            {
                RemoveBlock(block as VFXBlockUI);
            }

            return (elementsToRemove.Count > 0) ? EventPropagation.Stop : EventPropagation.Continue;
        }

        public override void SetPosition(Rect newPos)
        {
            //if (classList.Contains("vertical"))
            /*{
                base.SetPosition(newPos);
            }
            else*/
            {
                style.positionType = PositionType.Absolute;
                style.positionLeft = newPos.x;
                style.positionTop = newPos.y;
            }
        }

        public void RemoveBlock(VFXBlockUI block)
        {
            if (block == null)
                return;

            VFXContextPresenter contextPresenter = GetPresenter<VFXContextPresenter>();
            contextPresenter.RemoveBlock(block.GetPresenter<VFXBlockPresenter>().block);
        }

        private void InstantiateBlock(VFXBlockPresenter blockPresenter)
        {
            // call factory
            GraphElement newElem = typeFactory.Create(blockPresenter);

            if (newElem == null)
            {
                return;
            }

            newElem.SetPosition(blockPresenter.position);
            newElem.presenter = blockPresenter;
            m_BlockContainer.Add(newElem);

            newElem.presenter.selected = blockPresenter.selected;
        }

        public void RefreshContext()
        {
            VFXContextPresenter contextPresenter = GetPresenter<VFXContextPresenter>();
            var blockPresenters = contextPresenter.blockPresenters;

            // recreate the children list based on the presenter list to keep the order.

            var blocksUIs = new Dictionary<VFXBlockPresenter, VFXBlockUI>();
            for (int i = 0; i < m_BlockContainer.childCount; ++i)
            {
                var child = m_BlockContainer.ElementAt(i) as VFXBlockUI;
                if (child != null)
                    blocksUIs.Add(child.GetPresenter<VFXBlockPresenter>(), child);
            }

            foreach (var kv in blocksUIs)
            {
                m_BlockContainer.Remove(kv.Value);
            }
            foreach (var blockPresenter in blockPresenters)
            {
                if (blockPresenter.block != null)
                {
                    VFXBlockUI blockUI;
                    if (blocksUIs.TryGetValue(blockPresenter, out blockUI))
                    {
                        m_BlockContainer.Add(blockUI);
                    }
                    else
                    {
                        InstantiateBlock(blockPresenter);
                    }
                }
            }


            // Does not guarantee correct ordering
            /*var blocks = m_BlockContainer.children.OfType<VFXBlockUI>().ToList();

            // Process removals
            foreach (var c in blocks)
            {
                // been removed?
                var nb = c as VFXBlockUI;
                var block = contextPresenter.blockPresenters.OfType<VFXBlockPresenter>().FirstOrDefault(a => a == nb.GetPresenter<VFXBlockPresenter>());
                if (block == null)
                {
                    m_BlockContainer.RemoveFromSelection(nb);
                    m_BlockContainer.RemoveChild(nb);
                }
            }

            // Process additions
            foreach (var blockPresenter in contextPresenter.blockPresenters)
            {
                var block = blocks.OfType<VFXBlockUI>().FirstOrDefault(a => a.GetPresenter<VFXBlockPresenter>() == blockPresenter);
                if (block == null)
                {
                    InstantiateBlock(blockPresenter);
                }
            }*/
        }

        Texture2D GetIconForVFXType(VFXDataType type)
        {
            switch (type)
            {
                case VFXDataType.kNone:
                    return Resources.Load<Texture2D>("VFX/Execution");
                case VFXDataType.kParticle:
                    return Resources.Load<Texture2D>("VFX/Particles");
            }
            return null;
        }

        class GrowContext : IDisposable
        {
            VFXContextUI m_Context;
            Dictionary<VFXContextUI, float> m_PrevSizes = new Dictionary<VFXContextUI, float>();
            float m_PrevSize;
            public GrowContext(VFXContextUI context)
            {
                m_Context = context;
                m_PrevSize = context.layout.size.y;
            }

            void IDisposable.Dispose()
            {
                m_Context.OnDataChanged();

                (m_Context.panel as BaseVisualElementPanel).ValidateLayout();

                m_Context.GetFirstAncestorOfType<VFXView>().PushUnderContext(m_Context, m_Context.layout.size.y - m_PrevSize);
            }
        }

        void AddBlock(Vector2 position, VFXModelDescriptor<VFXBlock> descriptor)
        {
            int blockIndex = -1;

            var blocks = m_BlockContainer.Query().OfType<VFXBlockUI>().ToList();
            for (int i = 0; i < blocks.Count; ++i)
            {
                if (blocks[i].worldBound.Contains(position))
                {
                    blockIndex = i;
                    break;
                }
            }

            using (var growContext = new GrowContext(this))
            {
                GetPresenter<VFXContextPresenter>().AddBlock(blockIndex, descriptor.CreateInstance());
            }
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            VFXContextPresenter presenter = GetPresenter<VFXContextPresenter>();
            if (presenter == null || presenter.context == null)
                return;

            if (m_PopupManipulator == null)
            {
                m_PopupManipulator = new FilterPopup(new VFXBlockProvider(presenter, (d, mPos) =>
                    {
                        AddBlock(mPos, d);
                    }));
                m_NodeContainer.AddManipulator(m_PopupManipulator);
            }


            // Recreate label with good name // Dirty
            if (presenter.context.inputType != VFXDataType.kNone)
                m_HeaderTitle.text = string.Format("{0} {1}", presenter.context.name, presenter.context.inputType.ToString().Substring(1));
            else
                m_HeaderTitle.text = presenter.context.name;
            m_HeaderIcon.style.backgroundImage = GetIconForVFXType(presenter.context.inputType);

            VFXContextType contextType = presenter.context.contextType;

            RemoveFromClassList("spawner");
            RemoveFromClassList("init");
            RemoveFromClassList("update");
            RemoveFromClassList("output");


            foreach (int val in System.Enum.GetValues(typeof(CoordinateSpace)))
            {
                m_HeaderSpace.RemoveFromClassList("space" + ((CoordinateSpace)val).ToString());
            }
            m_HeaderSpace.AddToClassList("space" + (presenter.context.space).ToString());

            switch (contextType)
            {
                case VFXContextType.kSpawnerGPU:
                case VFXContextType.kSpawner: AddToClassList("spawner"); break;
                case VFXContextType.kInit:    AddToClassList("init"); break;
                case VFXContextType.kUpdate:  AddToClassList("update"); break;
                case VFXContextType.kOutput:  AddToClassList("output"); break;
                case VFXContextType.kEvent:   AddToClassList("event"); break;
                default: throw new Exception();
            }


            if (presenter.context.outputType == VFXDataType.kNone)
            {
                if (m_Footer.parent != null)
                    m_InsideContainer.Remove(m_Footer);
            }
            else
            {
                if (m_Footer.parent == null)
                    m_InsideContainer.Add(m_Footer);
                m_FooterTitle.text = presenter.context.outputType.ToString().Substring(1);
                m_FooterIcon.style.backgroundImage = GetIconForVFXType(presenter.context.outputType);
            }

            HashSet<VisualElement> newInAnchors = new HashSet<VisualElement>();

            foreach (var inanchorpresenter in presenter.flowInputAnchors)
            {
                var existing = m_FlowInputConnectorContainer.Select(t => t as VFXFlowAnchor).FirstOrDefault(t => t.presenter == inanchorpresenter);
                if (existing == null)
                {
                    var anchor = VFXFlowAnchor.Create(inanchorpresenter);
                    m_FlowInputConnectorContainer.Add(anchor);
                    newInAnchors.Add(anchor);
                }
                else
                {
                    newInAnchors.Add(existing);
                }
            }

            foreach (var nonLongerExistingAnchor in m_FlowInputConnectorContainer.Where(t => !newInAnchors.Contains(t)).ToList()) // ToList to make a copy because the enumerable will change when we delete
            {
                m_FlowInputConnectorContainer.Remove(nonLongerExistingAnchor);
            }


            HashSet<VisualElement> newOutAnchors = new HashSet<VisualElement>();

            foreach (var outanchorpresenter in presenter.flowOutputAnchors)
            {
                var existing = m_FlowOutputConnectorContainer.Select(t => t as VFXFlowAnchor).FirstOrDefault(t => t.presenter == outanchorpresenter);
                if (existing == null)
                {
                    var anchor = VFXFlowAnchor.Create(outanchorpresenter);
                    m_FlowOutputConnectorContainer.Add(anchor);
                    newOutAnchors.Add(anchor);
                }
                else
                {
                    newOutAnchors.Add(existing);
                }
            }

            foreach (var nonLongerExistingAnchor in m_FlowOutputConnectorContainer.Where(t => !newOutAnchors.Contains(t)).ToList()) // ToList to make a copy because the enumerable will change when we delete
            {
                m_FlowOutputConnectorContainer.Remove(nonLongerExistingAnchor);
            }


            RefreshContext();


            m_OwnData.presenter = presenter.slotPresenter;

            bool slotsVisible = presenter.slotPresenter.inputPorts.Count() > 0 || (presenter.slotPresenter.settings != null && presenter.slotPresenter.settings.Count() > 0);
            if (slotsVisible && m_OwnData.parent == null)
            {
                m_Header.Add(m_OwnData);
            }
            else if (!slotsVisible && m_OwnData.parent != null)
            {
                m_Header.Remove(m_OwnData);
            }
        }

        FilterPopup m_PopupManipulator;

        internal override void DoRepaint(IStylePainter painter)
        {
            base.DoRepaint(painter);
        }

        // TODO: Remove, unused except for debugging
        // Declare new USS rect-color and use it
        protected override void OnStyleResolved(ICustomStyle styles)
        {
            base.OnStyleResolved(styles);
            styles.ApplyCustomProperty(RectColorProperty, ref m_RectColor);
        }

        // TODO: Remove, unused except for debugging
        StyleValue<Color> m_RectColor;
        Color rectColor { get { return m_RectColor.GetSpecifiedValueOrDefault(Color.magenta); } }

        public IEnumerable<VFXBlockUI> GetAllBlocks()
        {
            foreach (VFXBlockUI block in m_BlockContainer.OfType<VFXBlockUI>())
            {
                yield return block;
            }
        }

        public IEnumerable<Port> GetAllAnchors(bool input, bool output)
        {
            return (IEnumerable<Port>)GetFlowAnchors(input, output);
        }

        public IEnumerable<VFXFlowAnchor> GetFlowAnchors(bool input, bool output)
        {
            if (input)
                foreach (VFXFlowAnchor anchor in m_FlowInputConnectorContainer)
                {
                    yield return anchor;
                }
            if (output)
                foreach (VFXFlowAnchor anchor in m_FlowOutputConnectorContainer)
                {
                    yield return anchor;
                }
        }
    }
}
