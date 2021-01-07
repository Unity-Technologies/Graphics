using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.Drawing
{
    interface ISGControlledElement
    {
        void OnControllerChanged(ref SGControllerChangedEvent e);

        void OnControllerEvent(SGControllerEvent e);
    }

    interface ISGControlledElement<T> : ISGControlledElement where T : SGController
    {
        T controller { get; }
    }
}
