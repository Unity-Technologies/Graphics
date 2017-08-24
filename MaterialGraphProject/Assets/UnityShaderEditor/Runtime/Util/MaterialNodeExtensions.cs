using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
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
