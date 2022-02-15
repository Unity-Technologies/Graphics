using UnityEditor.ShaderGraph;
using UnityEngine;
using System;
using UnityEditor.Rendering.Fullscreen.ShaderGraph;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using BlendMode = UnityEngine.Rendering.BlendMode;
using BlendOp = UnityEditor.ShaderGraph.BlendOp;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HDFullscreenSubTarget : FullscreenSubTarget<HDTarget>, IRequiresData<HDFullscreenData>, IHasMetadata
    {
        static readonly GUID kSourceCodeGuid = new GUID("657f6eb2bee4e2f4985ec1ac58eb04cb");  // HDFullscreenSubTarget.cs

        public override void Setup(ref TargetSetupContext context)
        {
            base.Setup(ref context);
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
        }

        protected override string pipelineTag => HDRenderPipeline.k_ShaderTagName;

        protected override IncludeCollection pregraphIncludes => new IncludeCollection
        {
            { CoreIncludes.MinimalCorePregraph },
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassSampling.hlsl", IncludeLocation.Pregraph},
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl", IncludeLocation.Pregraph}, // Need this to make the scene color/depth nodes work
            { "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fullscreen/HDFullscreenFunctions.hlsl", IncludeLocation.Pregraph},
            { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph }
        };

        HDFullscreenData m_HDFullscreenData;

        HDFullscreenData IRequiresData<HDFullscreenData>.data
        {
            get => m_HDFullscreenData;
            set => m_HDFullscreenData = value;
        }

        public HDFullscreenData hdFullscreenData
        {
            get => m_HDFullscreenData;
            set => m_HDFullscreenData = value;
        }

        public override bool IsNodeAllowedBySubTarget(Type nodeType)
        {
            if (nodeType == typeof(BakedGINode))
                return false;
            return base.IsNodeAllowedBySubTarget(nodeType);
        }

        // We don't need the save context / update materials for now
        public override object saveContext => null;

        public HDFullscreenSubTarget()
        {
            displayName = "Fullscreen";
        }

        protected override void GetStencilPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            context.AddProperty("Enable Stencil", new Toggle { value = fullscreenData.enableStencil }, (evt) =>
            {
                if (Equals(fullscreenData.enableStencil, evt.newValue))
                    return;

                registerUndo("Change Enable Stencil");
                fullscreenData.enableStencil = evt.newValue;
                onChange();
            });

            if (fullscreenData.enableStencil)
            {
                context.globalIndentLevel++;

                context.AddProperty("Show Only HDRP Bits", new Toggle { value = hdFullscreenData.showOnlyHDStencilBits }, (evt) =>
                {
                    if (Equals(hdFullscreenData.showOnlyHDStencilBits, evt.newValue))
                        return;

                    registerUndo("Change Show Only HDRP Bits");
                    hdFullscreenData.showOnlyHDStencilBits = evt.newValue;
                    onChange();
                });

                if (hdFullscreenData.showOnlyHDStencilBits)
                {
                    fullscreenData.stencilReference &= (int)UserStencilUsage.AllUserBits;
                    fullscreenData.stencilReadMask &= (int)UserStencilUsage.AllUserBits;
                    fullscreenData.stencilWriteMask &= (int)UserStencilUsage.AllUserBits;

                    var referenceUserBits = (UserStencilUsage)fullscreenData.stencilReference;
                    context.AddProperty("Reference", new EnumField(referenceUserBits) { value = referenceUserBits }, (evt) =>
                    {
                        if (Equals(fullscreenData.stencilReference, evt.newValue))
                            return;

                        registerUndo("Change Stencil Reference");
                        fullscreenData.stencilReference = (int)(UserStencilUsage)evt.newValue;
                        onChange();
                    });

                    var readMaskUserBits = (UserStencilUsage)fullscreenData.stencilReadMask;
                    context.AddProperty("Read Mask", new EnumField(readMaskUserBits) { value = readMaskUserBits }, (evt) =>
                    {
                        if (Equals(fullscreenData.stencilReadMask, evt.newValue))
                            return;

                        registerUndo("Change Stencil Read Mask");
                        fullscreenData.stencilReadMask = (int)(UserStencilUsage)evt.newValue;
                        onChange();
                    });

                    var writeMaskUserBits = (UserStencilUsage)fullscreenData.stencilWriteMask;
                    context.AddProperty("Write Mask", new EnumField(writeMaskUserBits) { value = writeMaskUserBits }, (evt) =>
                    {
                        if (Equals(fullscreenData.stencilWriteMask, evt.newValue))
                            return;

                        registerUndo("Change Stencil Write Mask");
                        fullscreenData.stencilWriteMask = (int)(UserStencilUsage)evt.newValue;
                        onChange();
                    });
                }
                else
                {
                    context.AddProperty("Reference", new IntegerField { value = fullscreenData.stencilReference, isDelayed = true }, (evt) =>
                    {
                        if (Equals(fullscreenData.stencilReference, evt.newValue))
                            return;

                        registerUndo("Change Stencil Reference");
                        fullscreenData.stencilReference = evt.newValue;
                        onChange();
                    });

                    context.AddProperty("Read Mask", new IntegerField { value = fullscreenData.stencilReadMask, isDelayed = true }, (evt) =>
                    {
                        if (Equals(fullscreenData.stencilReadMask, evt.newValue))
                            return;

                        registerUndo("Change Stencil Read Mask");
                        fullscreenData.stencilReadMask = evt.newValue;
                        onChange();
                    });

                    context.AddProperty("Write Mask", new IntegerField { value = fullscreenData.stencilWriteMask, isDelayed = true }, (evt) =>
                    {
                        if (Equals(fullscreenData.stencilWriteMask, evt.newValue))
                            return;

                        registerUndo("Change Stencil Write Mask");
                        fullscreenData.stencilWriteMask = evt.newValue;
                        onChange();
                    });
                }

                context.AddProperty("Comparison", new EnumField(fullscreenData.stencilCompareFunction) { value = fullscreenData.stencilCompareFunction }, (evt) =>
                {
                    if (Equals(fullscreenData.stencilCompareFunction, evt.newValue))
                        return;

                    registerUndo("Change Stencil Comparison");
                    fullscreenData.stencilCompareFunction = (CompareFunction)evt.newValue;
                    onChange();
                });

                context.AddProperty("Pass", new EnumField(fullscreenData.stencilPassOperation) { value = fullscreenData.stencilPassOperation }, (evt) =>
                {
                    if (Equals(fullscreenData.stencilPassOperation, evt.newValue))
                        return;

                    registerUndo("Change Stencil Pass Operation");
                    fullscreenData.stencilPassOperation = (StencilOp)evt.newValue;
                    onChange();
                });

                context.AddProperty("Fail", new EnumField(fullscreenData.stencilFailOperation) { value = fullscreenData.stencilFailOperation }, (evt) =>
                {
                    if (Equals(fullscreenData.stencilFailOperation, evt.newValue))
                        return;

                    registerUndo("Change Stencil Fail Operation");
                    fullscreenData.stencilFailOperation = (StencilOp)evt.newValue;
                    onChange();
                });

                context.AddProperty("Depth Fail", new EnumField(fullscreenData.stencilDepthTestFailOperation) { value = fullscreenData.stencilDepthTestFailOperation }, (evt) =>
                {
                    if (Equals(fullscreenData.stencilDepthTestFailOperation, evt.newValue))
                        return;

                    registerUndo("Change Stencil Depth Fail Operation");
                    fullscreenData.stencilDepthTestFailOperation = (StencilOp)evt.newValue;
                    onChange();
                });

                context.globalIndentLevel--;
            }
        }
    }
}
