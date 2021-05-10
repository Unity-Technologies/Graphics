using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;


namespace UnityEditor.ShaderGraph
{
    [Title("UV", "Parallax Occlusion Mapping Sandbox")]
    class ParallaxOcclusionMappingSandboxNode : SandboxNode<ParallaxOcclusionMappingNodeDefinition>
    {
    }

    class ParallaxOcclusionMappingNodeDefinition : JsonObject, ISandboxNodeDefinition
    {
        public void BuildRuntime(ISandboxNodeBuildContext context)
        {
            context.SetName("Parallax Occlusion Mapping Sandbox");

            var shaderFunc = Unity_POM();

            context.SetMainFunction(shaderFunc);
            context.SetPreviewFunction(shaderFunc);
        }

        static StructTypeDefinition k_PomStruct = null;
        static StructTypeDefinition PomStructDefinition()
        {
            if (k_PomStruct == null)
            {
                var str = new StructTypeDefinition.Builder("POMStruct_$precision");
                str.AddMember(Types._precision2, "uv");
                str.AddMember(Types._UnityTexture2D, "tex");
                str.AddMember(Types._UnitySamplerState, "samplerState");
                k_PomStruct = str.Build();
            }
            return k_PomStruct;
        }

        static ShaderFunction Unity_POM()
        {
            var POMStruct = new SandboxType(PomStructDefinition());
            var POMGetHeight = Unity_POM_GetHeight(POMStruct);
            var POMOffset = Unity_POM_Offset(POMGetHeight, POMStruct);
            var GetDisplacementObjectScale = Unity_GetDisplacementObjectScale();

            var func = new ShaderFunction.Builder("Unity_POM_$precision");

            func.AddInput(Types._UnityTexture2D, "Heightmap");
            func.AddInput(Types._UnitySamplerState, "HeightmapSampler");
            func.AddInput(Types._precision, "Amplitude", 1.0f);
            func.AddInput(Types._precision, "Steps", 5.0f);
            func.AddInput(Types._precision2, "UVs", Binding.MeshUV0);
            func.AddInput(Types._precision, "Lod", 0.0f);
            func.AddInput(Types._precision, "LodThreshold");
            func.AddInput(Types._precision3, "TangentSpaceViewDirection", Binding.TangentSpaceViewDirection);       // TODO: hide!

            func.AddOutput(Types._precision, "PixelDepthOffset");
            func.AddOutput(Types._precision2, "ParallaxUVs");

            func.AddLine("float3 objectScale;");
            func.Call(GetDisplacementObjectScale).Add("objectScale").Dispose();
            func.AddLine("float3 ViewDir = TangentSpaceViewDirection * objectScale.xzy;");
            func.AddLine("float NdotV = ViewDir.z;");
            func.AddLine("float MaxHeight = 2 * 0.01;"); // cm in the interface so we multiply by 0.01 in the shader to convert in meter

            // Transform the view vector into the UV space.
            func.AddLine("float3 ViewDirUV = normalize(float3(ViewDir.xy * MaxHeight, ViewDir.z));"); // TODO: skip normalize

            // construct the state to pass down
            func.DeclareVariable(POMStruct, "POM");
            func.AddLine("POM.uv = UVs;");
            func.AddLine("POM.tex = Heightmap;");
            func.AddLine("POM.samplerState = HeightmapSampler;");

            func.AddLine("float OutHeight;");
            func.AddLine("float2 UV_offset;");
            func.Call(POMOffset)
                .Add("Lod")
                .Add("LodThreshold")
                .Add("Steps")
                .Add("ViewDirUV")
                .Add("POM")
                .Add("OutHeight")
                .Add("UV_offset").Dispose();

            func.AddLine("ParallaxUVs = UVs + UV_offset;");
            func.AddLine("PixelDepthOffset = (MaxHeight - OutHeight * MaxHeight) / max(NdotV, 0.0001);");

            return func.Build();
        }

        static ShaderFunction Unity_POM_GetHeight(SandboxType POMStruct)
        {
            var func = new ShaderFunction.Builder("Unity_POM_GetHeight_" + POMStruct.Name);
            func.AddInput(Types._precision2, "texOffsetCurrent");
            func.AddInput(Types._precision, "lod");
            func.AddInput(POMStruct, "pomStruct");

            func.AddOutput(Types._precision, "outHeight");

            func.AddLine("outHeight = SAMPLE_TEXTURE2D_LOD(pomStruct.tex, pomStruct.samplerState, pomStruct.uv + texOffsetCurrent, lod).r;");

            return func.Build();
        }

