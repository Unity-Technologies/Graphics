using System;
using System.Collections.Generic;
using System.Linq;
using UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class BlockContainer : VisualContainer, IKeyFocusBlocker
    {
        // ISelection implementation
        public List<ISelectable> selection { get; private set; }


        public BlockContainer()
        {
            selection = new List<ISelectable>();
            clipChildren = false;
        }
    }

    class VFXContextUI : GraphElement, IDropTarget
    {
        // TODO: Unused except for debugging
        const string RectColorProperty = "rect-color";

        VisualContainer     m_Header;
        VisualContainer     m_HeaderContainer;
        VisualElement       m_HeaderIcon;
        VisualElement       m_HeaderTitle;

        VisualElement       m_HeaderSpace;

        VisualContainer     m_Footer;
        VisualElement       m_FooterIcon;
        VisualElement       m_FooterTitle;

        VisualContainer     m_FlowInputConnectorContainer;
        VisualContainer     m_FlowOutputConnectorContainer;
        VisualContainer     m_NodeContainer;
        BlockContainer      m_BlockContainer;

        VisualElement       m_DragDisplay;

        VFXContextSlotContainerUI  m_OwnData;

        protected GraphViewTypeFactory typeFactory { get; set; }

        public VFXContextSlotContainerUI ownData { get { return m_OwnData; }}

        public VFXContextUI()
        {
            forceNotififcationOnAdd = true;
            pickingMode = PickingMode.Ignore;

            m_FlowInputConnectorContainer = new VisualContainer()
            {
                name = "FlowInputs",
                pickingMode = PickingMode.Ignore,
            };
            m_FlowInputConnectorContainer.ClearClassList();
            m_FlowInputConnectorContainer.AddToClassList("FlowContainer");
            m_FlowInputConnectorContainer.AddToClassList("Input");

            m_FlowOutputConnectorContainer = new VisualContainer()
            {
                name = "FlowOutputs",
                pickingMode = PickingMode.Ignore
            };
            m_FlowOutputConnectorContainer.ClearClassList();
            m_FlowOutputConnectorContainer.AddToClassList("FlowContainer");
            m_FlowOutputConnectorContainer.AddToClassList("Output");

            m_NodeContainer = new VisualContainer()
            {
                name = "NodeContents",
                clipChildren = false
            };
            m_NodeContainer.clipChildren = false;

            AddChild(m_NodeContainer);

            m_Header = new VisualContainer() {
                name = "Header",
                clipChildren = false
            };
            m_HeaderContainer = new VisualContainer()
            {
                name = "HeaderContainer",
                clipChildren = false
            };
            m_HeaderContainer.AddToClassList("Extremity");
            m_HeaderTitle = new VisualElement() { name = "HeaderTitle" , text = "Title" };
            m_HeaderTitle.AddToClassList("title");
            m_HeaderIcon = new VisualElement() { name = "HeaderIcon"};
            m_HeaderIcon.AddToClassList("icon");
            m_HeaderContainer.AddChild(m_HeaderIcon);
            m_HeaderContainer.AddChild(m_HeaderTitle);

            m_Header.AddChild(m_FlowInputConnectorContainer);

            m_HeaderSpace = new VisualElement();
            m_HeaderSpace.name = "HeaderSpace";
            m_HeaderSpace.AddManipulator(new Clickable(OnSpace));

            m_HeaderContainer.AddChild(m_HeaderSpace);

            m_NodeContainer.AddChild(m_Header);


            m_OwnData = new VFXContextSlotContainerUI();
            m_Header.AddChild(m_HeaderContainer);
            m_Header.AddChild(m_OwnData);

            m_BlockContainer = new BlockContainer()
            {
                pickingMode = PickingMode.Ignore
            };

            m_BlockContainer.AddManipulator(new ClickSelector());

            m_NodeContainer.AddChild(m_BlockContainer);


            m_Footer = new VisualContainer() {
                name = "Footer",
                clipChildren = false
            };
            m_FooterTitle = new VisualElement() { name = "FooterTitle", text = "footer" };
            m_FooterTitle.AddToClassList("title");
            m_FooterIcon = new VisualElement() { name = "FooterIcon"};
            m_FooterIcon.AddToClassList("icon");
            m_Footer.AddChild(m_FooterIcon);
            m_Footer.AddChild(m_FooterTitle);
            m_Footer.AddToClassList("Extremity");

            m_Footer.AddChild(m_FlowOutputConnectorContainer);

            m_NodeContainer.AddChild(m_Footer);
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
            typeFactory = new GraphViewTypeFactory();
            typeFactory[typeof(VFXBlockPresenter)] = typeof(VFXBlockUI);
            typeFactory[typeof(VFXContextDataInputAnchorPresenter)] = typeof(VFXBlockDataAnchor);
            typeFactory[typeof(VFXContextDataOutputAnchorPresenter)] = typeof(VFXBlockDataAnchor);

            ClearClassList();
            AddToClassList("VFXContext");

            m_DragDisplay = new VisualElement();
            m_DragDisplay.AddToClassList("dragdisplay");
        }

        void OnSpace()
        {
            VFXContextPresenter presenter = GetPresenter<VFXContextPresenter>();
            int result = (int)presenter.context.space;

            presenter.context.space = (CoordinateSpace)(((int)presenter.context.space + 1) % (int)(CoordinateSpace.SpaceCount));
        }

        public bool CanDrop(IEnumerable<VFXBlockUI> blocks, VFXBlockUI target)
        {
            bool accept = true;
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

        public void DraggingBlocks(IEnumerable<VFXBlockUI> blocks, VFXBlockUI target, bool after)
        {
            DragFinished();
            if (!CanDrop(blocks, target))
            {
                return;
            }

            Vector2 position;
            if (target != null)
            {
                position = target.position.position;
                if (after)
                {
                    position.y += target.position.height;
                }
            }
            else
            {
                if (after)
                {
                    position.y = m_BlockContainer.position.height;
                }
                else
                {
                    position.y = 0;
                }
            }
            m_DragDisplay.positionTop = position.y;

            if (m_DragDisplay.parent == null)
            {
                m_BlockContainer.AddChild(m_DragDisplay);
            }
        }

        public void DragFinished()
        {
            if (m_DragDisplay.parent != null)
                m_BlockContainer.RemoveChild(m_DragDisplay);
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

            DraggingBlocks(blocksUI, null, true);
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
            IEnumerable<VFXBlockUI> blocksUI = selection.Select(t => t as VFXBlockUI).Where(t => t != null);
            if (!CanDrop(blocksUI, null))
                return EventPropagation.Stop;

            VFXContextPresenter presenter = GetPresenter<VFXContextPresenter>();

            foreach (var blockui in blocksUI)
            {
                VFXBlockPresenter blockPres = blockui.GetPresenter<VFXBlockPresenter>();
                presenter.AddBlock(-1, blockPres.block);
            }

            m_DragStarted = false;
            RemoveFromClassList("dropping");

            return EventPropagation.Stop;
        }

        EventPropagation IDropTarget.DragExited()
        {
            // TODO: Do something when current drag is canceled
            DragFinished();
            m_DragStarted = false;

            return EventPropagation.Stop;
        }

        public override void OnSelected()
        {
            this.SendToFront();
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
                positionType = PositionType.Absolute;
                positionLeft = newPos.x;
                positionTop = newPos.y;
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
            m_BlockContainer.AddChild(newElem);

            newElem.presenter.selected = blockPresenter.selected;
        }

        public void RefreshContext()
        {
            VFXContextPresenter contextPresenter = GetPresenter<VFXContextPresenter>();
            var blockPresenters = contextPresenter.blockPresenters;

            // recreate the children list based on the presenter list to keep the order.

            var blocksUIs = new Dictionary<VFXBlockPresenter, VFXBlockUI>();
            for (int i = 0; i < m_BlockContainer.childrenCount; ++i)
            {
                var child = m_BlockContainer.GetChildAt(i) as VFXBlockUI;
                if (child != null)
                    blocksUIs.Add(child.GetPresenter<VFXBlockPresenter>(), child);
            }

            foreach (var kv in blocksUIs)
            {
                m_BlockContainer.RemoveChild(kv.Value);
            }
            foreach (var blockPresenter in blockPresenters)
            {
                if (blockPresenter.block != null)
                {
                    VFXBlockUI blockUI;
                    if (blocksUIs.TryGetValue(blockPresenter, out blockUI))
                    {
                        m_BlockContainer.AddChild(blockUI);
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
                        GetPresenter<VFXContextPresenter>().AddBlock(-1, d.CreateInstance());
                    }));
                m_NodeContainer.AddManipulator(m_PopupManipulator);
            }


            // Recreate label with good name // Dirty
            if (presenter.context.inputType != VFXDataType.kNone)
                m_HeaderTitle.text = string.Format("{0} {1}", presenter.context.name, presenter.context.inputType.ToString().Substring(1));
            else
                m_HeaderTitle.text = presenter.context.name;
            m_HeaderIcon.backgroundImage = GetIconForVFXType(presenter.context.inputType);

            VFXContextType contextType = presenter.context.contextType;

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
                case VFXContextType.kInit: AddToClassList("init"); break;
                case VFXContextType.kUpdate: AddToClassList("update"); break;
                case VFXContextType.kOutput: AddToClassList("output"); break;
                default: throw new Exception();
            }


            if (presenter.context.outputType == VFXDataType.kNone)
            {
                if (m_Footer.parent != null)
                    m_NodeContainer.RemoveChild(m_Footer);
            }
            else
            {
                if (m_Footer.parent == null)
                    m_NodeContainer.AddChild(m_Footer);
                m_FooterTitle.text = presenter.context.outputType.ToString().Substring(1);
                m_FooterIcon.backgroundImage = GetIconForVFXType(presenter.context.outputType);
            }

            HashSet<VisualElement> newInAnchors = new HashSet<VisualElement>();

            foreach (var inanchorpresenter in presenter.inputAnchors)
            {
                var existing = m_FlowInputConnectorContainer.Select(t => t as VFXFlowAnchor).FirstOrDefault(t => t.presenter == inanchorpresenter);
                if (existing == null)
                {
                    var anchor = VFXFlowAnchor.Create<VFXFlowEdgePresenter>(inanchorpresenter);
                    m_FlowInputConnectorContainer.AddChild(anchor);
                    newInAnchors.Add(anchor);
                }
                else
                {
                    newInAnchors.Add(existing);
                }
            }

            foreach (var nonLongerExistingAnchor in m_FlowInputConnectorContainer.Where(t => !newInAnchors.Contains(t)).ToList()) // ToList to make a copy because the enumerable will change when we delete
            {
                m_FlowInputConnectorContainer.RemoveChild(nonLongerExistingAnchor);
            }


            HashSet<VisualElement> newOutAnchors = new HashSet<VisualElement>();

            foreach (var outanchorpresenter in presenter.outputAnchors)
            {
                var existing = m_FlowOutputConnectorContainer.Select(t => t as VFXFlowAnchor).FirstOrDefault(t => t.presenter == outanchorpresenter);
                if (existing == null)
                {
                    var anchor = VFXFlowAnchor.Create<VFXFlowEdgePresenter>(outanchorpresenter);
                    m_FlowOutputConnectorContainer.AddChild(anchor);
                    newOutAnchors.Add(anchor);
                }
                else
                {
                    newOutAnchors.Add(existing);
                }
            }

            foreach (var nonLongerExistingAnchor in m_FlowOutputConnectorContainer.Where(t => !newOutAnchors.Contains(t)).ToList()) // ToList to make a copy because the enumerable will change when we delete
            {
                m_FlowOutputConnectorContainer.RemoveChild(nonLongerExistingAnchor);
            }


            RefreshContext();


            m_OwnData.presenter = presenter;

            bool slotsVisible = presenter.inputAnchors.Count() > 0;
            if (slotsVisible && m_OwnData.parent == null)
            {
                m_Header.AddChild(m_OwnData);
            }
            else if (!slotsVisible && m_OwnData.parent != null)
            {
                m_Header.RemoveChild(m_OwnData);
            }
        }

        FilterPopup m_PopupManipulator;

        internal override void DoRepaint(IStylePainter painter)
        {
            base.DoRepaint(painter);
        }

        // TODO: Remove, unused except for debugging
        // Declare new USS rect-color and use it
        public override void OnStylesResolved(ICustomStyles styles)
        {
            base.OnStylesResolved(styles);
            styles.ApplyCustomProperty(RectColorProperty, ref m_RectColor);
        }

        // TODO: Remove, unused except for debugging
        Style<Color> m_RectColor;
        Color rectColor { get { return m_RectColor.GetSpecifiedValueOrDefault(Color.magenta); } }

        public IEnumerable<VFXBlockUI> GetAllBlocks()
        {
            foreach (VFXBlockUI block in m_BlockContainer.OfType<VFXBlockUI>())
            {
                yield return block;
            }
        }

        public IEnumerable<NodeAnchor> GetAllAnchors(bool input, bool output)
        {
            return (IEnumerable<NodeAnchor>)GetFlowAnchors(input, output);
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
