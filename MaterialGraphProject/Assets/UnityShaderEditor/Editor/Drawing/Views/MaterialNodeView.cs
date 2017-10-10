using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Util;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using Edge = UnityEditor.Experimental.UIElements.GraphView.Edge;

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
            /*
            if (scope == ModificationScope.Topological)
            {
                var slots = node.GetSlots<ISlot>().ToList();

                inputAnchors.RemoveAll(data => !slots.Contains(((GraphAnchorPresenter)data).slot));
                outputAnchors.RemoveAll(data => !slots.Contains(((GraphAnchorPresenter)data).slot));

                AddSlots(slots.Except(inputAnchors.Concat(outputAnchors).Select(data => ((GraphAnchorPresenter)data).slot)));

                inputAnchors.Sort((x, y) => slots.IndexOf(((GraphAnchorPresenter)x).slot) - slots.IndexOf(((GraphAnchorPresenter)y).slot));
                outputAnchors.Sort((x, y) => slots.IndexOf(((GraphAnchorPresenter)x).slot) - slots.IndexOf(((GraphAnchorPresenter)y).slot));
            }
            */
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
        List<GraphControlPresenter> m_CurrentControls;
        VisualElement m_PreviewToggle;
        Image m_PreviewImage;
        bool m_IsScheduled;

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
            m_CurrentControls = new List<GraphControlPresenter>();

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
        }

        void OnPreviewToggle()
        {
            AbstractMaterialNode materialNode = node;
            if (presenter != null)
                materialNode = GetPresenter<MaterialNodePresenter>().node;

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
            if (controls.SequenceEqual(m_CurrentControls) && expanded)
                return;

            m_ControlsContainer.Clear();
            m_CurrentControls.Clear();
            Dirty(ChangeType.Layout);

            if (!expanded)
                return;

            foreach (var controlData in controls)
            {
                m_ControlsContainer.Add(new IMGUIContainer(controlData.OnGUIHandler)
                {
                    name = "element"
                });
                m_CurrentControls.Add(controlData);
            }
        }

        void UpdateControls(MaterialNodePresenter nodeData)
        {
            if (nodeData.controls.SequenceEqual(m_CurrentControls) && nodeData.expanded)
                return;

            m_ControlsContainer.Clear();
            m_CurrentControls.Clear();
            Dirty(ChangeType.Layout);

            if (!nodeData.expanded)
                return;

            foreach (var controlData in nodeData.controls)
            {
                m_ControlsContainer.Add(new IMGUIContainer(controlData.OnGUIHandler)
                {
                    name = "element"
                });
                m_CurrentControls.Add(controlData);
            }
        }

        public override void SetPosition(Rect newPos)
        {
            var nodePresenter = GetPresenter<MaterialNodePresenter>();
            if (nodePresenter != null)
                nodePresenter.position = newPos;
            base.SetPosition(newPos);
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            var nodePresenter = GetPresenter<MaterialNodePresenter>();

            if (nodePresenter == null)
            {
                m_ControlsContainer.Clear();
                m_CurrentControls.Clear();
                UpdatePreviewTexture(null);
                return;
            }

            m_PreviewToggle.text = nodePresenter.node.previewExpanded ? "▲" : "▼";
            if (nodePresenter.node.hasPreview)
                m_PreviewToggle.RemoveFromClassList("inactive");
            else
                m_PreviewToggle.AddToClassList("inactive");

            UpdateControls(nodePresenter);

            UpdatePreviewTexture(nodePresenter.node.previewExpanded ? nodePresenter.previewTexture : null);
        }
    }
}
