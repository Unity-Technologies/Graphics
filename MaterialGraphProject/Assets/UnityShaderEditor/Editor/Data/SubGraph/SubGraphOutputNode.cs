using System;
using System.Linq;
using System.Reflection;
using UnityEditor.MaterialGraph.Drawing.Controls;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public class SubGraphOutputNode : AbstractSubGraphIONode
    {
        public SubGraphOutputNode()
        {
            name = "SubGraphOutputs";
        }

        [SubGraphOutputControl]
        int controlDummy { get; set; }

        public override int AddSlot()
        {
            var index = GetInputSlots<ISlot>().Count() + 1;
            AddSlot(new MaterialSlot(index, "Output " + index, "Output" + index, SlotType.Input, SlotValueType.Vector4, Vector4.zero));
            return index;
        }

        public override void RemoveSlot()
        {
            var index = GetInputSlots<ISlot>().Count();
            if (index == 0)
                return;

            RemoveSlot(index);
        }

        public override bool allowedInRemapGraph { get { return false; } }
    }

    public class SubGraphOutputControlAttribute : Attribute, IControlAttribute
    {
        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            if (!(node is AbstractSubGraphIONode))
                throw new ArgumentException("Node must inherit from AbstractSubGraphIONode.", "node");
            return new SubGraphOutputControlView((AbstractSubGraphIONode)node);
        }
    }

    public class SubGraphOutputControlView : VisualElement
    {
        AbstractSubGraphIONode m_Node;

        public SubGraphOutputControlView(AbstractSubGraphIONode node)
        {
            m_Node = node;
            Add(new Button(OnAdd) { text = "Add Slot" });
            Add(new Button(OnRemove) { text = "Remove Slot" });
        }

        void OnAdd()
        {
            m_Node.AddSlot();
        }

        void OnRemove()
        {
            m_Node.RemoveSlot();
        }
    }
}
