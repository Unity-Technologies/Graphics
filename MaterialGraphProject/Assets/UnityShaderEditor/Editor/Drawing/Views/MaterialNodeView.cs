using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph.Drawing
{
    public sealed class MaterialNodeView : Node
    {
        List<VisualElement> m_ControlViews;
        PreviewRenderData m_PreviewRenderData;
        PreviewTextureView m_PreviewTextureView;
        VisualElement m_ControlsContainer;
        VisualElement m_PreviewContainer;
        List<Attacher> m_Attachers;
        VisualElement m_ControlsDivider;

        public void Initialize(AbstractMaterialNode inNode, PreviewManager previewManager)
        {
            AddToClassList("MaterialNode");

            if (inNode == null)
                return;

            node = inNode;
            persistenceKey = node.guid.ToString();
            UpdateTitle();

            m_ControlsContainer = new VisualElement
            {
                name = "controls"
            };
            extensionContainer.Add(m_ControlsContainer);
            m_ControlsDivider = new VisualElement {name = "divider"};
            m_ControlsDivider.AddToClassList("horizontal");

            if (node.hasPreview)
            {
                m_PreviewContainer = new VisualElement { name = "previewContainer" };
                m_PreviewContainer.AddToClassList("expanded");
                {
                    m_PreviewTextureView = new PreviewTextureView
                    {
                        name = "preview",
                        pickingMode = PickingMode.Ignore,
                        image = Texture2D.whiteTexture
                    };
                    m_PreviewRenderData = previewManager.GetPreview(inNode);
                    m_PreviewRenderData.onPreviewChanged += UpdatePreviewTexture;
                    UpdatePreviewTexture();

                    var collapsePreviewButton = new VisualElement { name = "collapse"};
                    collapsePreviewButton.Add(new VisualElement { name = "icon" });
                    collapsePreviewButton.AddManipulator(new Clickable(() =>
                    {
                        node.owner.owner.RegisterCompleteObjectUndo("Collapse Preview");
                        UpdatePreviewExpandedState(false);
                    }));
                    UpdatePreviewExpandedState(node.previewExpanded);
                    m_PreviewTextureView.Add(collapsePreviewButton);

                    var expandPreviewButton = new VisualElement { name = "expand"};
                    expandPreviewButton.Add(new VisualElement { name = "icon"});
                    expandPreviewButton.AddManipulator(new Clickable(() =>
                    {
                        node.owner.owner.RegisterCompleteObjectUndo("Expand Preview");
                        UpdatePreviewExpandedState(true);
                    }));
                    m_PreviewContainer.Add(expandPreviewButton);
                }

                extensionContainer.Add(m_PreviewContainer);
            }

            m_ControlViews = new List<VisualElement>();
            foreach (var propertyInfo in node.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            foreach (IControlAttribute attribute in propertyInfo.GetCustomAttributes(typeof(IControlAttribute), false))
                m_ControlViews.Add(attribute.InstantiateControl(node, propertyInfo));
            m_Attachers = new List<Attacher>(node.GetInputSlots<MaterialSlot>().Count());

            AddSlots(node.GetSlots<MaterialSlot>());
            expanded = node.drawState.expanded;
            RefreshExpandedState(); //This should not be needed. GraphView needs to improve the extension api here
            UpdatePortInputVisibilities();

            SetPosition(new Rect(node.drawState.position.x, node.drawState.position.y, 0, 0));
            UpdateControls();

            if (node is PreviewNode)
            {
                var resizeHandle = new Label { name = "resize", text = "" };
                resizeHandle.AddManipulator(new Draggable(OnResize));
                Add(resizeHandle);
                UpdateSize();
            }
        }

        public AbstractMaterialNode node { get; private set; }

        public override bool expanded
        {
            get { return base.expanded; }
            set
            {
                if (base.expanded != value)
                    base.expanded = value;

                if (node.drawState.expanded != value)
                {
                    var ds = node.drawState;
                    ds.expanded = value;
                    node.drawState = ds;
                }

                UpdateControls();
                UpdatePortInputVisibilities();
                RefreshExpandedState(); //This should not be needed. GraphView needs to improve the extension api here
            }
        }

        void UpdatePreviewExpandedState(bool expanded)
        {
            node.previewExpanded = expanded;
            if (m_PreviewContainer == null)
                return;
            if (expanded)
            {
                if (m_PreviewTextureView.parent != m_PreviewContainer)
                {
                    m_PreviewContainer.Add(m_PreviewTextureView);
                }
                m_PreviewContainer.AddToClassList("expanded");
                m_PreviewContainer.RemoveFromClassList("collapsed");
            }
            else
            {
                if (m_PreviewTextureView.parent == m_PreviewContainer)
                {
                    m_PreviewTextureView.RemoveFromHierarchy();
                }
                m_PreviewContainer.RemoveFromClassList("expanded");
                m_PreviewContainer.AddToClassList("collapsed");
            }
        }

        void UpdateTitle()
        {
            var subGraphNode = node as SubGraphNode;
            if (subGraphNode != null && subGraphNode.subGraphAsset != null)
                title = subGraphNode.subGraphAsset.name;
            else
                title = node.name;
        }

        public void OnModified(ModificationScope scope)
        {
            UpdateTitle();
            if (node.hasPreview)
                UpdatePreviewExpandedState(node.previewExpanded);
            expanded = node.drawState.expanded;

            // Update slots to match node modification
            if (scope == ModificationScope.Topological)
            {
                var slots = node.GetSlots<MaterialSlot>().ToList();

                var anchorsToRemove = new List<VisualElement>();
                foreach (var anchor in inputContainer.Children())
                    if (!slots.Contains(anchor.userData as MaterialSlot))
                        anchorsToRemove.Add(anchor);
                foreach (var anchorElement in anchorsToRemove)
                {
                    inputContainer.Remove(anchorElement);
                    var attacher = m_Attachers.FirstOrDefault(a => a.target == anchorElement);
                    if (attacher != null)
                    {
                        attacher.Detach();
                        attacher.element.parent.Remove(attacher.element);
                        m_Attachers.Remove(attacher);
                    }
                }

                anchorsToRemove.Clear();
                foreach (var anchor in outputContainer.Children())
                    if (!slots.Contains(anchor.userData as MaterialSlot))
                        anchorsToRemove.Add(anchor);
                foreach (var ve in anchorsToRemove)
                    outputContainer.Remove(ve);

                foreach (var port in inputContainer.Union(outputContainer).OfType<Port>())
                {
                    var slot = (MaterialSlot)port.userData;
                    port.portName = slot.displayName;
                }

                AddSlots(slots.Except(inputContainer.Children().Concat(outputContainer.Children()).Select(data => data.userData as MaterialSlot)));

                if (inputContainer.childCount > 0)
                    inputContainer.Sort((x, y) => slots.IndexOf(x.userData as MaterialSlot) - slots.IndexOf(y.userData as MaterialSlot));
                if (outputContainer.childCount > 0)
                    outputContainer.Sort((x, y) => slots.IndexOf(x.userData as MaterialSlot) - slots.IndexOf(y.userData as MaterialSlot));
            }

            UpdatePortInputVisibilities();

            foreach (var control in m_ControlViews)
            {
                var listener = control as INodeModificationListener;
                if (listener != null)
                    listener.OnNodeModified(scope);
            }
        }

        void AddSlots(IEnumerable<MaterialSlot> slots)
        {
            foreach (var slot in slots)
            {
                if (slot.hidden)
                    continue;

                var port = InstantiatePort(Orientation.Horizontal, slot.isInputSlot ? Direction.Input : Direction.Output, null);
                port.portName = slot.displayName;
                port.userData = slot;
                port.visualClass = slot.concreteValueType.ToClassName();

                if (slot.isOutputSlot)
                {
                    outputContainer.Add(port);
                }
                else
                {
                    inputContainer.Add(port);
                    var portInputView = new PortInputView(slot);
                    Add(portInputView);
                    m_Attachers.Add(new Attacher(portInputView, port, SpriteAlignment.LeftCenter) { distance = -8f });
                }
            }
        }

        public void UpdatePortInputVisibilities()
        {
            foreach (var attacher in m_Attachers)
            {
                var slot = (MaterialSlot)attacher.target.userData;
                attacher.element.visible = expanded && !node.owner.GetEdges(node.GetSlotReference(slot.id)).Any();
            }
        }

        public void UpdatePortInputTypes()
        {
            foreach (var anchor in inputContainer.Concat(outputContainer).OfType<Port>())
            {
                var slot = (MaterialSlot) anchor.userData;
                anchor.portName = slot.displayName;
                anchor.visualClass = slot.concreteValueType.ToClassName();
            }

            foreach (var attacher in m_Attachers)
            {
                var portInputView = (PortInputView)attacher.element;
                portInputView.UpdateSlotType();
            }
        }

        void OnResize(Vector2 deltaSize)
        {
            var updatedWidth = topContainer.layout.width + deltaSize.x;
            var updatedHeight = m_PreviewTextureView.layout.height + deltaSize.y;

            var previewNode = node as PreviewNode;
            if (previewNode != null)
            {
                previewNode.SetDimensions(updatedWidth, updatedHeight);
                UpdateSize();
            }
        }

        void UpdatePreviewTexture()
        {
            if (m_PreviewRenderData.texture == null || !node.previewExpanded)
            {
                m_PreviewTextureView.visible = false;
                m_PreviewTextureView.image = Texture2D.blackTexture;
            }
            else
            {
                m_PreviewTextureView.visible = true;
                m_PreviewTextureView.AddToClassList("visible");
                m_PreviewTextureView.RemoveFromClassList("hidden");
                if (m_PreviewTextureView.image != m_PreviewRenderData.texture)
                    m_PreviewTextureView.image = m_PreviewRenderData.texture;
                else
                    m_PreviewTextureView.Dirty(ChangeType.Repaint);
            }
        }

        void UpdateControls()
        {
            if (!expanded)
            {
                m_ControlsContainer.Clear();
                m_ControlsDivider.RemoveFromHierarchy();
            }
            else if (m_ControlsContainer.childCount != m_ControlViews.Count)
            {
                m_ControlsContainer.Clear();
                foreach (var view in m_ControlViews)
                    m_ControlsContainer.Add(view);
                extensionContainer.Add(m_ControlsDivider);
                if (m_PreviewContainer != null)
                    m_ControlsDivider.PlaceBehind(m_PreviewContainer);
            }
        }

        void UpdateSize()
        {
            var previewNode = node as PreviewNode;

            if (previewNode == null)
                return;

            var width = previewNode.width;
            var height = previewNode.height;

            m_PreviewTextureView.style.height = height;
        }

        public void Dispose()
        {
            foreach (var attacher in m_Attachers)
            {
                ((PortInputView) attacher.element).Dispose();
                attacher.Detach();
                attacher.element.parent.Remove(attacher.element);
            }
            m_Attachers.Clear();

            node = null;
            if (m_PreviewRenderData != null)
            {
                m_PreviewRenderData.onPreviewChanged -= UpdatePreviewTexture;
                m_PreviewRenderData = null;
            }
        }
    }
}
