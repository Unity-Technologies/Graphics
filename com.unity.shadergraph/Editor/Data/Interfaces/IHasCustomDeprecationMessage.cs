using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    interface IHasCustomDeprecationMessage
    {
        public void GetCustomDeprecationMessage(out string deprecationString, out string buttonText, out string labelText, out MessageType messageType);
        public string GetCustomDeprecationLabel();
    }
}
