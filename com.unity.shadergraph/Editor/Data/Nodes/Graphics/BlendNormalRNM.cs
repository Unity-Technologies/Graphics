using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Graphics", "Normal", "BlendNormalRNM")]
    public class BlendNormalRNM : CodeFunctionNode
    {
        public BlendNormalRNM()
        {
            name = "BlendNormalRNM";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/BlendNormalRNM-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_BlendNormalRNM", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_BlendNormalRNM(
            [Slot(0, Binding.None)] Vector3 N0,
            [Slot(1, Binding.None)] Vector3 N1,
            [Slot(2, Binding.None)] out Vector3 Nf)
        {
			Nf = N0;
            return
                @"
				{
					real3 t = N0.xyz + real3(0.0, 0.0, 1.0);
					real3 u = N1.xyz * real3(-1.0, -1.0, 1.0);
					Nf = (t / t.z) * dot(t, u) - u;
				}
				";
        }
    }
}
