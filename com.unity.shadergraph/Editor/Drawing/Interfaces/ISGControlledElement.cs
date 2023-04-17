using System;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.Drawing
{
    interface ISGControlledElement
    {
        SGController controller
        {
            get;
        }

        void OnControllerChanged(ref SGControllerChangedEvent e);

        void OnControllerEvent(SGControllerEvent e);
    }

    interface ISGControlledElement<T> : ISGControlledElement where T : SGController
    {
        // This provides a way to access the controller of a ControlledElement at both the base class SGController level and child class level
        new T controller { get; }
    }
}
