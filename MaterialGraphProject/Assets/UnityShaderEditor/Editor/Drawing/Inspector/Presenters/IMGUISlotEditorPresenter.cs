using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
{
    public class IMGUISlotEditorPresenter : ScriptableObject
    {
        [SerializeField]
        MaterialSlot m_Slot;

        public MaterialSlot slot
        {
            get { return m_Slot; }
            set { m_Slot = value; }
        }

        public Vector4 value
        {
            get { return m_Slot.currentValue; }
            set
            {
                if (value == slot.currentValue)
                    return;
                slot.currentValue = value;
                m_Slot.owner.onModified(m_Slot.owner, ModificationScope.Node);
            }
        }
    }
}
