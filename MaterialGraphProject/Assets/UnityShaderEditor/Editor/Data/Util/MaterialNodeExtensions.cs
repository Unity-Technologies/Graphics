using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public static class MaterialNodeExtensions
    {
        public static bool RequiresTime(this INode node)
        {
            var timeNode = node as IMayRequireTime;
            return timeNode != null && timeNode.RequiresTime();
        }
    }
}
