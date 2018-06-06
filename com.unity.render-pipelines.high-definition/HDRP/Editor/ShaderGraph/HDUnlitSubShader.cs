using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
//    [Serializable] ??
    public class HDUnlitSubShader : IUnlitSubShader
    {
        Pass m_PassDepthOnly = new Pass()
        {
            Name = "Depth prepass",
            LightMode = "DepthForwardOnly",
            TemplateName = "HDUnlitPassForward.template",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            ZWriteOverride = "ZWrite On",
            Includes = new List<string>()
            {
                "#include \"HDRP/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId
            }
        };

        Pass m_PassForward = new Pass()
        {
            Name = "Forward Unlit",
            LightMode = "ForwardOnly",
            TemplateName = "HDUnlitPassForward.template",
            ShaderPassName = "SHADERPASS_FORWARD_UNLIT",
            ExtraDefines = new List<string>()
            {
                "#pragma multi_compile _ DEBUG_DISPLAY"
            },
            Includes = new List<string>()
            {
                "#include \"HDRP/ShaderPass/ShaderPassForwardUnlit.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                UnlitMasterNode.ColorSlotId,
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId
            }
        };

        Pass m_PassMETA = new Pass()
        {
            Name = "META",
            LightMode = "Meta",
            TemplateName = "HDUnlitPassForward.template",
            ShaderPassName = "SHADERPASS_LIGHT_TRANSPORT",
            CullOverride = "Cull Off",
            Includes = new List<string>()
            {
                "#include \"HDRP/ShaderPass/ShaderPassLightTransport.hlsl\"",
            },
            RequiredFields = new List<string>()
            {
                "AttributesMesh.normalOS",
                "AttributesMesh.tangentOS",     // Always present as we require it also in case of anisotropic lighting
                "AttributesMesh.uv0",
                "AttributesMesh.uv1",
                "AttributesMesh.color",
                "AttributesMesh.uv2",           // SHADERPASS_LIGHT_TRANSPORT always uses uv2
            },
            PixelShaderSlots = new List<int>()
            {
                UnlitMasterNode.ColorSlotId,
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId
            }
        };

        Pass m_PassDistortion = new Pass()
        {
            Name = "Distortion",
            LightMode = "DistortionVectors",
            TemplateName = "HDUnlitPassForward.template",
            ShaderPassName = "SHADERPASS_DISTORTION",
            BlendOverride = "Blend One One, One One",   // [_DistortionSrcBlend] [_DistortionDstBlend], [_DistortionBlurSrcBlend] [_DistortionBlurDstBlend]
            BlendOpOverride = "BlendOp Add, Add",       // Add, [_DistortionBlurBlendOp]
            ZTestOverride = "ZTest LEqual",             // [_ZTestModeDistortion]
            ZWriteOverride = "ZWrite Off",
            Includes = new List<string>()
            {
                "#include \"HDRP/ShaderPass/ShaderPassDistortion.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            }
        };

        private static string GetVariantDefines(UnlitMasterNode masterNode)
        {
            ShaderGenerator defines = new ShaderGenerator();

            // #pragma shader_feature _ALPHATEST_ON
            float constantAlpha = 0.0f;
            if (masterNode.IsSlotConnected(PBRMasterNode.AlphaThresholdSlotId) ||
                (float.TryParse(masterNode.GetSlotValue(PBRMasterNode.AlphaThresholdSlotId, GenerationMode.ForReals), out constantAlpha) && (constantAlpha > 0.0f)))
            {
                defines.AddShaderChunk("#define _ALPHATEST_ON 1", true);
            }

//             if (kTesselationMode != TessellationMode.None)
//             {
//                 defines.AddShaderChunk("#define _TESSELLATION_PHONG 1", true);
//             }

            // #pragma shader_feature _ _VERTEX_DISPLACEMENT _PIXEL_DISPLACEMENT
//             switch (kDisplacementMode)
//             {
//                 case DisplacementMode.None:
//                     break;
//                 case DisplacementMode.Vertex:
//                     defines.AddShaderChunk("#define _VERTEX_DISPLACEMENT 1", true);
//                     break;
//                 case DisplacementMode.Pixel:
//                     defines.AddShaderChunk("#define _PIXEL_DISPLACEMENT 1", true);
            // Depth offset is only enabled if per pixel displacement is
//                     if (kDepthOffsetEnable)
//                     {
//                         // #pragma shader_feature _DEPTHOFFSET_ON
//                         defines.AddShaderChunk("#define _DEPTHOFFSET_ON 1", true);
//                     }
//                     break;
//                 case DisplacementMode.Tessellation:
//                     if (kTessellationEnabled)
//                     {
//                         defines.AddShaderChunk("#define _TESSELLATION_DISPLACEMENT 1", true);
//                     }
//                     break;
//             }

            // #pragma shader_feature _VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE
            // #pragma shader_feature _DISPLACEMENT_LOCK_TILING_SCALE
            // #pragma shader_feature _PIXEL_DISPLACEMENT_LOCK_OBJECT_SCALE
            // #pragma shader_feature _VERTEX_WIND
            // #pragma shader_feature _ _REFRACTION_PLANE _REFRACTION_SPHERE
            //
            // #pragma shader_feature _ _MAPPING_PLANAR _MAPPING_TRIPLANAR          // MOVE to a node
            // #pragma shader_feature _NORMALMAP_TANGENT_SPACE
            // #pragma shader_feature _ _REQUIRE_UV2 _REQUIRE_UV3

            // #pragma shader_feature _MASKMAP
            // #pragma shader_feature _BENTNORMALMAP
            // #pragma shader_feature _EMISSIVE_COLOR_MAP
            // #pragma shader_feature _ENABLESPECULAROCCLUSION
            // #pragma shader_feature _HEIGHTMAP
            // #pragma shader_feature _TANGENTMAP
            // #pragma shader_feature _ANISOTROPYMAP
            // #pragma shader_feature _DETAIL_MAP                                   // MOVE to a node
            // #pragma shader_feature _SUBSURFACE_RADIUS_MAP
            // #pragma shader_feature _THICKNESSMAP
            // #pragma shader_feature _SPECULARCOLORMAP
            // #pragma shader_feature _TRANSMITTANCECOLORMAP

            // Keywords for transparent
            // #pragma shader_feature _SURFACE_TYPE_TRANSPARENT
            if (masterNode.surfaceType != SurfaceType.Opaque)
            {
                // transparent-only defines
                defines.AddShaderChunk("#define _SURFACE_TYPE_TRANSPARENT 1", true);

                // #pragma shader_feature _ _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
                if (masterNode.alphaMode == AlphaMode.Alpha)
                {
                    defines.AddShaderChunk("#define _BLENDMODE_ALPHA 1", true);
                }
                else if (masterNode.alphaMode == AlphaMode.Additive)
                {
                    defines.AddShaderChunk("#define _BLENDMODE_ADD 1", true);
                }
//                else if (masterNode.alphaMode == PBRMasterNode.AlphaMode.PremultiplyAlpha)            // TODO
//                {
//                    defines.AddShaderChunk("#define _BLENDMODE_PRE_MULTIPLY 1", true);
//                }

                // #pragma shader_feature _BLENDMODE_PRESERVE_SPECULAR_LIGHTING
//                 if (kEnableBlendModePreserveSpecularLighting)
//                 {
//                     defines.AddShaderChunk("#define _BLENDMODE_PRESERVE_SPECULAR_LIGHTING 1", true);
//                 }

                // #pragma shader_feature _ENABLE_FOG_ON_TRANSPARENT
//                 if (kEnableFogOnTransparent)
//                 {
//                     defines.AddShaderChunk("#define _ENABLE_FOG_ON_TRANSPARENT 1", true);
//                 }
            }
            else
            {
                // opaque-only defines
            }

            // enable dithering LOD crossfade
            // #pragma multi_compile _ LOD_FADE_CROSSFADE
            // TODO: We should have this keyword only if VelocityInGBuffer is enable, how to do that ?
            //#pragma multi_compile VELOCITYOUTPUT_OFF VELOCITYOUTPUT_ON

            return defines.GetShaderString(2);
        }

        private static bool GenerateShaderPass(UnlitMasterNode masterNode, Pass pass, GenerationMode mode, SurfaceMaterialOptions materialOptions, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            var templateLocation = Path.Combine(Path.Combine(Path.Combine(HDEditorUtils.GetHDRenderPipelinePath(), "Editor"), "ShaderGraph"), pass.TemplateName);
            if (!File.Exists(templateLocation))
            {
                // TODO: produce error here
                return false;
            }

            sourceAssetDependencyPaths.Add(templateLocation);

            // grab all of the active nodes
            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, masterNode, NodeUtils.IncludeSelf.Include, pass.PixelShaderSlots);

            // graph requirements describe what the graph itself requires
            var graphRequirements = ShaderGraphRequirements.FromNodes(activeNodeList, ShaderStageCapability.All, true, true);

            ShaderStringBuilder graphNodeFunctions = new ShaderStringBuilder();
            graphNodeFunctions.IncreaseIndent();
            var functionRegistry = new FunctionRegistry(graphNodeFunctions);

            // Build the list of active slots based on what the pass requires
            // TODO: this can be a shared function -- From here through GraphUtil.GenerateSurfaceDescription(..)
            var activeSlots = new List<MaterialSlot>();
            foreach (var id in pass.PixelShaderSlots)
            {
                MaterialSlot slot = masterNode.FindSlot<MaterialSlot>(id);
                if (slot != null)
                {
                    activeSlots.Add(slot);
                }
            }

            // build the graph outputs structure to hold the results of each active slots (and fill out activeFields to indicate they are active)
            string graphInputStructName = "SurfaceDescriptionInputs";
            string graphOutputStructName = "SurfaceDescription";
            string graphEvalFunctionName = "SurfaceDescriptionFunction";
            var graphEvalFunction = new ShaderStringBuilder();
            var graphOutputs = new ShaderStringBuilder();
            PropertyCollector graphProperties = new PropertyCollector();

            // build the graph outputs structure, and populate activeFields with the fields of that structure
            HashSet<string> activeFields = new HashSet<string>();
            GraphUtil.GenerateSurfaceDescriptionStruct(graphOutputs, activeSlots, true);

            // Build the graph evaluation code, to evaluate the specified slots
            GraphUtil.GenerateSurfaceDescriptionFunction(
                activeNodeList,
                masterNode,
                masterNode.owner as AbstractMaterialGraph,
                graphEvalFunction,
                functionRegistry,
                graphProperties,
                graphRequirements,  // TODO : REMOVE UNUSED
                mode,
                graphEvalFunctionName,
                graphOutputStructName,
                null,
                activeSlots,
                graphInputStructName);

            var blendCode = new ShaderStringBuilder();
            var cullCode = new ShaderStringBuilder();
            var zTestCode = new ShaderStringBuilder();
            var zWriteCode = new ShaderStringBuilder();
            var stencilCode = new ShaderStringBuilder();
            var colorMaskCode = new ShaderStringBuilder();
            HDSubShaderUtilities.BuildRenderStatesFromPassAndMaterialOptions(pass, materialOptions, blendCode, cullCode, zTestCode, zWriteCode, stencilCode, colorMaskCode);

            if (masterNode.twoSided.isOn)
            {
                activeFields.Add("DoubleSided");
                if (pass.ShaderPassName != "SHADERPASS_VELOCITY")   // HACK to get around lack of a good interpolator dependency system
                {                                                   // we need to be able to build interpolators using multiple input structs
                                                                    // also: should only require isFrontFace if Normals are required...
                    activeFields.Add("DoubleSided.Mirror");         // TODO: change this depending on what kind of normal flip you want..
                    activeFields.Add("FragInputs.isFrontFace");     // will need this for determining normal flip mode
                }
            }

            if (pass.PixelShaderSlots != null)
            {
                foreach (var slotId in pass.PixelShaderSlots)
                {
                    var slot = masterNode.FindSlot<MaterialSlot>(slotId);
                    if (slot != null)
                    {
                        var rawSlotName = slot.RawDisplayName().ToString();
                        var descriptionVar = string.Format("{0}.{1}", graphOutputStructName, rawSlotName);
                        activeFields.Add(descriptionVar);
                    }
                }
            }

            var packedInterpolatorCode = new ShaderGenerator();
            var graphInputs = new ShaderGenerator();
            HDRPShaderStructs.Generate(
                packedInterpolatorCode,
                graphInputs,
                graphRequirements,
                pass.RequiredFields,
                CoordinateSpace.World,
                activeFields);

            // debug output all active fields
            var interpolatorDefines = new ShaderGenerator();
            {
                interpolatorDefines.AddShaderChunk("// ACTIVE FIELDS:");
                foreach (string f in activeFields)
                {
                    interpolatorDefines.AddShaderChunk("//   " + f);
                }
            }

            ShaderGenerator defines = new ShaderGenerator();
            {
                defines.AddShaderChunk(string.Format("#define SHADERPASS {0}", pass.ShaderPassName), true);
                if (pass.ExtraDefines != null)
                {
                    foreach (var define in pass.ExtraDefines)
                        defines.AddShaderChunk(define);
                }
                defines.AddGenerator(interpolatorDefines);
            }

            var shaderPassIncludes = new ShaderGenerator();
            if (pass.Includes != null)
            {
                foreach (var include in pass.Includes)
                    shaderPassIncludes.AddShaderChunk(include);
            }


            // build graph code
            var graph = new ShaderGenerator();
            graph.AddShaderChunk("// Graph Inputs");
            graph.Indent();
            graph.AddGenerator(graphInputs);
            graph.Deindent();
            graph.AddShaderChunk("// Graph Outputs");
            graph.Indent();
            graph.AddShaderChunk(graphOutputs.ToString());
            //graph.AddGenerator(graphOutputs);
            graph.Deindent();
            graph.AddShaderChunk("// Graph Properties (uniform inputs)");
            graph.AddShaderChunk(graphProperties.GetPropertiesDeclaration(1));
            graph.AddShaderChunk("// Graph Node Functions");
            graph.AddShaderChunk(graphNodeFunctions.ToString());
            graph.AddShaderChunk("// Graph Evaluation");
            graph.Indent();
            graph.AddShaderChunk(graphEvalFunction.ToString());
            //graph.AddGenerator(graphEvalFunction);
            graph.Deindent();

            // build the hash table of all named fragments      TODO: could make this Dictionary<string, ShaderGenerator / string>  ?
            Dictionary<string, string> namedFragments = new Dictionary<string, string>();
            namedFragments.Add("${Defines}",                defines.GetShaderString(2, false));
            namedFragments.Add("${Graph}",                  graph.GetShaderString(2, false));
            namedFragments.Add("${LightMode}",              pass.LightMode);
            namedFragments.Add("${PassName}",               pass.Name);
            namedFragments.Add("${Includes}",               shaderPassIncludes.GetShaderString(2, false));
            namedFragments.Add("${InterpolatorPacking}",    packedInterpolatorCode.GetShaderString(2, false));
            namedFragments.Add("${Blending}",               blendCode.ToString());
            namedFragments.Add("${Culling}",                cullCode.ToString());
            namedFragments.Add("${ZTest}",                  zTestCode.ToString());
            namedFragments.Add("${ZWrite}",                 zWriteCode.ToString());
            namedFragments.Add("${Stencil}",                stencilCode.ToString());
            namedFragments.Add("${ColorMask}",              colorMaskCode.ToString());
            namedFragments.Add("${LOD}",                    materialOptions.lod.ToString());
            namedFragments.Add("${VariantDefines}",         GetVariantDefines(masterNode));

            // process the template to generate the shader code for this pass   TODO: could make this a shared function
            string[] templateLines = File.ReadAllLines(templateLocation);
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            foreach (string line in templateLines)
            {
                ShaderSpliceUtil.PreprocessShaderCode(line, activeFields, namedFragments, builder);
                builder.AppendLine();
            }

            result.AddShaderChunk(builder.ToString(), false);

            return true;
        }

        public string GetSubshader(IMasterNode inMasterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // HDUnlitSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("292c6a3c80161fa4cb49a9d11d35cbe9"));
                // HDSubShaderUtilities.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("713ced4e6eef4a44799a4dd59041484b"));
            }

            var masterNode = inMasterNode as UnlitMasterNode;
            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                SurfaceMaterialOptions materialOptions = HDSubShaderUtilities.BuildMaterialOptions(masterNode.surfaceType, masterNode.alphaMode, masterNode.twoSided.isOn);

                // Add tags at the SubShader level
                {
                    var tagsVisitor = new ShaderStringBuilder();
                    materialOptions.GetTags(tagsVisitor);
                    subShader.AddShaderChunk(tagsVisitor.ToString(), false);
                }

                // generate the necessary shader passes
//                bool opaque = (masterNode.surfaceType == SurfaceType.Opaque);
//                bool transparent = (masterNode.surfaceType != SurfaceType.Opaque);
                bool distortionActive = false;

                GenerateShaderPass(masterNode, m_PassDepthOnly, mode, materialOptions, subShader, sourceAssetDependencyPaths);
                GenerateShaderPass(masterNode, m_PassForward, mode, materialOptions, subShader, sourceAssetDependencyPaths);
                GenerateShaderPass(masterNode, m_PassMETA, mode, materialOptions, subShader, sourceAssetDependencyPaths);
                if (distortionActive)
                {
                    GenerateShaderPass(masterNode, m_PassDistortion, mode, materialOptions, subShader, sourceAssetDependencyPaths);
                }
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            return subShader.GetShaderString(0);
        }
    }
}
