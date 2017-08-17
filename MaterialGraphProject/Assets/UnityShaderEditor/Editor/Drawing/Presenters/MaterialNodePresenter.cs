using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
    public class MaterialNodePresenter : NodePresenter
    {
        public INode node { get; private set; }

        [SerializeField]
        protected List<GraphElementPresenter> m_Controls = new List<GraphElementPresenter>();

        [SerializeField]
        NodePreviewPresenter m_NodePreviewPresenter;

        [SerializeField]
        int m_Version;

        public bool requiresTime
        {
            get { return node is IRequiresTime; }
        }

        public IEnumerable<GraphElementPresenter> elements
        {
            // TODO JOCE Sub ideal to use yield, but will do for now.
            get
            {
                return inputAnchors.Concat(outputAnchors).Cast<GraphElementPresenter>().Concat(m_Controls).Concat(new [] {m_NodePreviewPresenter});
            }
        }

        public override bool expanded
        {
            get { return base.expanded; }
            set
            {
                if (base.expanded != value)
                {
                    base.expanded = value;
                    DrawState ds = node.drawState;
                    ds.expanded = value;
                    node.drawState = ds;
                }
            }
        }

        public override UnityEngine.Object[] GetObjectsToWatch()
        {
            var towatch = new List<UnityEngine.Object>();
            towatch.AddRange(base.GetObjectsToWatch());
            towatch.Add(m_NodePreviewPresenter);
            return towatch.ToArray();
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

            // TODO: Propagate callback rather than setting property
            if (m_NodePreviewPresenter != null)
                m_NodePreviewPresenter.modificationScope = scope;
        }

        public override void CommitChanges()
        {
            var drawState = node.drawState;
            drawState.position = position;
            node.drawState = drawState;
        }

        protected virtual IEnumerable<GraphElementPresenter> GetControlData()
        {
            return Enumerable.Empty<GraphElementPresenter>();
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

        public virtual void Initialize(INode inNode)
        {
            node = inNode;

            if (node == null)
                return;

            title = inNode.name;
            expanded = node.drawState.expanded;

            AddSlots(node.GetSlots<ISlot>());

            var controlData = GetControlData();
            m_Controls.AddRange(controlData);

            position = new Rect(node.drawState.position.x, node.drawState.position.y, 0, 0);

            m_Version = 0;
            AddPreview(inNode);
        }

        private void AddPreview(INode inNode)
        {
            var materialNode = inNode as AbstractMaterialNode;
            if (materialNode == null || !materialNode.hasPreview)
                return;

            m_NodePreviewPresenter = CreateInstance<NodePreviewPresenter>();
            m_NodePreviewPresenter.Initialize(materialNode);
        }
    }
}
