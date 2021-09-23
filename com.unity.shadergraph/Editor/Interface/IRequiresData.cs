using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph
{
    interface IRequiresData<T> where T : JsonObject
    {
        T data { get; set; }
    }
}
