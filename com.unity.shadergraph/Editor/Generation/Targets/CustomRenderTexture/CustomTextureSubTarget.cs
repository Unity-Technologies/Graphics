using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;

namespace UnityEditor.Rendering.CustomRenderTexture.ShaderGraph
{
    sealed class CustomTextureSubTarget : SubTarget<CustomRenderTextureTarget>
    {
        const string kAssetGuid = "5b2d4724a38a5485ba5e7dc2f7d86f1a"; // CustomTextureSubTarget.cs

        internal static FieldDescriptor colorField = new FieldDescriptor(String.Empty, "Color", string.Empty);

        public CustomTextureSubTarget()
        {
            isHidden = false;
            displayName = "Custom Render Texture";
        }

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(new GUID(kAssetGuid), AssetCollection.Flags.SourceDependency);
            context.AddSubShader(SubShaders.CustomRenderTexture);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            context.AddField(colorField, true);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
        }

        static class SubShaders
        {
            public static SubShaderDescriptor CustomRenderTexture = new SubShaderDescriptor()
            {
                generatesPreview = true,
                passes = new PassCollection
                {
                    { FullscreePasses.CustomRenderTexture },
                },
            };
        }
    }
}
