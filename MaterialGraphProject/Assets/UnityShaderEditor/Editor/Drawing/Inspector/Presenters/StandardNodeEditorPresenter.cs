using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
{
    public sealed class StandardNodeEditorPresenter : AbstractNodeEditorPresenter
    {
        [SerializeField]
        AbstractMaterialNode m_Node;

        // TODO: Use abstract base class such that other editors can be used
        [SerializeField]
        List<IMGUISlotEditorPresenter> m_SlotEditorPresenters;

        public AbstractMaterialNode node
        {
            get { return m_Node; }
        }

        public IEnumerable<IMGUISlotEditorPresenter> slotEditorPresenters
        {
            get { return m_SlotEditorPresenters; }
        }

        public override void Initialize(INode node)
        {
            m_Node = node as AbstractMaterialNode;
            m_SlotEditorPresenters = new List<IMGUISlotEditorPresenter>();
            foreach (var slot in node.GetInputSlots<MaterialSlot>())
            {
                if (slot.concreteValueType == ConcreteSlotValueType.Vector1
                    || slot.concreteValueType == ConcreteSlotValueType.Vector2
                    || slot.concreteValueType == ConcreteSlotValueType.Vector3
                    || slot.concreteValueType == ConcreteSlotValueType.Vector4)
                {
                    var slotEditorPresenter = CreateInstance<IMGUISlotEditorPresenter>();
                    slotEditorPresenter.slot = slot;
                    m_SlotEditorPresenters.Add(slotEditorPresenter);
                }
            }
        }
    }
}
