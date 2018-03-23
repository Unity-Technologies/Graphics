using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Profiling;

namespace UnityEditor.VFX.UI
{
    class VFXContextUI : VFXNodeUI, IDropTarget
    {
        // TODO: Unused except for debugging
        const string RectColorProperty = "rect-color";

        Image                       m_HeaderIcon;
        Image                       m_HeaderSpace;

        VisualElement               m_Footer;
        Image                       m_FooterIcon;
        Label                       m_FooterTitle;

        VisualElement               m_FlowInputConnectorContainer;
        VisualElement               m_FlowOutputConnectorContainer;
        VisualElement               m_BlockContainer;
        VisualElement               m_NoBlock;

        VisualElement               m_DragDisplay;

        public new VFXContextController controller
        {
            get { return base.controller as VFXContextController; }
        }
        protected override void OnNewController()
        {
            var blocks = new List<VFXModelDescriptor<VFXBlock>>(VFXLibrary.GetBlocks());

            m_CanHaveBlocks = blocks.Any(t => controller.model.AcceptChild(t.model));
        }

        public static string ContextEnumToClassName(string name)
        {
            if (name[0] != 'k')
            {
                Debug.LogError("Fix this since k has been removed from enums");
            }

            return name.Substring(1).ToLower();
        }

        protected override void SelfChange()
        {
            base.SelfChange();


            Profiler.BeginSample("VFXContextUI.CreateBlockProvider");
            if (m_BlockProvider == null)
            {
                m_BlockProvider = new VFXBlockProvider(controller, (d, mPos) =>
                    {
                        AddBlock(mPos, d);
                    });
            }
            Profiler.EndSample();

            m_HeaderIcon.image = GetIconForVFXType(controller.context.inputType);


            Profiler.BeginSample("VFXContextUI.SetAllStyleClasses");

            VFXContextType contextType = controller.context.contextType;
            foreach (var value in System.Enum.GetNames(typeof(VFXContextType)))
            {
                RemoveFromClassList(ContextEnumToClassName(value));
            }
            AddToClassList(ContextEnumToClassName(contextType.ToString()));

            var inputType = controller.context.inputType;
            if (inputType == VFXDataType.kNone)
            {
                inputType = controller.context.ownedType;
            }
            foreach (var value in System.Enum.GetNames(typeof(VFXDataType)))
            {
                RemoveFromClassList("inputType" + ContextEnumToClassName(value));
            }
            AddToClassList("inputType" + ContextEnumToClassName(inputType.ToString()));

            var outputType = controller.context.outputType;
            foreach (var value in System.Enum.GetNames(typeof(VFXDataType)))
            {
                RemoveFromClassList("outputType" + ContextEnumToClassName(value));
            }
            AddToClassList("outputType" + ContextEnumToClassName(outputType.ToString()));

            var type = controller.context.ownedType;
            foreach (var value in System.Enum.GetNames(typeof(VFXDataType)))
            {
                RemoveFromClassList("type" + ContextEnumToClassName(value));
            }
            AddToClassList("type" + ContextEnumToClassName(type.ToString()));


            foreach (int val in System.Enum.GetValues(typeof(CoordinateSpace)))
            {
                m_HeaderSpace.RemoveFromClassList("space" + ((CoordinateSpace)val).ToString());
            }
            m_HeaderSpace.AddToClassList("space" + (controller.context.space).ToString());
            Profiler.EndSample();

            if (controller.context.outputType == VFXDataType.kNone)
            {
                if (m_Footer.parent != null)
                    m_Footer.RemoveFromHierarchy();
            }
            else
            {
                if (m_Footer.parent == null)
                    mainContainer.Add(m_Footer);
                m_FooterTitle.text = controller.context.outputType.ToString().Substring(1);
                m_FooterIcon.image = GetIconForVFXType(controller.context.outputType);
            }

            Profiler.BeginSample("VFXContextUI.CreateInputFlow");
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
            Profiler.EndSample();

            Profiler.BeginSample("VFXContextUI.CreateInputFlow");
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
            Profiler.EndSample();

            RefreshContext();
        }

