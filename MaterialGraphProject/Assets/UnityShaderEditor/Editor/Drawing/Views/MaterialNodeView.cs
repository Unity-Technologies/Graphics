using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Util;
using UnityEditor.MaterialGraph.Drawing.Controls;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using Edge = UnityEditor.Experimental.UIElements.GraphView.Edge;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class MaterialNodeView : Node
    {
        public AbstractMaterialNode node { get; private set; }

        protected List<GraphControlPresenter> m_Controls = new List<GraphControlPresenter>();

        public List<GraphControlPresenter> controls
        {
            get { return m_Controls; }
        }

        int m_Version;
        PreviewData m_Preview;

        public Texture previewTexture { get; private set; }

        public override bool expanded
        {
            get { return base.expanded; }
            set
            {
                if (base.expanded != value)
                {
                    base.expanded = value;
                    var ds = node.drawState;
                    ds.expanded = value;
                    node.drawState = ds;
                }
            }
        }

        public virtual void OnModified(ModificationScope scope)
        {
            m_Version++;

            expanded = node.drawState.expanded;

            if (scope == ModificationScope.Topological)
            {
                var slots = node.GetSlots<ISlot>().ToList();

                var anchorsToRemove = new List<VisualElement>();
                foreach (var anchor in inputContainer.Children())
                {
                    if (!slots.Contains(anchor.userData as ISlot))
                        anchorsToRemove.Add(anchor);
                }
                foreach (var ve in anchorsToRemove)
                    inputContainer.Remove(ve);

                anchorsToRemove.Clear();
                foreach (var anchor in outputContainer.Children())
                {
                    if (!slots.Contains(anchor.userData as ISlot))
                        anchorsToRemove.Add(anchor);
                }
                foreach (var ve in anchorsToRemove)
                    outputContainer.Remove(ve);

                AddSlots(slots.Except(inputContainer.Children().Concat(outputContainer.Children()).Select(data => data.userData as ISlot)));

                inputContainer.Sort((x, y) => slots.IndexOf(x.userData as ISlot) - slots.IndexOf(y.userData as ISlot));
                outputContainer.Sort((x, y) => slots.IndexOf(x.userData as ISlot) - slots.IndexOf(y.userData as ISlot));
            }
        }

        protected virtual IEnumerable<GraphControlPresenter> GetControlData()
        {
            return Enumerable.Empty<GraphControlPresenter>();
        }

        protected void AddSlots(IEnumerable<ISlot> slots)
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
                {
                    outputContainer.Add(data);
                }
                else
                {
                    inputContainer.Add(data);
                }
            }
        }

        public virtual void Initialize(INode inNode, PreviewSystem previewSystem)
        {
            node = inNode as AbstractMaterialNode;
            userData = node;

            if (node == null)
                return;

            title = inNode.name;
            expanded = node.drawState.expanded;

            AddSlots(node.GetSlots<ISlot>());

            var controlData = GetControlData();
            controls.AddRange(controlData);

            SetPosition(new Rect(node.drawState.position.x, node.drawState.position.y, 0, 0));

            m_Version = 0;

            m_Preview = previewSystem.GetPreview(inNode);
            m_Preview.onPreviewChanged += OnPreviewChanged;

            node.onReplaced += OnReplaced;

            // From OnDataChange()

            m_PreviewToggle.text = node.previewExpanded ? "▲" : "▼";
            if (node.hasPreview)
                m_PreviewToggle.RemoveFromClassList("inactive");
            else
                m_PreviewToggle.AddToClassList("inactive");

            UpdateControls();

            UpdatePreviewTexture(node.previewExpanded ? previewTexture : null);

            m_NodeGuid = node.guid;
            if (node is PreviewNode)
            {
                if (!m_ResizeHandleAdded)
                {
                    m_ResizeHandle = new VisualElement() { name = "resize", text = "" };
                    m_ResizeHandle.AddManipulator(new Draggable(OnResize));
                    Add(m_ResizeHandle);

                    m_ResizeHandleAdded = true;
                }

                UpdateSize();
            }
        }

        void OnReplaced(INode previous, INode current)
        {
            node = current as AbstractMaterialNode;
        }

        void OnPreviewChanged()
        {
            previewTexture = m_Preview.texture;
            UpdatePreviewTexture(node.previewExpanded ? previewTexture : null);
            m_Version++;
        }

        public void Dispose()
        {
            if (m_Preview != null)
            {
                m_Preview.onPreviewChanged -= OnPreviewChanged;
                m_Preview = null;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////

        VisualElement m_ControlsContainer;
        List<VisualElement> m_ControlViews;
        Guid m_NodeGuid;
        VisualElement m_PreviewToggle;
        VisualElement m_ResizeHandle;
        Image m_PreviewImage;
        bool m_IsScheduled;

        bool m_ResizeHandleAdded;

        public MaterialNodeView()
        {
            CreateContainers();

            AddToClassList("MaterialNode");
        }

        void CreateContainers()
        {
            m_ControlsContainer = new VisualElement
            {
                name = "controls"
            };
            leftContainer.Add(m_ControlsContainer);
            m_ControlViews = new List<VisualElement>();

            m_PreviewToggle = new VisualElement { name = "toggle", text = "" };
            m_PreviewToggle.AddManipulator(new Clickable(OnPreviewToggle));
            leftContainer.Add(m_PreviewToggle);

            m_PreviewImage = new Image
            {
                name = "preview",
                pickingMode = PickingMode.Ignore,
                image = Texture2D.whiteTexture
            };

            leftContainer.Add(m_PreviewImage);

            m_ResizeHandleAdded = false;
        }

        void OnResize(Vector2 deltaSize)
        {
            float updatedWidth = leftContainer.layout.width + deltaSize.x;
            float updatedHeight = m_PreviewImage.layout.height + deltaSize.y;

            PreviewNode previewNode = node as PreviewNode;

            if (previewNode != null)
            {
                previewNode.SetDimensions(updatedWidth, updatedHeight);
                UpdateSize();
            }
        }

        void OnPreviewToggle()
        {
            node.previewExpanded = !node.previewExpanded;
            m_PreviewToggle.text = node.previewExpanded ? "▲" : "▼";
        }

        void UpdatePreviewTexture(Texture previewTexture)
        {
            if (previewTexture == null)
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
                m_PreviewImage.image = previewTexture;
            }
            Dirty(ChangeType.Repaint);

        }

        void UpdateControls()
        {
            if (!node.guid.Equals(m_NodeGuid))
            {
                m_ControlViews.Clear();
                foreach (var propertyInfo in node.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    foreach (IControlAttribute attribute in propertyInfo.GetCustomAttributes(typeof(IControlAttribute), false))
                        m_ControlViews.Add(attribute.InstantiateControl(node, propertyInfo));
                }
            }

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
            {
                return;
            }

            float width = previewNode.width;
            float height = previewNode.height;

            leftContainer.style.width = width;
            m_PreviewImage.style.height = height;
        }
    }
}
