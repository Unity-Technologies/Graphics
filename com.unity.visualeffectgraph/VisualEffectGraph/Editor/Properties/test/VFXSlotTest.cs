using UnityEngine;

namespace UnityEngine.Experimental.VFX
{
    static class VFXSlotTest
    {
        public static void Run()
        {
            var outputNodeSphere = new OutputTestNode(VFXProperty.Create<VFXSphereType>("sphere"));
            var inputNodeSphere = new InputTestNode(VFXProperty.Create<VFXSphereType>("sphere"));
            var inputNodeVector = new InputTestNode(VFXProperty.Create<VFXFloat3Type>("position"));

            var outputPositionSlot = outputNodeSphere.Slot.GetChild(0);
            inputNodeVector.Slot.SetValue(new Vector3(0.0f,0.0f,42.0f));

            inputNodeSphere.Slot.Link(outputNodeSphere.Slot);
            inputNodeVector.Slot.Link(outputPositionSlot);

            outputPositionSlot.SetValue(new Vector3(1.0f, 2.0f, 3.0f));
            inputNodeVector.Slot.Unlink();
        }
    }

    class InputTestNode : VFXPropertySlotObserver
    {
        public InputTestNode(VFXProperty prop)
        {
            m_Slot = new VFXInputSlot(prop, this);
        }

        public void OnSlotEvent(VFXPropertySlot.Event type, VFXPropertySlot slot)
        {
            string valueStr = slot.ValueRef == null ? "<NONE>" : slot.ValueRef.ToString();
            Debug.Log("Slot event on input: " + valueStr + " " + type);
        }

        public VFXInputSlot Slot { get { return m_Slot; }}
        private VFXInputSlot m_Slot;
    }

    class OutputTestNode : VFXPropertySlotObserver
    {
        public OutputTestNode(VFXProperty prop)
        {
            m_Slot = new VFXOutputSlot(prop, this);
        }

        public void OnSlotEvent(VFXPropertySlot.Event type, VFXPropertySlot slot)
        {
            string valueStr = slot.ValueRef == null ? "<NONE>" : slot.ValueRef.ToString();
            Debug.Log("Slot event on output: " + valueStr + " " + type);
        }

        public VFXOutputSlot Slot { get { return m_Slot; } }
        private VFXOutputSlot m_Slot;
    }
}