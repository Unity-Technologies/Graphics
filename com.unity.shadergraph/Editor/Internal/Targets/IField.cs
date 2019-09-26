using System.Diagnostics;

namespace UnityEditor.ShaderGraph.Internal
{
    public interface IField
    {
        string tag { get; }
        string name { get; }
        string define { get; }
    }
}
