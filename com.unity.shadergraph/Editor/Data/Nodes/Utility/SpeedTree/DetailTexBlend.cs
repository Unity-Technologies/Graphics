using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{

    [Title("Utility", "SpeedTree", "DetailTexBlend")]
    class DetailTexNode : CodeFunctionNode
    {
        public DetailTexNode()
        {
            name = "Detail Tex Blend";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("SpeedTree_DetailTex", BindingFlags.Static | BindingFlags.NonPublic);
        }

        // This is actually pretty simple and could be done within the graph itself with
        // existing nodes.  But the problem is making it dependent on the geometry type, which
        // either requires doing it in the node, creating a modal predicate node, or just
        // writing it into the template.
        // Doing it in the template means we also can't do Hue Variation in a node because it
        // would have to come after, which means it needs to be in the template as well.
        // So since we need a node anyway, might as well do it straight out.
        static string SpeedTree_DetailTex(
    [Slot(0, Binding.None, 0.0f)] Vector1 Discriminant,
    [Slot(1, Binding.None, 0.2f, 0.2f, 0.2f, 1f)] ColorRGBA BaseColor,
    [Slot(2, Binding.None, 0.0f, 0.0f, 0.0f, 0.0f)] ColorRGBA DetailColor,
    [Slot(3, Binding.None, ShaderStageCapability.Fragment)] out ColorRGBA Out)
        {
            Out = BaseColor;
            return
                @"
{
#ifdef GEOM_TYPE_BRANCH_DETAIL
    Out.rgb = lerp(BaseColor.rgb, DetailColor.rgb, Discriminant < 2.0 ? saturate(Discriminant) : DetailColor.a);
    Out.a = BaseColor.a;
#else
    Out = BaseColor;
#endif
}";
        }
    }
}
