using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

using static UnityEditor.Rendering.HighDefinition.HDFields;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class PBRSkyData : HDTargetData
    {
        public NormalDropOffSpace normalDropOffSpace = NormalDropOffSpace.Tangent;
        public bool groundShading = false;
        public bool debugSymbols = false;
    }

    sealed class PBRSkySubTarget : SubTarget<HDTarget>, IRequiresData<PBRSkyData>
    {
        class Styles
        {
            public static GUIContent groundShadingText = new GUIContent("Ground Shading", "When enabled, HDRP uses more controls for the shading of the planet.");
        }

        public PBRSkySubTarget() => displayName = "Physically Based Sky";

        static readonly string kTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}/Editor/Material/PBRSky/ShaderGraph/";
        static readonly string kTemplatePath = $"{kTemplateDirectory}/ShaderPass.template";

        static readonly GUID kSubTargetSourceCodeGuid = new GUID("f7d870597a428d646b08e894b1d9b741");  // PBRSkySubTarget.cs
        static readonly string[] kSharedTemplateDirectories = new string[] { GenerationUtils.GetDefaultSharedTemplateDirectories()[0], kTemplateDirectory };

        static readonly string kBSDF = $"{HDUtils.GetCorePath()}/ShaderLibrary/BSDF.hlsl";
        static readonly string kTexture = $"{HDUtils.GetCorePath()}/ShaderLibrary/Texture.hlsl";
        static readonly string kCommonMaterial = $"{HDUtils.GetCorePath()}/ShaderLibrary/CommonMaterial.hlsl";
        static readonly string kFunctions = "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl";
        static readonly string kShaderPass = $"{HDUtils.GetHDRenderPipelinePath()}/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl";
        static readonly string kPBRSkyRendering = $"{HDUtils.GetHDRenderPipelinePath()}/Runtime/Sky/PhysicallyBasedSky/PhysicallyBasedSkyRendering.hlsl";
        static readonly string kPBRSkyEvaluation = $"{HDUtils.GetHDRenderPipelinePath()}/Runtime/Sky/PhysicallyBasedSky/PhysicallyBasedSkyEvaluation.hlsl";

        PBRSkyData m_SkyData;

        PBRSkyData IRequiresData<PBRSkyData>.data
        {
            get => m_SkyData;
            set => m_SkyData = value;
        }

        public PBRSkyData skyData
        {
            get => m_SkyData;
            set => m_SkyData = value;
        }

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSubTargetSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            context.AddSubShader(GenerateSubShader());
        }

        public override bool IsActive() => true;

        public override void GetFields(ref TargetFieldContext context)
        {
            context.AddField(Fields.GraphPixel);
        }


        public struct SurfaceDescriptionInputs
        {
            public static string name = "SurfaceDescriptionInputs";
            public static FieldDescriptor renderSunDisk = new FieldDescriptor(name, "renderSunDisk", "", ShaderValueType.Boolean, subscriptOptions: StructFieldOptions.Static);
            public static FieldDescriptor radiance = new FieldDescriptor(name, "radiance", "", ShaderValueType.Float3, subscriptOptions: StructFieldOptions.Static);
            public static FieldDescriptor intersectAtmosphere = new FieldDescriptor(name, "intersectAtmosphere", "", ShaderValueType.Boolean, subscriptOptions: StructFieldOptions.Static);
            public static FieldDescriptor tFrag = new FieldDescriptor(name, "tFrag", "", ShaderValueType.Float, subscriptOptions: StructFieldOptions.Static);
            public static FieldDescriptor tGround = new FieldDescriptor(name, "tGround", "", ShaderValueType.Float, subscriptOptions: StructFieldOptions.Static);
            public static FieldDescriptor hitGround = new FieldDescriptor(name, "hitGround", "", ShaderValueType.Boolean, subscriptOptions: StructFieldOptions.Static);
        }

        [GenerateBlocks]
        public struct PBRSkyBlocks
        {
            // Water specific block descriptors
            public static BlockFieldDescriptor GroundColor = new BlockFieldDescriptor(kMaterial, "GroundColor", "Ground Color", "", new ColorControl(Color.white, false), ShaderStage.Fragment);
            public static BlockFieldDescriptor GroundEmission = new BlockFieldDescriptor(kMaterial, "GroundEmission", "Ground Emission", "", new ColorControl(Color.black, true), ShaderStage.Fragment);
            public static BlockFieldDescriptor GroundNormalOS = new BlockFieldDescriptor(kMaterial, "GroundNormalOS", "Ground Normal (Object Space)", "", new NormalControl(CoordinateSpace.Object), ShaderStage.Fragment);
            public static BlockFieldDescriptor GroundNormalTS = new BlockFieldDescriptor(kMaterial, "GroundNormalTS", "Ground Normal (Tangent Space)", "", new NormalControl(CoordinateSpace.Tangent), ShaderStage.Fragment);
            public static BlockFieldDescriptor GroundNormalWS = new BlockFieldDescriptor(kMaterial, "GroundNormalWS", "Ground Normal (World Space)", "", new NormalControl(CoordinateSpace.World), ShaderStage.Fragment);
            public static BlockFieldDescriptor GroundSmoothness = new BlockFieldDescriptor(kMaterial, "GroundSmoothness", "Ground Smoothness", "", new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SpaceColor = new BlockFieldDescriptor(kMaterial, "SpaceColor", "Space Color", "", new ColorControl(Color.black, true), ShaderStage.Fragment);
        }

        public static KeywordDescriptor LocalSky = new KeywordDescriptor
        {
            displayName = "Local Sky",
            referenceName = "LOCAL_SKY",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            if (skyData.groundShading)
            {
                context.AddBlock(PBRSkyBlocks.GroundColor);
                context.AddBlock(PBRSkyBlocks.GroundEmission);
                context.AddBlock(PBRSkyBlocks.GroundNormalOS, skyData.normalDropOffSpace == NormalDropOffSpace.Object);
                context.AddBlock(PBRSkyBlocks.GroundNormalTS, skyData.normalDropOffSpace == NormalDropOffSpace.Tangent);
                context.AddBlock(PBRSkyBlocks.GroundNormalWS, skyData.normalDropOffSpace == NormalDropOffSpace.World);
                context.AddBlock(PBRSkyBlocks.GroundSmoothness);
            }

            context.AddBlock(PBRSkyBlocks.SpaceColor);
        }

        void AddProperty<F, T>(ref TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo, GUIContent style, BaseField<F> field, Func<T> getter, Action<T> setter)
        {
            field.value = (F)(object)getter();

            context.AddProperty(style.text, style.tooltip, 0, field, (evt) =>
            {
                if (Equals(getter(), evt.newValue))
                    return;

                registerUndo(style.text);
                setter((T)(object)evt.newValue);
                onChange();
            });
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            AddProperty(ref context, onChange, registerUndo, SurfaceOptionUIBlock.Styles.fragmentNormalSpace,
                new EnumField(skyData.normalDropOffSpace), () => skyData.normalDropOffSpace, (v) => { skyData.normalDropOffSpace = v; });

            AddProperty(ref context, onChange, registerUndo, Styles.groundShadingText,
                new Toggle(), () => skyData.groundShading, (v) => { skyData.groundShading = v; });

            if (Unsupported.IsDeveloperMode())
                AddProperty(ref context, onChange, registerUndo, AdvancedOptionsPropertyBlock.Styles.debugSymbolsText,
                    new Toggle(), () => skyData.debugSymbols, (v) => { skyData.debugSymbols = v; });
        }


        SubShaderDescriptor GenerateSubShader()
        {
            var result = new SubShaderDescriptor()
            {
                generatesPreview = false,
                passes = new PassCollection(),
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
            };

            result.passes.Add(GeneratePass(true));
            result.passes.Add(GeneratePass(false));

            return result;
        }

        public RenderStateCollection GetRenderState(bool cubemap)
        {
            return new RenderStateCollection()
            {
                RenderState.ZWrite(ZWrite.Off),
                RenderState.ZTest(cubemap ? ZTest.Always : ZTest.LEqual),
                RenderState.Cull(Cull.Off),
            };
        }

        public PassDescriptor GeneratePass(bool cubemap)
        {
            var fullscreenPass = new PassDescriptor
            {
                // Definition
                displayName = "PBR Sky" + (cubemap ? " cubemap" : ""),
                referenceName = "SHADERPASS_PBRSKY",
                useInPreview = false,

                // Template
                passTemplatePath = kTemplatePath,
                sharedTemplateDirectories = kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = null,
                validPixelBlocks = new BlockFieldDescriptor[]
                {
                    PBRSkyBlocks.GroundColor,
                    PBRSkyBlocks.GroundEmission,
                    PBRSkyBlocks.GroundNormalOS,
                    PBRSkyBlocks.GroundNormalTS,
                    PBRSkyBlocks.GroundNormalWS,
                    PBRSkyBlocks.GroundSmoothness,
                    PBRSkyBlocks.SpaceColor,
                },

                // Fields
                structs = new StructCollection
                {
                    new StructDescriptor() {
                        name = "Attributes",
                        packFields = false,
                        fields = new FieldDescriptor[]
                        {
                            StructFields.Attributes.vertexID,
                            StructFields.Attributes.instanceID,
                        }
                    },

                    new StructDescriptor()
                    {
                        name = "SurfaceDescriptionInputs",
                        packFields = false,
                        populateWithCustomInterpolators = true,
                        fields = new FieldDescriptor[]
                        {
                            StructFields.SurfaceDescriptionInputs.WorldSpaceViewDirection,
                            StructFields.SurfaceDescriptionInputs.WorldSpacePosition,
                            StructFields.SurfaceDescriptionInputs.TangentSpaceNormal,
                            StructFields.SurfaceDescriptionInputs.ObjectSpaceNormal,
                            StructFields.SurfaceDescriptionInputs.WorldSpaceNormal,
                            StructFields.SurfaceDescriptionInputs.TimeParameters,

                            SurfaceDescriptionInputs.renderSunDisk,
                            SurfaceDescriptionInputs.radiance,
                            SurfaceDescriptionInputs.intersectAtmosphere,
                            SurfaceDescriptionInputs.tFrag,
                            SurfaceDescriptionInputs.tGround,
                            SurfaceDescriptionInputs.hitGround,
                        }
                    },

                    new StructDescriptor()
                    {
                        name = "Varyings",
                        packFields = true,
                        populateWithCustomInterpolators = false,
                        fields = new FieldDescriptor[]
                        {
                            StructFields.Varyings.positionCS,
                            StructFields.Varyings.instanceID,
                            StructFields.Varyings.stereoTargetEyeIndexAsBlendIdx0,
                            StructFields.Varyings.stereoTargetEyeIndexAsRTArrayIdx,
                        }
                    },
                },
                fieldDependencies = FieldDependencies.Default,
                requiredFields = new FieldCollection
                {
                    StructFields.SurfaceDescriptionInputs.WorldSpaceViewDirection,
                    StructFields.SurfaceDescriptionInputs.WorldSpacePosition,
                    StructFields.SurfaceDescriptionInputs.WorldSpaceNormal,
                    StructFields.Attributes.vertexID,
                },

                keywords = new KeywordCollection { { LocalSky } },
                renderStates = GetRenderState(cubemap),
                pragmas = new PragmaCollection
                {
                    { Pragma.Target(ShaderModel.Target45) },
                    { Pragma.Vertex("Vert") },
                    { Pragma.Fragment(cubemap ? "FragBaking" : "Frag") },
                    { Pragma.EditorSyncCompilation },
                },
                includes = new IncludeCollection
                {
                    { kBSDF, IncludeLocation.Pregraph },
                    { kTexture, IncludeLocation.Pregraph },
                    { kCommonMaterial, IncludeLocation.Pregraph },
                    { kFunctions, IncludeLocation.Pregraph },
                    { kShaderPass, IncludeLocation.Pregraph },
                    { kPBRSkyRendering, IncludeLocation.Pregraph },
                    { kPBRSkyEvaluation, IncludeLocation.Pregraph },
                    { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
                },
            };

            if (skyData.debugSymbols)
                fullscreenPass.pragmas.Add(Pragma.DebugSymbols);

            return fullscreenPass;
        }


        class DoCreatePBRSkyShaderGraph : ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var material = GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineRuntimeMaterials>().pbrSkyMaterial;
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(material), pathName);
            }
        }

        [MenuItem("Assets/Create/Shader Graph/HDRP/PBR Sky Shader Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority + 7)]
        static void CreatePBRSkyGraph()
        {
            /*
            // Create an empty graph from scratch
            var target = (HDTarget)Activator.CreateInstance(typeof(HDTarget));
            target.TrySetActiveSubTarget(typeof(PBRSkySubTarget));

            var blockDescriptors = new[]
            {
                PBRSkyBlocks.SpaceColor,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
            */

            // Copy the default graph from the package
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreatePBRSkyShaderGraph>(), "PBR Sky Shader Graph.shadergraph", null, null);
        }
    }
}
