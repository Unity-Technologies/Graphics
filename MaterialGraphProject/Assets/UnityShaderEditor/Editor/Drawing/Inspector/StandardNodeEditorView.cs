using System.Linq;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.Drawing.Inspector
{
    public class StandardNodeEditorView : AbstractNodeEditorView
    {
        NodeEditorHeaderView m_HeaderView;
        VisualElement m_SlotsContainer;
        VisualElement m_DefaultSlotValuesSection;
        AbstractMaterialNode m_Node;
        int m_SlotsHash;

        public override INode node
        {
            get { return m_Node; }
            set
            {
                if (value == m_Node)
                    return;
                if (m_Node != null)
                    m_Node.onModified -= OnModified;
                m_Node = value as AbstractMaterialNode;
                OnModified(m_Node, ModificationScope.Node);
                if (m_Node != null)
                    m_Node.onModified += OnModified;
            }
        }

        public override void Dispose()
        {
            if (m_Node != null)
                m_Node.onModified -= OnModified;
        }

        public StandardNodeEditorView()
        {
            AddToClassList("nodeEditor");

            m_HeaderView = new NodeEditorHeaderView() { type = "node" };
            Add(m_HeaderView);

            m_DefaultSlotValuesSection = new VisualElement();
            m_DefaultSlotValuesSection.AddToClassList("section");
            {
                var sectionTitle = new VisualElement { text = "Default Slot Values" };
                sectionTitle.AddToClassList("title");
                m_DefaultSlotValuesSection.Add(sectionTitle);

                m_SlotsContainer = new VisualElement { name = "slots" };
                m_DefaultSlotValuesSection.Add(m_SlotsContainer);
            }
            Add(m_DefaultSlotValuesSection);
        }

        void OnModified(INode changedNode, ModificationScope scope)
        {
            if (node == null)
                return;

            m_HeaderView.title = node.name;

            var slotsHash = UIUtilities.GetHashCode(node.GetInputSlots<MaterialSlot>().Select(s => UIUtilities.GetHashCode(s.slotReference.nodeGuid.GetHashCode(), s.slotReference.slotId)));

            if (slotsHash != m_SlotsHash)
            {
                m_SlotsHash = slotsHash;
                m_SlotsContainer.Clear();
                foreach (var slot in node.GetInputSlots<MaterialSlot>())
                    m_SlotsContainer.Add(new IMGUISlotEditorView(slot));

                if (m_SlotsContainer.Any())
                    m_DefaultSlotValuesSection.RemoveFromClassList("hidden");
                else
                    m_DefaultSlotValuesSection.AddToClassList("hidden");
            }
        }
    }
}
