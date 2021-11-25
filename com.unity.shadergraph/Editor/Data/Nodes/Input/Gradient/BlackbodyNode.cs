using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Gradient", "Blackbody")]
    class BlackbodyNode : CodeFunctionNode
    {
        public BlackbodyNode()
        {
            name = "Blackbody";
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Blackbody", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Blackbody(
            [Slot(0, Binding.None, 512.0f, 512.0f, 512.0f, 512.0f)] Vector1 Temperature,
            [Slot(1, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.zero;
            return
@"
{
    //based on data by Mitchell Charity http://www.vendian.org/mncharity/dir3/blackbody/
    $precision3 color = $precision3(255.0, 255.0, 255.0);
    color.x = 56100000. * pow(Temperature,(-3.0 / 2.0)) + 148.0;
    color.y = 100.04 * log(Temperature) - 623.6;
    if (Temperature > 6500.0) color.y = 35200000.0 * pow(Temperature,(-3.0 / 2.0)) + 184.0;
    color.z = 194.18 * log(Temperature) - 1448.6;
    color = clamp(color, 0.0, 255.0)/255.0;
    if (Temperature < 1000.0) color *= Temperature/1000.0;
    Out = color;
}
";
        }
    }
}
