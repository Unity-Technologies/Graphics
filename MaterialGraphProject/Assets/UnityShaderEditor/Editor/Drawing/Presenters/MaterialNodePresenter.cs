using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using UnityEngine;

namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
    public class MaterialNodePresenter : NodePresenter, IDisposable
    {
        public AbstractMaterialNode node { get; private set; }

        [SerializeField]
        protected List<GraphControlPresenter> m_Controls = new List<GraphControlPresenter>();

        public List<GraphControlPresenter> controls
        {
            get { return m_Controls; }
        }

        [SerializeField]
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

                inputAnchors.RemoveAll(data => !slots.Contains(((GraphAnchorPresenter)data).slot));
                outputAnchors.RemoveAll(data => !slots.Contains(((GraphAnchorPresenter)data).slot));

                AddSlots(slots.Except(inputAnchors.Concat(outputAnchors).Select(data => ((GraphAnchorPresenter)data).slot)));

                inputAnchors.Sort((x, y) => slots.IndexOf(((GraphAnchorPresenter)x).slot) - slots.IndexOf(((GraphAnchorPresenter)y).slot));
                outputAnchors.Sort((x, y) => slots.IndexOf(((GraphAnchorPresenter)x).slot) - slots.IndexOf(((GraphAnchorPresenter)y).slot));
            }
        }

        public override void CommitChanges()
        {
            var drawState = node.drawState;
            drawState.position = position;
            node.drawState = drawState;
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

                var data = CreateInstance<GraphAnchorPresenter>();
                data.capabilities &= ~Capabilities.Movable;
                data.Initialize(slot);

                if (slot.isOutputSlot)
                {
                    outputAnchors.Add(data);
                }
                else
                {
                    inputAnchors.Add(data);
                }
            }
        }

        protected MaterialNodePresenter()
        {}

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

            position = new Rect(node.drawState.position.x, node.drawState.position.y, 0, 0);

            m_Version = 0;

            m_Preview = previewSystem.GetPreview(inNode);
            m_Preview.onPreviewChanged += OnPreviewChanged;

            node.onReplaced += OnReplaced;
        }

        void OnReplaced(INode previous, INode current)
        {
            node = current as AbstractMaterialNode;
        }

        void OnPreviewChanged()
        {
            previewTexture = m_Preview.texture;
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
    }
}
