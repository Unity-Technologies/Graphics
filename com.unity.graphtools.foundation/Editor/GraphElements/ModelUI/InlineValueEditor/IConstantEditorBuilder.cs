using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface used by constant editor builder extension methods to receive the data needed to build a constant editor.
    /// </summary>
    public interface IConstantEditorBuilder
    {
        Action<IChangeEvent> OnValueChanged { get; }
        Dispatcher CommandDispatcher { get; }
        bool ConstantIsLocked { get; }
        IPortModel PortModel { get; }
    }
}
