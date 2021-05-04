using UnityEditor.ShaderGraph;
using UnityEngine;
using static UnityEditor.Rendering.BuiltIn.ShaderUtils;

namespace UnityEditor.Rendering.BuiltIn.ShaderGraph
{
    abstract class BuiltInSubTarget : SubTarget<BuiltInTarget>, IHasMetadata
    {
        static readonly GUID kSourceCodeGuid = new GUID("b0ad362e98650f847a0f2dc834fcbc88");  // BuiltInSubTarget.cs

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
        }

        protected abstract ShaderID shaderID { get; }

        public virtual string identifier => GetType().Name;
        public virtual ScriptableObject GetMetadataObject()
        {
            var bultInMetadata = ScriptableObject.CreateInstance<BuiltInMetadata>();
            bultInMetadata.shaderID = shaderID;
            return bultInMetadata;
        }

        public override object saveContext
        {
            get
            {
                // Ideally this should be used to cause every material using a shader graph to get updated when the built-in target is added to it.
                // There's currently no good path for this (the methods URP and HDRP use are flawed) so instead this is always run
                // on save while waiting for upcoming changes for import dependencies.
                bool needsUpdate = true;
                return new BuiltInShaderGraphSaveContext { updateMaterials = needsUpdate };
            }
        }
    }

    internal static class SubShaderUtils
    {
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
