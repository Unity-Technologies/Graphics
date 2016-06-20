using System;
using System.Collections.Generic;

namespace UnityEngine.Graphing
{
    public interface INode
    {
        IGraph owner { get; set; }
        Guid guid { get; }
        string name { get; set; }
        bool canDeleteNode { get; }
        IEnumerable<T> GetInputSlots<T>() where T : ISlot;
        IEnumerable<T> GetOutputSlots<T>() where T : ISlot;
        IEnumerable<T> GetSlots<T>() where T : ISlot;
        void AddSlot(ISlot slot);
        void RemoveSlot(string name);
        SlotReference GetSlotReference(string name);
        T FindSlot<T>(string name) where T : ISlot;
        T FindInputSlot<T>(string name) where T : ISlot;
        T FindOutputSlot<T>(string name) where T : ISlot;
        IEnumerable<ISlot> GetInputsWithNoConnection();
        DrawingData drawState { get; set; }
        bool hasError { get; }
        void ValidateNode();
        void UpdateNodeAfterDeserialization();
    }
}