        static ShaderFunction Unity_POM_Offset(ShaderFunction GetHeight, SandboxType POMStruct)
        {
            var func = new ShaderFunction.Builder("Unity_POM_Offset_" + GetHeight.Name);

            // TODO: generic function parameters
            // ideally we actually want to define the generic parameter as a class that implements a GetHeight function..
            // so that it's just one generic parameter with a type restriction
            // but we would need interfaces or base class / inheritance to be able to express that
            // func.AddGenericFunctionParameter("$GetHeight$");

            // could we express the GetHeight function / POMStruct class as an INPUT parameter?

            // inputs
            func.AddInput(Types._precision, "lod");
            func.AddInput(Types._precision, "lodThreshold");
            func.AddInput(Types._int, "numSteps");
            func.AddInput(Types._precision3, "viewDirTS");
            func.AddInput(POMStruct, "pomStruct");

            // outputs
            func.AddOutput(Types._precision, "outHeight");
            func.AddOutput(Types._precision2, "uvOffset");

            // Convention: 1.0 is top, 0.0 is bottom - POM is always inward, no extrusion
            func.AddLine("real stepSize = 1.0 / (real) numSteps;");

            // View vector is from the point to the camera, but we want to raymarch from camera to point, so reverse the sign
            // The length of viewDirTS vector determines the furthest amount of displacement:
            // real parallaxLimit = -length(viewDirTS.xy) / viewDirTS.z;
            // real2 parallaxDir = normalize(Out.viewDirTS.xy);
            // real2 parallaxMaxOffsetTS = parallaxDir * parallaxLimit;
            // Above code simplify to
            func.AddLine("real2 parallaxMaxOffsetTS = (viewDirTS.xy / -viewDirTS.z);");
            func.AddLine("real2 texOffsetPerStep = stepSize * parallaxMaxOffsetTS;");

            // Do a first step before the loop to init all value correctly
            func.AddLine("real2 texOffsetCurrent = real2(0.0, 0.0);");
            func.AddLine("real prevHeight, currHeight;");
            func.Call(GetHeight, "texOffsetCurrent", "lod", "pomStruct", "prevHeight");
            func.AddLine("texOffsetCurrent += texOffsetPerStep;");
            func.Call(GetHeight, "texOffsetCurrent", "lod", "pomStruct", "currHeight");
            func.AddLine("real rayHeight = 1.0 - stepSize;"); // Start at top less one sample

            // Linear search
            func.AddLine("for (int stepIndex = 0; stepIndex < numSteps; ++stepIndex)");
            using (func.BlockScope())
            {
                // Have we found a height below our ray height ? then we have an intersection
                func.AddLine("if (currHeight > rayHeight) break;");
                func.AddLine("prevHeight = currHeight;");
                func.AddLine("rayHeight -= stepSize;");
                func.AddLine("texOffsetCurrent += texOffsetPerStep;");

                // Sample height map which in this case is stored in the alpha channel of the normal map:
                func.Call(GetHeight, "texOffsetCurrent", "lod", "pomStruct", "currHeight");
            }

            // Found below and above points, now perform line intersection (ray) with piecewise linear heightfield approximation

            // Refine the search with secant method
            func.AddLine("real pt0 = rayHeight + stepSize;");
            func.AddLine("real pt1 = rayHeight;");
            func.AddLine("real delta0 = pt0 - prevHeight;");
            func.AddLine("real delta1 = pt1 - currHeight;");
            func.AddLine("real delta;");
            func.AddLine("real2 offset;");

            // Secant method to affine the search
            // Ref: Faster Relief Mapping Using the Secant Method - Eric Risser
            func.AddLine("for (int i = 0; i < 3; ++i)");
            using (func.BlockScope())
            {
                // intersectionHeight is the height [0..1] for the intersection between view ray and heightfield line
                func.AddLine("real intersectionHeight = (pt0 * delta1 - pt1 * delta0) / (delta1 - delta0);");

                // Retrieve offset require to find this intersectionHeight
                func.AddLine("offset = (1 - intersectionHeight) * texOffsetPerStep * numSteps;");
                func.Call(GetHeight, "offset", "lod", "pomStruct", "currHeight");
                func.AddLine("delta = intersectionHeight - currHeight;");
                func.AddLine("if (abs(delta) <= 0.01) break;");

                // intersectionHeight < currHeight => new lower bounds
                func.AddLine("if (delta < 0.0)");
                using (func.BlockScope())
                {
                    func.AddLine("delta1 = delta;");
                    func.AddLine("pt1 = intersectionHeight;");
                }
                func.AddLine("else");
                using (func.BlockScope())
                {
                    func.AddLine("delta0 = delta;");
                    func.AddLine("pt0 = intersectionHeight;");
                }
            }

            func.AddLine("outHeight = currHeight;");

            // Fade the effect with lod (allow to avoid pop when switching to a discrete LOD mesh)
            func.AddLine("offset *= (1.0 - saturate(lod - lodThreshold));");
            func.AddLine("uvOffset = offset;");

            return func.Build();
        }

        static ShaderFunction Unity_GetDisplacementObjectScale()
        {
            var func = new ShaderFunction.Builder("Unity_GetDisplacementObjectScale_$precision");

            func.AddOutput(Types._precision3, "objectScale");

            func.AddLine("objectScale = float3(1.0, 1.0, 1.0);");
            func.AddLine("float4x4 worldTransform = GetWorldToObjectMatrix();");    // TODO: add a binding for the matrices
            func.AddLine("objectScale.x = length(float3(worldTransform._m00, worldTransform._m01, worldTransform._m02));");
            func.AddLine("objectScale.z = length(float3(worldTransform._m20, worldTransform._m21, worldTransform._m22));");

            return func.Build();
        }
    }
}
