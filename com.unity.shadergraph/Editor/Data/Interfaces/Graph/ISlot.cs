using System;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Graphing
{
    interface ISlot : IEquatable<ISlot>
    {
        int id { get; }
        string displayName { get; set; }
        bool isInputSlot { get; }
        bool isOutputSlot { get; }
        int priority { get; set; }
        SlotReference slotReference { get; }
        AbstractMaterialNode owner { get; set; }
        bool hidden { get; set; }
    }
}
