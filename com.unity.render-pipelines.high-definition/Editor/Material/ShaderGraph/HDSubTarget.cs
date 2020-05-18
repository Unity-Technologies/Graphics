using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.Rendering.HighDefinition.ShaderGraph.Legacy;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using static UnityEditor.Rendering.HighDefinition.HDShaderUtils;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    abstract class HDSubTarget : SubTarget<HDTarget>, IHasMetadata,
        IRequiresData<SystemData>
    {
        SystemData m_SystemData;

        // Interface Properties
        SystemData IRequiresData<SystemData>.data
        {
            get => m_SystemData;
            set => m_SystemData = value;
        }

        // Public properties
        public SystemData systemData
        {
            get => m_SystemData;
            set => m_SystemData = value;
        }

        protected virtual int ComputeMaterialNeedsUpdateHash()
        {
            // Alpha test is currently the only property in system data to trigger the material upgrade script.
            int hash = (systemData.alphaTest ? 0 : 1) << 0;
            return hash;
        }

        public override bool IsActive() => true;

        protected abstract ShaderID shaderID { get; }
        protected abstract string customInspector { get; }
        protected abstract string subTargetAssetGuid { get; }
        protected abstract string renderType { get; }
        protected abstract string renderQueue { get; }

        public virtual string identifier => GetType().Name;

        public virtual ScriptableObject GetMetadataObject()
        {
            var hdMetadata = ScriptableObject.CreateInstance<HDMetadata>();
            hdMetadata.shaderID = shaderID;
            return hdMetadata;
        }

        public sealed override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("c09e6e9062cbd5a48900c48a0c2ed1c2")); // HDSubTarget.cs
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath(subTargetAssetGuid));
            context.SetDefaultShaderGUI(customInspector);

            foreach (var subShader in EnumerateSubShaders())
            {
                // patch render type and render queue from pass declaration:
                var patchedSubShader = subShader;
                patchedSubShader.renderType = renderType;
                patchedSubShader.renderQueue = renderQueue;
                context.AddSubShader(patchedSubShader);
            }
        }

        protected abstract IEnumerable<SubShaderDescriptor> EnumerateSubShaders();

        // System data specific fields:
        public override void GetFields(ref TargetFieldContext context)
        {
            // Features
            context.AddField(Fields.LodCrossFade,          systemData.supportLodCrossFade);

            // Surface Type
            context.AddField(Fields.SurfaceOpaque,         systemData.surfaceType == SurfaceType.Opaque);
            context.AddField(Fields.SurfaceTransparent,    systemData.surfaceType != SurfaceType.Opaque);

            // Dots
            context.AddField(HDFields.DotsInstancing,      systemData.dotsInstancing);

            // Blend Mode
            context.AddField(Fields.BlendAdd,              systemData.surfaceType != SurfaceType.Opaque && systemData.blendMode == BlendMode.Additive);
            context.AddField(Fields.BlendAlpha,            systemData.surfaceType != SurfaceType.Opaque && systemData.blendMode == BlendMode.Alpha);
            context.AddField(Fields.BlendPremultiply,      systemData.surfaceType != SurfaceType.Opaque && systemData.blendMode == BlendMode.Premultiply);

            // Double Sided
            context.AddField(HDFields.DoubleSided,         systemData.doubleSidedMode != DoubleSidedMode.Disabled);

            // We always generate the keyword ALPHATEST_ON
            context.AddField(Fields.AlphaTest,             systemData.alphaTest
                && (context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold)
                    || context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow)
                || context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass)
                || context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass)));

            // TODO: we probably need to remove these for some master nodes (eye, stacklit, )
            context.AddField(HDFields.DoAlphaTestPrepass,                   systemData.alphaTest && systemData.alphaTestDepthPrepass
                && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass));
            context.AddField(HDFields.DoAlphaTestPostpass,                  systemData.alphaTest && systemData.alphaTestDepthPostpass
                && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass));

            context.AddField(HDFields.TransparentDepthPrePass,              systemData.surfaceType != SurfaceType.Opaque && systemData.alphaTestDepthPrepass);
            context.AddField(HDFields.TransparentDepthPostPass,             systemData.surfaceType != SurfaceType.Opaque && systemData.alphaTestDepthPostpass);
        }

        public override object saveContext
        {
            get
            {
                int hash = ComputeMaterialNeedsUpdateHash();
                bool needsUpdate = hash != systemData.materialNeedsUpdateHash;
                if (needsUpdate)
                    systemData.materialNeedsUpdateHash = hash;

                return new HDSaveContext{ updateMaterials = needsUpdate };
            }
        } 
    }
}