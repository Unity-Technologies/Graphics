using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.MaterialGraph.Drawing.Controls;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    public sealed class MaterialNodeView : Node
    {
        List<VisualElement> m_ControlViews;
        PreviewData m_Preview;
        PreviewView m_PreviewImage;
        VisualElement m_PreviewToggle;
        VisualElement m_ControlsContainer;

        public MaterialNodeView(AbstractMaterialNode inNode, PreviewSystem previewSystem)
        {
            AddToClassList("MaterialNode");

            if (inNode == null)
                return;

            node = inNode;
            title = inNode.name;

            m_ControlsContainer = new VisualElement
            {
                name = "controls"
            };
            leftContainer.Add(m_ControlsContainer);

            m_PreviewToggle = new VisualElement { name = "toggle", text = "" };
            m_PreviewToggle.AddManipulator(new Clickable(() => previewExpanded = !previewExpanded));
            if (node.hasPreview)
                m_PreviewToggle.RemoveFromClassList("inactive");
            else
                m_PreviewToggle.AddToClassList("inactive");
            previewExpanded = node.previewExpanded;
            leftContainer.Add(m_PreviewToggle);

            m_PreviewImage = new PreviewView
            {
                name = "preview",
                pickingMode = PickingMode.Ignore,
                image = Texture2D.whiteTexture
            };
            m_Preview = previewSystem.GetPreview(inNode);
            m_Preview.onPreviewChanged += UpdatePreviewTexture;
            UpdatePreviewTexture();
            leftContainer.Add(m_PreviewImage);

            m_ControlViews = new List<VisualElement>();
            foreach (var propertyInfo in node.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            foreach (IControlAttribute attribute in propertyInfo.GetCustomAttributes(typeof(IControlAttribute), false))
                m_ControlViews.Add(attribute.InstantiateControl(node, propertyInfo));
            expanded = node.drawState.expanded;

            AddSlots(node.GetSlots<ISlot>());

            SetPosition(new Rect(node.drawState.position.x, node.drawState.position.y, 0, 0));
            UpdateControls();

            if (node is PreviewNode)
            {
                var resizeHandle = new VisualElement { name = "resize", text = "" };
                resizeHandle.AddManipulator(new Draggable(OnResize));
                Add(resizeHandle);

                UpdateSize();
            }

            clippingOptions = ClippingOptions.ClipContents;
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
            }
        }

        bool previewExpanded
        {
            get { return node.previewExpanded; }
            set
            {
                node.previewExpanded = value;
                m_PreviewToggle.text = node.previewExpanded ? "▲" : "▼";
            }
        }

        public void OnModified(ModificationScope scope)
        {
            expanded = node.drawState.expanded;

            // Update slots to match node modification
            if (scope == ModificationScope.Topological)
            {
                var slots = node.GetSlots<ISlot>().ToList();

                var anchorsToRemove = new List<VisualElement>();
                foreach (var anchor in inputContainer.Children())
                    if (!slots.Contains(anchor.userData as ISlot))
                        anchorsToRemove.Add(anchor);
                foreach (var ve in anchorsToRemove)
                    inputContainer.Remove(ve);

                anchorsToRemove.Clear();
                foreach (var anchor in outputContainer.Children())
                    if (!slots.Contains(anchor.userData as ISlot))
                        anchorsToRemove.Add(anchor);
                foreach (var ve in anchorsToRemove)
                    outputContainer.Remove(ve);

                AddSlots(slots.Except(inputContainer.Children().Concat(outputContainer.Children()).Select(data => data.userData as ISlot)));

                if (inputContainer.childCount > 0)
                    inputContainer.Sort((x, y) => slots.IndexOf(x.userData as ISlot) - slots.IndexOf(y.userData as ISlot));
                if (outputContainer.childCount > 0)
                    outputContainer.Sort((x, y) => slots.IndexOf(x.userData as ISlot) - slots.IndexOf(y.userData as ISlot));
            }
        }

        void AddSlots(IEnumerable<ISlot> slots)
        {
            foreach (var slot in slots)
            {
                if (slot.hidden)
                    continue;

                var data = InstantiateNodeAnchor(Orientation.Horizontal, slot.isInputSlot ? Direction.Input : Direction.Output, typeof(Vector4));
                data.capabilities &= ~Capabilities.Movable;
                data.anchorName = slot.displayName;
                data.userData = slot;

                if (slot.isOutputSlot)
                    outputContainer.Add(data);
                else
                    inputContainer.Add(data);
            }
        }

        void OnResize(Vector2 deltaSize)
        {
            var updatedWidth = leftContainer.layout.width + deltaSize.x;
            var updatedHeight = m_PreviewImage.layout.height + deltaSize.y;

            var previewNode = node as PreviewNode;
            if (previewNode != null)
            {
                previewNode.SetDimensions(updatedWidth, updatedHeight);
                UpdateSize();
            }
        }

        void UpdatePreviewTexture()
        {
            if (m_Preview.texture == null || !node.previewExpanded)
            {
                m_PreviewImage.visible = false;
                m_PreviewImage.RemoveFromClassList("visible");
                m_PreviewImage.AddToClassList("hidden");
                m_PreviewImage.image = Texture2D.whiteTexture;
            }
            else
            {
                m_PreviewImage.visible = true;
                m_PreviewImage.AddToClassList("visible");
                m_PreviewImage.RemoveFromClassList("hidden");
                m_PreviewImage.image = m_Preview.texture;
            }
            Dirty(ChangeType.Repaint);
        }

        void UpdateControls()
        {
            if (!expanded)
            {
                m_ControlsContainer.Clear();
            }
            else if (m_ControlsContainer.childCount != m_ControlViews.Count)
            {
                m_ControlsContainer.Clear();
                foreach (var view in m_ControlViews)
                    m_ControlsContainer.Add(view);
            }
        }

        void UpdateSize()
        {
            var previewNode = node as PreviewNode;

            if (previewNode == null)
                return;

            var width = previewNode.width;
            var height = previewNode.height;

            leftContainer.style.width = width;
            m_PreviewImage.style.height = height;
        }

        public void Dispose()
        {
            node = null;
            if (m_Preview != null)
            {
                m_Preview.onPreviewChanged -= UpdatePreviewTexture;
                m_Preview = null;
            }
        }
    }
}
