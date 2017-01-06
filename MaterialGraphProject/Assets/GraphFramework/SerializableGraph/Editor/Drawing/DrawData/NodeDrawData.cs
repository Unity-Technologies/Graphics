using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public class NodeDrawData : NodePresenter
    {
        protected NodeDrawData()
        {}

        public INode node { get; private set; }

        public bool expanded = true;

        [SerializeField]
        protected List<GraphElementPresenter> m_Controls = new List<GraphElementPresenter>();

        public virtual IEnumerable<GraphElementPresenter> elements
        {
            get { return inputAnchors.Concat(outputAnchors).Cast<GraphElementPresenter>().Concat(m_Controls); }
        }

        public virtual void OnModified(ModificationScope scope)
        {
            expanded = node.drawState.expanded;

            if (scope == ModificationScope.Topological)
            {
                var slots = node.GetSlots<ISlot>().ToList();

                m_InputAnchors.RemoveAll(data => !slots.Contains(((AnchorDrawData)data).slot));
                m_OutputAnchors.RemoveAll(data => !slots.Contains(((AnchorDrawData)data).slot));

                AddSlots(slots.Except(m_InputAnchors.Concat(m_OutputAnchors).Select(data => ((AnchorDrawData)data).slot)));

                m_InputAnchors.Sort((x, y) => slots.IndexOf(((AnchorDrawData)x).slot) - slots.IndexOf(((AnchorDrawData)y).slot));
                m_OutputAnchors.Sort((x, y) => slots.IndexOf(((AnchorDrawData)x).slot) - slots.IndexOf(((AnchorDrawData)y).slot));
            }
        }

        public override void CommitChanges()
        {
            var drawData = node.drawState;
            drawData.position = position;
            node.drawState = drawData;
        }

        protected virtual IEnumerable<GraphElementPresenter> GetControlData()
        {
            return Enumerable.Empty<GraphElementPresenter>();
        }

        protected void AddSlots(IEnumerable<ISlot> slots)
        {
            foreach (var slot in slots)
            {
                var data = CreateInstance<AnchorDrawData>();
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

        // TODO JOCE: Move to OnEnable??
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
        }
    }
}
