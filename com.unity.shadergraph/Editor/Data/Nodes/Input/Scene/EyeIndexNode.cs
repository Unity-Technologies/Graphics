using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Eye Index")]
    class EyeIndexNode : CodeFunctionNode
    {
        public override bool hasPreview { get { return false; } }

        public EyeIndexNode()
        {
            name = "Eye Index";
            synonyms = new string[] { "stereo", "3d" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("UnityGetEyeIndex", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string UnityGetEyeIndex([Slot(0, Binding.None)] out Vector1 Out)
        {
            return
@"
{
#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    Out = unity_StereoEyeIndex;
#else
    Out = 0;
#endif
}
";
        }
    }
}
