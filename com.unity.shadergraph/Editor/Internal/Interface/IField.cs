using System.Diagnostics;

namespace UnityEditor.ShaderGraph.Internal
{
    public interface IField
    {
        string tag { get; }
        string name { get; }
        string define { get; }
    }

    public static class FieldUtilities
    {
        public static string ToFieldString(this IField field)
        {
            if(!string.IsNullOrEmpty(field.tag))
                return $"{field.tag}.{field.name}";
            else
                return field.name;
        }
    }    
}
