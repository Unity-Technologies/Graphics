using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Foundry
{
    sealed class FoundryTestTarget : Target
    {
        public override int latestVersion => 1;

        static readonly GUID kSourceCodeGuid = new GUID("4e0e689008cd7684a925a2949bee42c6"); // FoundryTestTarget.cs
        // Constants
        public static readonly string[] kSharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories().Union(new string[]
        {
            "Assets/CommonAssets/Editor/Targets/Templates"
        }).ToArray();
        public const string kUberTemplatePath = "Assets/CommonAssets/Editor/Targets/Templates/ShaderPass.template";

        // SubTarget
        List<SubTarget> m_SubTargets;
        List<string> m_SubTargetNames;

        [SerializeField]
        JsonData<SubTarget> m_ActiveSubTarget;

        public SubTarget activeSubTarget
        {
            get => m_ActiveSubTarget.value;
            set => m_ActiveSubTarget = value;
        }

        public FoundryTestTarget()
        {
            displayName = "FoundryTest";
            m_SubTargets = TargetUtils.GetSubTargets(this);
            m_SubTargetNames = m_SubTargets.Select(x => x.displayName).ToList();
            TargetUtils.ProcessSubTargetList(ref m_ActiveSubTarget, ref m_SubTargets);
        }

        public string renderType => $"{RenderType.Opaque}";
        public string renderQueue => $"{UnityEditor.ShaderGraph.RenderQueue.Geometry}";

        public override bool IsActive()
        {
            bool isFoundryRenderPipeline = GraphicsSettings.currentRenderPipeline is SimpleSRPFoundryPipeline;
            return isFoundryRenderPipeline && activeSubTarget.IsActive();
        }

        public override bool IsNodeAllowedByTarget(Type nodeType)
        {
            SRPFilterAttribute srpFilter = NodeClassCache.GetAttributeOnNodeType<SRPFilterAttribute>(nodeType);
            bool worksWithThisSrp = srpFilter == null || srpFilter.srpTypes.Contains(typeof(SimpleSRPFoundryPipeline));

            SubTargetFilterAttribute subTargetFilter = NodeClassCache.GetAttributeOnNodeType<SubTargetFilterAttribute>(nodeType);
            bool worksWithThisSubTarget = subTargetFilter == null || subTargetFilter.subTargetTypes.Contains(activeSubTarget.GetType());

            return worksWithThisSrp && worksWithThisSubTarget && base.IsNodeAllowedByTarget(nodeType);
        }

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);

            // Setup the active SubTarget
            TargetUtils.ProcessSubTargetList(ref m_ActiveSubTarget, ref m_SubTargets);
            m_ActiveSubTarget.value.target = this;
            m_ActiveSubTarget.value.Setup(ref context);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            var descs = context.blocks.Select(x => x.descriptor);
            // Core fields
            context.AddField(Fields.GraphVertex, descs.Contains(BlockFields.VertexDescription.Position) ||
                descs.Contains(BlockFields.VertexDescription.Normal) ||
                descs.Contains(BlockFields.VertexDescription.Tangent));
            context.AddField(Fields.GraphPixel);

            // SubTarget fields
            m_ActiveSubTarget.value.GetFields(ref context);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Core blocks
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.Tangent);
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);

            // SubTarget blocks
            m_ActiveSubTarget.value.GetActiveBlocks(ref context);
        }

        public override void ProcessPreviewMaterial(Material material)
        {
        }

        public override object saveContext => m_ActiveSubTarget.value?.saveContext;

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);
            activeSubTarget.CollectShaderProperties(collector, generationMode);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
        }

        public bool TrySetActiveSubTarget(Type subTargetType)
        {
            if (!subTargetType.IsSubclassOf(typeof(SubTarget)))
                return false;

            foreach (var subTarget in m_SubTargets)
            {
                if (subTarget.GetType().Equals(subTargetType))
                {
                    m_ActiveSubTarget = subTarget;
                    return true;
                }
            }

            return false;
        }

        public override bool WorksWithSRP(RenderPipelineAsset scriptableRenderPipeline)
        {
            return scriptableRenderPipeline?.GetType() == typeof(SimpleSRPFoundryPipeline);
        }
    }

    #region PortMasks
    class CoreBlockMasks
    {
        public static readonly BlockFieldDescriptor[] Vertex = new BlockFieldDescriptor[]
        {
            BlockFields.VertexDescription.Position,
            BlockFields.VertexDescription.Normal,
            BlockFields.VertexDescription.Tangent,
        };

        public static readonly BlockFieldDescriptor[] Fragment = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.BaseColor,
            BlockFields.SurfaceDescription.Alpha,
        };
    }
    #endregion

    #region StructCollections
    static class FoundryStructs
    {
        public static StructDescriptor Varyings = new StructDescriptor()
        {
            name = "Varyings",
            packFields = true,
            populateWithCustomInterpolators = true,
            fields = new FieldDescriptor[]
            {
                StructFields.Varyings.positionCS,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,
                StructFields.Varyings.texCoord0,
                StructFields.Varyings.texCoord1,
                StructFields.Varyings.texCoord2,
                StructFields.Varyings.texCoord3,
                StructFields.Varyings.color,
                StructFields.Varyings.screenPosition,
                StructFields.Varyings.instanceID,
                StructFields.Varyings.cullFace,
            }
        };
    }

    static class CoreStructCollections
    {
        public static readonly StructCollection Default = new StructCollection
        {
            { Structs.Attributes },
            { FoundryStructs.Varyings },
            { Structs.SurfaceDescriptionInputs },
            { Structs.VertexDescriptionInputs },
        };
    }
    #endregion

    #region FieldDependencies
    static class CoreFieldDependencies
    {
        public static readonly DependencyCollection Default = new DependencyCollection()
        {
            { FieldDependencies.Default },
        };
    }
    #endregion

    #region RenderStates
    static class CoreRenderStates
    {
        public static readonly RenderStateCollection Default = new RenderStateCollection
        {
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.Cull(Cull.Back) },
            { RenderState.Blend(Blend.One, Blend.Zero) },
        };
    }
    #endregion

    #region Pragmas
    static class CorePragmas
    {
        public static readonly PragmaCollection Forward = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target20) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };
    }
    #endregion

    #region Includes
    static class CoreIncludes
    {
        const string kColor = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl";
        const string kTexture = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl";
        const string kCore = "Assets/CommonAssets/Editor/Targets/Shaders/Core.hlsl";
        const string kVaryings = "Assets/CommonAssets/Editor/Targets/Shaders/Varyings.hlsl";
        const string kShaderPass = "Assets/CommonAssets/Editor/Targets/Shaders/ShaderPass.hlsl";
        const string kTextureStack = "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl";

        public static readonly IncludeCollection CorePregraph = new IncludeCollection
        {
            { kColor, IncludeLocation.Pregraph },
            { kTexture, IncludeLocation.Pregraph },
            { kCore, IncludeLocation.Pregraph },
            { kTextureStack, IncludeLocation.Pregraph },
        };

        public static readonly IncludeCollection CorePostgraph = new IncludeCollection
        {
            { kShaderPass, IncludeLocation.Pregraph },
            { kVaryings, IncludeLocation.Postgraph },
        };
    }
    #endregion

    #region KeywordDescriptors
    static class CoreKeywordDescriptors
    {
        public static readonly KeywordDescriptor DebugDisplay = new KeywordDescriptor()
        {
            displayName = "Debug Display",
            referenceName = "DEBUG_DISPLAY",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };
    }
    #endregion

    #region CustomInterpolators
    static class CoreCustomInterpDescriptors
    {
        public static readonly CustomInterpSubGen.Collection Common = new CustomInterpSubGen.Collection
        {
            // Custom interpolators are not explicitly defined in the SurfaceDescriptionInputs template.
            // This entry point will let us generate a block of pass-through assignments for each field.
            CustomInterpSubGen.Descriptor.MakeBlock(CustomInterpSubGen.Splice.k_spliceCopyToSDI, "output", "input"),

            // sgci_PassThroughFunc is called from BuildVaryings in Varyings.hlsl to copy custom interpolators from vertex descriptions.
            // this entry point allows for the function to be defined before it is used.
            CustomInterpSubGen.Descriptor.MakeFunc(CustomInterpSubGen.Splice.k_splicePreSurface, "CustomInterpolatorPassThroughFunc", "Varyings", "VertexDescription", "CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC", "FEATURES_GRAPH_VERTEX")
        };
    }
    #endregion
}
