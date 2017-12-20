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

    class VFXContextUI : GraphElement, IControlledElement<VFXContextController>, IControlledElement<VFXNodeController>, IDropTarget
    {
        // TODO: Unused except for debugging
        const string RectColorProperty = "rect-color";

        VisualElement               m_Header;
        VisualElement               m_HeaderContainer;
        VisualElement               m_HeaderIcon;
        Label                       m_HeaderTitle;

        VisualElement               m_HeaderSpace;

        VisualElement               m_Footer;
        VisualElement               m_FooterIcon;
        Label                       m_FooterTitle;

        VisualElement               m_FlowInputConnectorContainer;
        VisualElement               m_FlowOutputConnectorContainer;
        VisualElement               m_NodeContainer;
        BlockContainer              m_BlockContainer;
        VisualElement               m_InsideContainer;

        VisualElement               m_DragDisplay;

        VFXContextSlotContainerUI   m_OwnData;

        protected GraphViewTypeFactory typeFactory { get; set; }

        public VFXContextSlotContainerUI ownData { get { return m_OwnData; }}


        VFXContextController m_Controller;


        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }

        VFXNodeController IControlledElement<VFXNodeController>.controller
        {
            get { return m_Controller; }
            set
            {
                controller = value as VFXContextController;
            }
        }

        public VFXContextController controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != null)
                {
                    m_Controller.UnregisterHandler(this);
                }
                m_Controller = value;
                if (m_Controller != null)
                {
                    m_Controller.RegisterHandler(this);

                    m_OwnData.controller = m_Controller.slotContainerController;

                    bool slotsVisible = m_Controller.slotContainerController.inputPorts.Count() > 0 || (m_Controller.slotContainerController.settings != null && m_Controller.slotContainerController.settings.Count() > 0);
                    if (slotsVisible && m_OwnData.parent == null)
                    {
                        m_Header.Add(m_OwnData);
                    }
                    else if (!slotsVisible && m_OwnData.parent != null)
                    {
                        m_Header.Remove(m_OwnData);
                    }
                }
            }
        }

        void OnChange(ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                style.positionType = PositionType.Absolute;
                style.positionLeft = controller.position.x;
                style.positionTop = controller.position.y;

                if (m_BlockProvider == null)
                {
                    m_BlockProvider = new VFXBlockProvider(controller, (d, mPos) =>
                        {
                            AddBlock(mPos, d);
                        });
                }


                // Recreate label with good name // Dirty
                if (controller.context.inputType != VFXDataType.kNone)
                    m_HeaderTitle.text = string.Format("{0} {1}", controller.context.name, controller.context.inputType.ToString().Substring(1));
                else
                    m_HeaderTitle.text = controller.context.name;
                m_HeaderIcon.style.backgroundImage = GetIconForVFXType(controller.context.inputType);

                VFXContextType contextType = controller.context.contextType;

                RemoveFromClassList("spawner");
                RemoveFromClassList("init");
                RemoveFromClassList("update");
                RemoveFromClassList("output");


                foreach (int val in System.Enum.GetValues(typeof(CoordinateSpace)))
                {
                    m_HeaderSpace.RemoveFromClassList("space" + ((CoordinateSpace)val).ToString());
                }
                m_HeaderSpace.AddToClassList("space" + (controller.context.space).ToString());

                switch (contextType)
                {
                    case VFXContextType.kSpawner: AddToClassList("spawner"); break;
                    case VFXContextType.kInit: AddToClassList("init"); break;
                    case VFXContextType.kUpdate: AddToClassList("update"); break;
                    case VFXContextType.kOutput: AddToClassList("output"); break;
                    case VFXContextType.kEvent: AddToClassList("event"); break;
                    default: throw new Exception();
                }


                if (controller.context.outputType == VFXDataType.kNone)
                {
                    if (m_Footer.parent != null)
                        m_InsideContainer.Remove(m_Footer);
                }
                else
                {
                    if (m_Footer.parent == null)
                        m_InsideContainer.Add(m_Footer);
                    m_FooterTitle.text = controller.context.outputType.ToString().Substring(1);
                    m_FooterIcon.style.backgroundImage = GetIconForVFXType(controller.context.outputType);
                }

                HashSet<VisualElement> newInAnchors = new HashSet<VisualElement>();

                foreach (var inanchorcontroller in controller.flowInputAnchors)
                {
                    var existing = m_FlowInputConnectorContainer.Select(t => t as VFXFlowAnchor).FirstOrDefault(t => t.controller == inanchorcontroller);
                    if (existing == null)
                    {
                        var anchor = VFXFlowAnchor.Create(inanchorcontroller);
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

                foreach (var outanchorcontroller in controller.flowOutputAnchors)
                {
                    var existing = m_FlowOutputConnectorContainer.Select(t => t as VFXFlowAnchor).FirstOrDefault(t => t.controller == outanchorcontroller);
                    if (existing == null)
                    {
                        var anchor = VFXFlowAnchor.Create(outanchorcontroller);
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
            }
        }

        public VFXContextUI()
        {
            capabilities |= Capabilities.Selectable | Capabilities.Movable | Capabilities.Deletable | Capabilities.Ascendable;
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
            m_HeaderTitle = new Label() { name = "HeaderTitle" , text = "Title" };
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

            m_InsideContainer.Add(m_BlockContainer);


            m_Footer = new VisualElement() {
                name = "Footer"
            };
            m_FooterTitle = new Label() { name = "FooterTitle", text = "footer" };
            m_FooterTitle.AddToClassList("title");
            m_FooterIcon = new VisualElement() { name = "FooterIcon"};
            m_FooterIcon.AddToClassList("icon");
            m_Footer.Add(m_FooterIcon);
            m_Footer.Add(m_FooterTitle);
            m_Footer.AddToClassList("Extremity");

            Add(m_FlowOutputConnectorContainer);

            m_InsideContainer.Add(m_Footer);

            m_NodeContainer.Add(m_InsideContainer);

            ClearClassList();
            AddToClassList("VFXContext");

            m_DragDisplay = new VisualElement();
            m_DragDisplay.AddToClassList("dragdisplay");

            Add(new VisualElement() { name = "icon" });

            clippingOptions = VisualElement.ClippingOptions.NoClipping;

            RegisterCallback<ControllerChangedEvent>(OnChange);
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        public override void UpdatePresenterPosition()
        {
            controller.position = GetPosition().position;
        }

        void OnSpace()
        {
            controller.context.space = (CoordinateSpace)(((int)controller.context.space + 1) % (CoordinateSpaceInfo.SpaceCount));
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
                if (!controller.model.AcceptChild(block.controller.block))
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

            BlocksDropped(null, after, blocksUI, evt.imguiEvent.control);

            DragAndDrop.AcceptDrag();

            m_DragStarted = false;
            RemoveFromClassList("dropping");

            return EventPropagation.Stop;
        }

        public void BlocksDropped(VFXBlockController blockController, bool after, IEnumerable<VFXBlockUI> draggedBlocks, bool copy)
        {
            HashSet<VFXContextController> contexts = new HashSet<VFXContextController>();
            foreach (var draggedBlock in draggedBlocks)
            {
                contexts.Add(draggedBlock.context.controller);
            }

            using (var growContext = new GrowContext(this))
            {
                controller.BlocksDropped(blockController, after, draggedBlocks.Select(t => t.controller), copy);
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

            controller.RemoveBlock(block.controller.block);
        }

        private void InstantiateBlock(VFXBlockController blockController)
        {
            var blockUI = new VFXBlockUI();
            blockUI.controller = blockController;

            m_BlockContainer.Add(blockUI);
        }

        public void RefreshContext()
        {
            var blockControllers = controller.blockControllers;

            // recreate the children list based on the controller list to keep the order.

            var blocksUIs = new Dictionary<VFXBlockController, VFXBlockUI>();
            for (int i = 0; i < m_BlockContainer.childCount; ++i)
            {
                var child = m_BlockContainer.ElementAt(i) as VFXBlockUI;
                if (child != null)
                    blocksUIs.Add(child.controller, child);
            }

            foreach (var kv in blocksUIs)
            {
                m_BlockContainer.Remove(kv.Value);
            }
            foreach (var blockController in blockControllers)
            {
                VFXBlockUI blockUI;
                if (blocksUIs.TryGetValue(blockController, out blockUI))
                {
                    m_BlockContainer.Add(blockUI);
                }
                else
                {
                    InstantiateBlock(blockController);
                }
            }
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
                VFXView view = m_Context.GetFirstAncestorOfType<VFXView>();
                view.controller.ApplyChanges();
                (m_Context.panel as BaseVisualElementPanel).ValidateLayout();

                view.PushUnderContext(m_Context, m_Context.layout.size.y - m_PrevSize);
            }
        }

        void AddBlock(Vector2 position, VFXModelDescriptor<VFXBlock> descriptor)
        {
            VFXViewWindow.currentWindow.Focus();
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
                controller.AddBlock(blockIndex, descriptor.CreateInstance());
            }
        }

        public void OnCreateBlock(EventBase evt)
        {
            Vector2 referencePosition;
            if (evt is IMouseEvent)
            {
                referencePosition = (evt as IMouseEvent).mousePosition;
            }
            else
            {
                referencePosition = evt.imguiEvent.mousePosition;
            }

            VFXFilterWindow.Show(referencePosition, m_BlockProvider);
        }

        VFXBlockProvider m_BlockProvider = null;

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

        public virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is VFXContextUI || evt.target is VFXBlockUI)
            {
                evt.menu.AppendAction("Create Block", OnCreateBlock, e => ContextualMenu.MenuAction.StatusFlags.Normal);
                evt.menu.AppendSeparator();
            }
        }
    }
}
