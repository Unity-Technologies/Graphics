using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Graphing
{
    public interface INode
    {
        IGraph owner { get; set; }
        Guid guid { get; }
        string name { get; set; }
        bool canDeleteNode { get; }
        Rect position { get; set; }
        IEnumerable<ISlot> inputSlots { get; }
        IEnumerable<ISlot> outputSlots { get; }
        IEnumerable<ISlot> slots { get; }
        void AddSlot(ISlot slot);
        void RemoveSlot(string name);
        SlotReference GetSlotReference(string name);
        ISlot FindSlot(string name);
        ISlot FindInputSlot(string name);
        ISlot FindOutputSlot(string name);
        IEnumerable<ISlot> GetInputsWithNoConnection();
    }
}
