using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Graphing;
using System.Linq;

namespace UnityEditor.Graphing.Drawing
{
    public class NodeDrawData : GraphElementPresenter
    {
        protected NodeDrawData()
        {}

        public INode node { get; private set; }

        public bool expanded = true;

        [SerializeField]
        protected List<GraphElementPresenter> m_Children = new List<GraphElementPresenter>();

        [SerializeField]
        protected List<AnchorDrawData> m_Anchors = new List<AnchorDrawData>();

        [SerializeField]
        protected List<GraphElementPresenter> m_Controls = new List<GraphElementPresenter>();

        public IEnumerable<GraphElementPresenter> elements
        {
            get { return m_Children.Concat(m_Anchors.Cast<GraphElementPresenter>()).Concat(m_Controls); }
        }

        public virtual void OnModified(ModificationScope scope)
        {
            expanded = node.drawState.expanded;

            if (scope == ModificationScope.Topological)
            {
                var slots = node.GetSlots<ISlot>().ToList();
                m_Anchors.RemoveAll(data => !slots.Contains(data.slot));
                AddSlots(slots.Except(m_Anchors.Select(x => x.slot)));
                m_Anchors.Sort((x, y) => slots.IndexOf(x.slot) - slots.IndexOf(y.slot));
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
            return new ControlDrawData[0];
        }

        protected void AddSlots(IEnumerable<ISlot> slots)
        {
            foreach (var input in slots)
            {
                var data = CreateInstance<AnchorDrawData>();
                data.Initialize(input);
                m_Anchors.Add(data);
            }
        }

        public virtual void Initialize(INode inNode)
        {
            node = inNode;
            capabilities |= Capabilities.Movable;

            if (node == null)
                return;

            name = inNode.name;
            expanded = node.drawState.expanded;

            var headerData = CreateInstance<HeaderDrawData>();
            headerData.Initialize(inNode);
            m_Children.Add(headerData);

            AddSlots(node.GetSlots<ISlot>());

            var controlData = GetControlData();
            m_Controls.AddRange(controlData);

            position = new Rect(node.drawState.position.x, node.drawState.position.y, 0, 0);
            //position
        }
    }
}