        public VFXContextUI() : base(UXMLHelper.GetUXMLPath("uxml/VFXContext.uxml"))
        {
            AddStyleSheetPath("VFXContext");
            capabilities |= Capabilities.Selectable | Capabilities.Movable | Capabilities.Deletable | Capabilities.Ascendable;

            m_FlowInputConnectorContainer = this.Q("flow-inputs");

            m_FlowOutputConnectorContainer = this.Q("flow-outputs");

            m_HeaderIcon = titleContainer.Q<Image>("icon");
            m_HeaderSpace = titleContainer.Q<Image>("header-space");
            m_HeaderSpace.AddManipulator(new Clickable(OnSpace));

            m_BlockContainer = this.Q("block-container");
            m_NoBlock = m_BlockContainer.Q("no-blocks");

            m_Footer = this.Q("footer");

            m_FooterTitle = m_Footer.Q<Label>("title-label");
            m_FooterIcon = m_Footer.Q<Image>("icon");

            AddToClassList("VFXContext");

            m_DragDisplay = new VisualElement();
            m_DragDisplay.AddToClassList("dragdisplay");

            RegisterCallback<ControllerChangedEvent>(OnChange);
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        bool m_CanHaveBlocks = false;

        public void OnMoved()
        {
            controller.position = GetPosition().position;
        }

        void OnSpace()
        {
            controller.context.space = (CoordinateSpace)(((int)controller.context.space + 1) % (CoordinateSpaceInfo.SpaceCount));
        }

        public bool CanDrop(IEnumerable<VFXBlockUI> blocks)
        {
            bool accept = true;
            if (blocks.Count() == 0) return false;
            foreach (var block in blocks)
            {
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

        public void DraggingBlocks(IEnumerable<VFXBlockUI> blocks, int index)
        {
            m_DragDisplay.RemoveFromHierarchy();

            if (!CanDrop(blocks))
            {
                return;
            }

            float y = GetBlockIndexY(index, false);

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

            return CanDrop(blocksUI);
        }

        public float GetBlockIndexY(int index, bool middle)
        {
            float y = 0;
            if (m_BlockContainer.childCount == 0)
            {
                return 0;
            }
            if (index >= m_BlockContainer.childCount)
            {
                return m_BlockContainer.ElementAt(m_BlockContainer.childCount - 1).layout.yMax;
            }
            else if (middle)
            {
                return m_BlockContainer.ElementAt(index).layout.center.y;
            }
            else
            {
                y = m_BlockContainer.ElementAt(index).layout.yMin;

                if (index > 0)
                {
                    y = (y + m_BlockContainer.ElementAt(index - 1).layout.yMax) * 0.5f;
                }
            }

            return y;
        }

        public int GetDragBlockIndex(Vector2 mousePosition)
        {
            for (int i = 0; i < m_BlockContainer.childCount; ++i)
            {
                float y = GetBlockIndexY(i, true);

                if (mousePosition.y < y)
                {
                    return i;
                }
            }

            return m_BlockContainer.childCount;
        }

        bool IDropTarget.DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            IEnumerable<VFXBlockUI> blocksUI = selection.Select(t => t as VFXBlockUI).Where(t => t != null);

            Vector2 mousePosition = m_BlockContainer.WorldToLocal(evt.mousePosition);

            int blockIndex = GetDragBlockIndex(mousePosition);

            DraggingBlocks(blocksUI, blockIndex);
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

            return true;
        }

        bool IDropTarget.DragPerform(DragPerformEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            DragFinished();

            Vector2 mousePosition = m_BlockContainer.WorldToLocal(evt.mousePosition);

            IEnumerable<VFXBlockUI> blocksUI = selection.OfType<VFXBlockUI>();
            if (!CanDrop(blocksUI))
                return true;

            int blockIndex = GetDragBlockIndex(mousePosition);

            BlocksDropped(blockIndex, blocksUI, evt.ctrlKey);

            DragAndDrop.AcceptDrag();

            m_DragStarted = false;
            RemoveFromClassList("dropping");

            return true;
        }

        public void BlocksDropped(int blockIndex, IEnumerable<VFXBlockUI> draggedBlocks, bool copy)
        {
            HashSet<VFXContextController> contexts = new HashSet<VFXContextController>();
            foreach (var draggedBlock in draggedBlocks)
            {
                contexts.Add(draggedBlock.context.controller);
            }

            using (var growContext = new GrowContext(this))
            {
                controller.BlocksDropped(blockIndex, draggedBlocks.Select(t => t.controller), copy);

                foreach (var context in contexts)
                {
                    context.ApplyChanges();
                }
            }
        }

        bool IDropTarget.DragExited()
        {
            // TODO: Do something when current drag is canceled
            DragFinished();
            m_DragStarted = false;

            return true;
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
            Profiler.BeginSample("VFXContextUI.InstantiateBlock");
            Profiler.BeginSample("VFXContextUI.new VFXBlockUI");
            var blockUI = new VFXBlockUI();
            Profiler.EndSample();
            blockUI.controller = blockController;

            m_BlockContainer.Add(blockUI);
            Profiler.EndSample();
        }

        public void RefreshContext()
        {
            Profiler.BeginSample("VFXContextUI.RefreshContext");
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
            if( blockControllers.Count() > 0 || ! m_CanHaveBlocks)
            {
                m_NoBlock.RemoveFromHierarchy();
            }
            else if( m_NoBlock.parent == null)
            {
                m_BlockContainer.Add(m_NoBlock);
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
            Profiler.EndSample();
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
                m_Context.controller.ApplyChanges();
                (m_Context.panel as BaseVisualElementPanel).ValidateLayout();

                view.PushUnderContext(m_Context, m_Context.layout.size.y - m_PrevSize);
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
                controller.AddBlock(blockIndex, descriptor.CreateInstance());
            }
        }

        public void OnCreateBlock(ContextualMenu.MenuAction evt)
        {
            Vector2 referencePosition = evt.eventInfo.mousePosition;

            OnCreateBlock(referencePosition);
        }

        public void OnCreateBlock(Vector2 referencePosition)
        {
            VFXFilterWindow.Show(VFXViewWindow.currentWindow, referencePosition, m_BlockProvider);
        }

        VFXBlockProvider m_BlockProvider = null;

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
