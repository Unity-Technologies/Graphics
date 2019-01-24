using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequireTime
    {
        bool RequiresTime();
    }


    static class MayRequireTimeExtensions
    {
        public static bool RequiresTime(this AbstractMaterialNode node)
        {
            return node is IMayRequireTime mayRequireTime && mayRequireTime.RequiresTime();
        }
    }
}
