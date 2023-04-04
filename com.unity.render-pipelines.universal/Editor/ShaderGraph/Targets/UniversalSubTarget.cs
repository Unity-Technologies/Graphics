using UnityEngine;
using UnityEditor.ShaderGraph;
using static Unity.Rendering.Universal.ShaderUtils;
using UnityEditor.ShaderGraph.Internal;
#if HAS_VFX_GRAPH
using UnityEditor.VFX;
#endif

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    abstract class UniversalSubTarget : SubTarget<UniversalTarget>, IHasMetadata
#if HAS_VFX_GRAPH
        , IRequireVFXContext
#endif
    {
        static readonly GUID kSourceCodeGuid = new GUID("92228d45c1ff66740bfa9e6d97f7e280");  // UniversalSubTarget.cs

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
        }

        protected abstract ShaderID shaderID { get; }

#if HAS_VFX_GRAPH
        // VFX Properties
        VFXContext m_ContextVFX = null;
        VFXTaskCompiledData m_TaskDataVFX;
        protected bool TargetsVFX() => m_ContextVFX != null;

        public void ConfigureContextData(VFXContext context, VFXTaskCompiledData data)
        {
            m_ContextVFX = context;
            m_TaskDataVFX = data;
        }

#endif

        protected SubShaderDescriptor PostProcessSubShader(SubShaderDescriptor subShaderDescriptor)
        {
#if HAS_VFX_GRAPH
            if (TargetsVFX())
                return VFXSubTarget.PostProcessSubShader(subShaderDescriptor, m_ContextVFX, m_TaskDataVFX);
#endif
            return subShaderDescriptor;
        }

        public override void GetFields(ref TargetFieldContext context)
        {
#if HAS_VFX_GRAPH
            if (TargetsVFX())
                VFXSubTarget.GetFields(ref context, m_ContextVFX);
#endif
        }

        public virtual string identifier => GetType().Name;
        public virtual ScriptableObject GetMetadataObject(GraphDataReadOnly graphData)
        {
            var urpMetadata = ScriptableObject.CreateInstance<UniversalMetadata>();
            urpMetadata.shaderID = shaderID;
            urpMetadata.allowMaterialOverride = target.allowMaterialOverride;
            urpMetadata.surfaceType = target.surfaceType;
            urpMetadata.alphaMode = target.alphaMode;
            urpMetadata.castShadows = target.castShadows;
            return urpMetadata;
        }

        private int lastMaterialNeedsUpdateHash = 0;
        protected virtual int ComputeMaterialNeedsUpdateHash() => 0;

        public override object saveContext
        {
            get
            {
                int hash = ComputeMaterialNeedsUpdateHash();
                bool needsUpdate = hash != lastMaterialNeedsUpdateHash;
                if (needsUpdate)
                    lastMaterialNeedsUpdateHash = hash;

                return new UniversalShaderGraphSaveContext { updateMaterials = needsUpdate };
            }
        }
    }

    internal static class SubShaderUtils
    {
        internal static void AddFloatProperty(this PropertyCollector collector, string referenceName, float defaultValue, HLSLDeclaration declarationType = HLSLDeclaration.DoNotDeclare)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Default,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = declarationType,
                value = defaultValue,
                displayName = referenceName,
                overrideReferenceName = referenceName,
            });
        }

        internal static void AddToggleProperty(this PropertyCollector collector, string referenceName, bool defaultValue, HLSLDeclaration declarationType = HLSLDeclaration.DoNotDeclare)
        {
            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = defaultValue,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = declarationType,
                displayName = referenceName,
                overrideReferenceName = referenceName,
            });
        }

        // Overloads to do inline PassDescriptor modifications
        // NOTE: param order should match PassDescriptor field order for consistency
        #region PassVariant
        internal static PassDescriptor PassVariant(in PassDescriptor source, PragmaCollection pragmas)
        {
            var result = source;
            result.pragmas = pragmas;
            return result;
        }

        #endregion
    }
}
