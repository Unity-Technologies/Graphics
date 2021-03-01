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
    abstract class UniversalSubTarget : SubTarget<UniversalTarget>
    {
        static readonly GUID kSourceCodeGuid = new GUID("92228d45c1ff66740bfa9e6d97f7e280");  // UniversalSubTarget.cs

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);

/*
            context.AddAssetDependency(subTargetAssetGuid, AssetCollection.Flags.SourceDependency);
            if (!context.HasCustomEditorForRenderPipeline(typeof(HDRenderPipelineAsset)))
                context.AddCustomEditorForRenderPipeline(customInspector, typeof(HDRenderPipelineAsset));

            if (migrationSteps.Migrate(this))
                OnBeforeSerialize();

            // Migration hack to have the case where SG doesn't have version yet but is already upgraded to the stack system
            if (!systemData.firstTimeMigrationExecuted)
            {
                // Force the initial migration step
                MigrateTo(ShaderGraphVersion.FirstTimeMigration);
                systemData.firstTimeMigrationExecuted = true;
                OnBeforeSerialize();
                systemData.materialNeedsUpdateHash = ComputeMaterialNeedsUpdateHash();
            }

            foreach (var subShader in EnumerateSubShaders())
            {
                // patch render type and render queue from pass declaration:
                var patchedSubShader = subShader;
                patchedSubShader.renderType = renderType;
                patchedSubShader.renderQueue = renderQueue;
                context.AddSubShader(patchedSubShader);
            }
*/
        }

        /*
        protected SubShaderDescriptor PostProcessSubShader(SubShaderDescriptor subShaderDescriptor)
        {
            // Update Render State
            subShaderDescriptor.renderType = target.renderType;
            subShaderDescriptor.renderQueue = target.renderQueue;
            return subShaderDescriptor;
        }
        */
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

        /*
        internal static PassDescriptor PassVariant(in PassDescriptor source, BlockFieldDescriptor[] vertexBlocks, BlockFieldDescriptor[] pixelBlocks, PragmaCollection pragmas, DefineCollection defines)
        {
            var result = source;
            result.validVertexBlocks = vertexBlocks;
            result.validPixelBlocks = pixelBlocks;
            result.pragmas = pragmas;
            result.defines = defines;
            return result;
        }
        */

        #endregion
    }
}
