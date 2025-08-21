using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using static UnityEngine.Rendering.HighDefinition.HDMaterial;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using static UnityEditor.Rendering.HighDefinition.HDFields;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph

{
    class HDSixWaySubTarget : SurfaceSubTarget, IRequiresData<HDSixWayData>
    {
        public HDSixWaySubTarget() => displayName = "Six-way Smoke Lit";

        static readonly GUID kSubTargetSourceCodeGuid = new GUID("b20b7afb3a1f43afafc0ac6ea3f2cb26");  // HDSixWaySubTarget.cs

        static string[] passTemplateMaterialDirectories = new string[]
        {
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/SixWayLit/ShaderGraph/"
        };
        protected override string renderType => HDRenderTypeTags.HDLitShader.ToString();

        internal override MaterialResetter setupMaterialKeywordsAndPassFunc => ShaderGraphAPI.ValidateSixWayMaterial;
        protected override string customInspector => "Rendering.HighDefinition.SixWayGUI";

        protected override string[] templateMaterialDirectories => passTemplateMaterialDirectories;
        protected override FieldDescriptor subShaderField => new FieldDescriptor(kSubShader, "Six-way Lit Subshader", "");
        protected override string subShaderInclude => CoreIncludes.kSixWayLit;
        protected override ShaderID shaderID => ShaderID.SG_SixWay;
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;

        protected override bool supportLighting => true;

        HDSixWayData m_SixWayData;
        HDSixWayData IRequiresData<HDSixWayData>.data
        {
            get => m_SixWayData;
            set => m_SixWayData = value;
        }
        public HDSixWayData sixWayData
        {
            get => m_SixWayData;
            set => m_SixWayData = value;
        }

        public override void Setup(ref TargetSetupContext context)
        {
            if (TargetsVFX())
            {
                var inspector = typeof(VFXShaderGraphGUISixWay).FullName;
                context.AddCustomEditorForRenderPipeline(inspector, typeof(HDRenderPipelineAsset));
            }
            base.Setup(ref context);
        }

        class SixWayShaderPasses
        {
            public static PassDescriptor GenerateForwardOnly(bool useVFX, bool useTessellation, bool useColorAbsorption)
            {
                FieldCollection requiredFields = new FieldCollection();
                requiredFields.Add(CoreRequiredFields.BasicLighting);
                requiredFields.Add(SixWayStructs.RequiredFields);

                DefineCollection defines = HDShaderPasses.GenerateDefines(CoreDefines.Forward, useVFX, useTessellation);

                if (useColorAbsorption)
                {
                    requiredFields.Add(BlockFields.SurfaceDescription.AbsorptionStrength);
                }

                return new PassDescriptor
                {
                    // Definition
                    displayName = "ForwardOnly",
                    referenceName = "SHADERPASS_FORWARD",
                    lightMode = "ForwardOnly",
                    useInPreview = true,

                    // Collections
                    structs = GenerateStructs(useVFX, useTessellation),
                    // We need motion vector version as Forward pass support transparent motion vector and we can't use ifdef for it
                    requiredFields = requiredFields,
                    renderStates = CoreRenderStates.Forward,
                    pragmas = HDShaderPasses.GeneratePragmas(CorePragmas.DotsInstanced, useVFX, useTessellation),
                    defines = defines,
                    includes = GenerateIncludes(),

                    virtualTextureFeedback = true,
                    customInterpolators = CoreCustomInterpolators.Common
                };

                IncludeCollection GenerateIncludes()
                {
                    var includes = new IncludeCollection();

                    includes.Add(CoreIncludes.CorePregraph);

                    includes.Add(CoreIncludes.kLighting, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph);

                    includes.Add(CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph);

                    includes.Add(CoreIncludes.kLightLoop, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.CoreUtility);

                    includes.Add(CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph);
                    includes.Add(CoreIncludes.kPassForward, IncludeLocation.Postgraph);

                    return includes;
                }

                StructCollection GenerateStructs(bool useVFX, bool useTessellation)
                {
                    StructCollection structs = useTessellation && !useVFX
                        ? SixWayStructs.SixWayBasicTesselation
                        : SixWayStructs.SixWayBasic;
                    return structs;
                }
            }
        }

        static class SixWayStructs
        {
            internal static StructCollection SixWayBasic = new StructCollection()
            {
                { HDStructs.AttributesMesh },
                { AddSixWayVaryings(HDStructs.VaryingsMeshToPS) },
                { Structs.VertexDescriptionInputs },
                { Structs.SurfaceDescriptionInputs },
            };

            internal static StructCollection SixWayBasicTesselation = new StructCollection()
            {
                { HDStructs.AttributesMesh },
                { AddSixWayVaryings(HDStructs.VaryingsMeshToDS) },
                { AddSixWayVaryings(HDStructs.VaryingsMeshToPS) },
                { Structs.VertexDescriptionInputs },
                { Structs.SurfaceDescriptionInputs },
            };

            static StructDescriptor AddSixWayVaryings(StructDescriptor defaultVaryings)
            {
                StructDescriptor newVaryings = defaultVaryings;
                var newFields = new FieldDescriptor[defaultVaryings.fields.Length + SixWayVaryings.AllVaryings.Length];
                SixWayVaryings.AllVaryings.CopyTo(newFields, 0);
                defaultVaryings.fields.CopyTo(newFields, SixWayVaryings.AllVaryings.Length);
                newVaryings.fields = newFields;
                return newVaryings;
            }

            public static FieldCollection RequiredFields = new FieldCollection()
            {
                StructFields.SurfaceDescriptionInputs.FaceSign,
                SixWayVaryings.diffuseGIData0,
                SixWayVaryings.diffuseGIData1,
                SixWayVaryings.diffuseGIData2,
            };
        }

        public struct SixWayVaryings
        {
            public static string name = "Varyings";

            public static FieldDescriptor diffuseGIData0 = new FieldDescriptor(SixWayVaryings.name, "diffuseGIData0",
                "VARYINGS_NEED_SIX_WAY_DIFFUSE_GI_DATA", ShaderValueType.Float4 ,subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor diffuseGIData1 = new FieldDescriptor(SixWayVaryings.name, "diffuseGIData1",
                "VARYINGS_NEED_SIX_WAY_DIFFUSE_GI_DATA", ShaderValueType.Float4 ,subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor diffuseGIData2 = new FieldDescriptor(SixWayVaryings.name, "diffuseGIData2",
                "VARYINGS_NEED_SIX_WAY_DIFFUSE_GI_DATA", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor[] AllVaryings = new FieldDescriptor[]
                { diffuseGIData0, diffuseGIData1, diffuseGIData2 };
        }

        public struct SixWayFragInputs
        {
            public static string name = "FragInputs";
            public static FieldDescriptor diffuseGIData0 = new FieldDescriptor(SixWayFragInputs.name, "diffuseGIData0",
                "VARYINGS_NEED_SIX_WAY_DIFFUSE_GI_DATA", ShaderValueType.Float4 ,subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor diffuseGIData1 = new FieldDescriptor(SixWayFragInputs.name, "diffuseGIData1",
                "VARYINGS_NEED_SIX_WAY_DIFFUSE_GI_DATA", ShaderValueType.Float4 ,subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor diffuseGIData2 = new FieldDescriptor(SixWayFragInputs.name, "diffuseGIData2",
                "VARYINGS_NEED_SIX_WAY_DIFFUSE_GI_DATA", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Optional);

            public static FieldDescriptor[] AllFragInputs = new FieldDescriptor[]
                { StructFields.Varyings.cullFace, diffuseGIData0, diffuseGIData1, diffuseGIData2 };
        }


        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);
            context.AddBlock(BlockFields.SurfaceDescription.MapRightTopBack);
            context.AddBlock(BlockFields.SurfaceDescription.MapLeftBottomFront);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);

            if(sixWayData.useColorAbsorption)
                context.AddBlock(BlockFields.SurfaceDescription.AbsorptionStrength);
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new SixWaySurfaceOptionPropertyBlock(sixWayData));

            blockList.AddPropertyBlock(new AdvancedOptionsPropertyBlock());
        }

        protected override int ComputeMaterialNeedsUpdateHash()
        {
            int hash = base.ComputeMaterialNeedsUpdateHash();

            unchecked
            {
                hash = hash * 23 + builtinData.alphaTestShadow.GetHashCode();
            }

            return hash;
        }
        protected override SubShaderDescriptor GetSubShaderDescriptor()
        {
            var descriptor = base.GetSubShaderDescriptor();
            descriptor.passes.Add(HDShaderPasses.GenerateDepthForwardOnlyPass(supportLighting, TargetsVFX(), systemData.tessellation));
            descriptor.passes.Add(SixWayShaderPasses.GenerateForwardOnly(TargetsVFX(), systemData.tessellation, sixWayData.useColorAbsorption));
            return descriptor;
        }

        protected override void CollectPassKeywords(ref PassDescriptor pass)
        {
            base.CollectPassKeywords(ref pass);
            if (pass.IsLightingOrMaterial())
            {
                pass.keywords.Add(SixWayKeywords.ProbeVolumes);
            }

            if (pass.IsForward())
            {
                pass.keywords.Add(CoreKeywordDescriptors.PunctualShadow);
                pass.keywords.Add(CoreKeywordDescriptors.DirectionalShadow);
                pass.keywords.Add(CoreKeywordDescriptors.AreaShadow);
                pass.keywords.Add(CoreKeywordDescriptors.ScreenSpaceShadow);
                pass.keywords.Add(CoreKeywordDescriptors.LightList);
                pass.keywords.Add(SixWayKeywords.ReceiveShadowsOff);
                if(sixWayData.useColorAbsorption)
                    pass.keywords.Add(SixWayKeywords.UseColorAbsorption);
            }
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);
            if (context.pass.IsForward())
            {
                foreach (var fragInput in SixWayFragInputs.AllFragInputs)
                    context.AddField(fragInput);
            }
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);

            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = false,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.UnityPerMaterial,
                overrideReferenceName = kEnableBlendModePreserveSpecularLighting,
            });

            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = sixWayData.receiveShadows,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = kReceiveShadows,
            });

            if(sixWayData.useColorAbsorption)
                collector.AddShaderProperty(new BooleanShaderProperty
                {
                    value = sixWayData.useColorAbsorption,
                    hidden = true,
                    overrideHLSLDeclaration = true,
                    hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                    overrideReferenceName = kUseColorAbsorption,
                });
        }

        static class SixWayKeywords
        {
            public static readonly KeywordDescriptor ReceiveShadowsOff = new KeywordDescriptor()
            {
                displayName = "Receive Shadows Off",
                referenceName = "_RECEIVE_SHADOWS_OFF",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.Fragment,
            };

            public static readonly KeywordDescriptor UseColorAbsorption = new KeywordDescriptor()
            {
                displayName = "Use Color Absorption",
                referenceName = "_SIX_WAY_COLOR_ABSORPTION",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.Fragment,
            };

            //Probes evaluated in vertex
            public static KeywordDescriptor ProbeVolumes = new KeywordDescriptor()
            {
                displayName = "ProbeVolumes",
                referenceName = "PROBE_VOLUMES",
                type = KeywordType.Enum,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
                entries = new KeywordEntry[]
                {
                    new KeywordEntry() { displayName = "Off", referenceName = "" },
                    new KeywordEntry() { displayName = "L1", referenceName = "L1" },
                    new KeywordEntry() { displayName = "L2", referenceName = "L2" },
                },
                stages = KeywordShaderStage.Vertex | KeywordShaderStage.Domain,
            };
        }

    }
}
