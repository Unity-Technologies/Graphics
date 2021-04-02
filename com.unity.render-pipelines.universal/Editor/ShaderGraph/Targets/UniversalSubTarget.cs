using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Legacy;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    abstract class UniversalSubTarget : SubTarget<UniversalTarget>, IHasMetadata
    {
        static readonly GUID kSourceCodeGuid = new GUID("92228d45c1ff66740bfa9e6d97f7e280");  // UniversalSubTarget.cs

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
        }

        public virtual string identifier => GetType().Name;

        public virtual ScriptableObject GetMetadataObject()
        {
            // Universal currently does not use a metadata object (instead simply looking for the identifier in the subshader tags)
            // but it could be upgraded to use this path if we need to inject more information in the future
            return null;
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
