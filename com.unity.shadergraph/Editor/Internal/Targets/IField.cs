using System.Diagnostics;

namespace UnityEditor.ShaderGraph.Internal
{
    interface IField
    {
        string tag { get; }
        string name { get; }
        string define { get; }
    }
}
