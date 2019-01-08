using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequireTime
    {
        bool RequiresTime();
    }


    static class MayRequireTimeExtensions
    {
        public static bool RequiresTime(this INode node)
        {
            var mayRequireTime = node as IMayRequireTime;
            return mayRequireTime != null && mayRequireTime.RequiresTime();
        }
    }
}
