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

        [SerializeField]
        List<IMGUISlotEditorPresenter> m_SlotEditorPresenters;

        public override INode node
        {
            get { return m_Node; }
            set
            {
                m_Node = (AbstractMaterialNode)value;
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

        public IEnumerable<IMGUISlotEditorPresenter> slotEditorPresenters
        {
            get { return m_SlotEditorPresenters; }
        }
    }
}
