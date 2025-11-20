using UnityEditor.AssetImporters;
using System.IO;

namespace UnityEditor.Rendering.UnifiedRayTracing
{
    [ScriptedImporter(1, "urtshader")]
    internal class UnifiedRTShaderImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
#if UNITY_6000_5_OR_NEWER
            string source = File.ReadAllText(FileUtil.PathToAbsolutePath(ctx.assetPath));
#else
            string source = File.ReadAllText(ctx.assetPath);
#endif

            var com = ShaderUtil.CreateComputeShaderAsset(ctx, computeShaderTemplate.Replace("SHADERCODE", source));
            var rt = ShaderUtil.CreateRayTracingShaderAsset(ctx,
                raytracingShaderTemplate.Replace("SHADERCODE", source));

            ctx.AddObjectToAsset("ComputeShader", com);
            ctx.AddObjectToAsset("RayTracingShader", rt);
            ctx.SetMainObject(com);
        }

        const string computeShaderTemplate =
            "#define UNIFIED_RT_BACKEND_COMPUTE\n" +
            "SHADERCODE\n" +
            "#include_with_pragmas \"Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Compute/ComputeRaygenShader.hlsl\"\n";

        const string raytracingShaderTemplate =
            "#define UNIFIED_RT_BACKEND_HARDWARE\n" +
            "SHADERCODE\n" +
            "#include_with_pragmas \"Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Hardware/HardwareRaygenShader.hlsl\"\n";
    }
}
