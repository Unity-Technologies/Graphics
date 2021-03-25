using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    public static class NativeRenderPassUtils
    {
        public static void SetMRTAttachmentsList(ScriptableRenderPass renderPass, ref CameraData cameraData, uint validColorBuffersCount, bool needCustomCameraColorClear,
            bool needCustomCameraDepthClear, Dictionary<Hash128, int[]> mergeableRenderPassesMap, Hash128[] sceneIndexToPassHash,
            Dictionary<Hash128, int> renderPassesAttachmentCount, List<ScriptableRenderPass> activeRenderPassQueue,
            ref AttachmentDescriptor[] activeColorAttachmentDescriptors, ref AttachmentDescriptor activeDepthAttachmentDescriptor)
        {
            int currentSceneIndex = renderPass.sceneIndex;
            Hash128 currentPassHash = sceneIndexToPassHash[currentSceneIndex];
            int[] currentMergeablePasses = mergeableRenderPassesMap[currentPassHash];
            bool isFirstMergeablePass = currentMergeablePasses.First() == currentSceneIndex;

            if (!isFirstMergeablePass)
                return;

            renderPassesAttachmentCount[currentPassHash] = 0;

            int currentAttachmentIdx = 0;
            foreach (var passIdx in currentMergeablePasses)
            {
                if (passIdx == -1)
                    break;
                ScriptableRenderPass pass = activeRenderPassQueue[passIdx];

                for (int i = 0; i < pass.attachmentIndices.Length; ++i)
                    pass.attachmentIndices[i] = -1;

                // TODO: review the lastPassToBB logic to mak it work with merged passes
                bool isLastPass = pass.isLastPass;
                bool isLastPassToBB = false;

                for (int i = 0; i < validColorBuffersCount; ++i)
                {
                    AttachmentDescriptor currentAttachmentDescriptor = new AttachmentDescriptor(pass.renderTargetFormat[i] != GraphicsFormat.None ? pass.renderTargetFormat[i] : SystemInfo.GetGraphicsFormat(DefaultFormat.LDR));

                    // if this is the current camera's last pass, also check if one of the RTs is the backbuffer (BuiltinRenderTextureType.CameraTarget)
                    isLastPassToBB |= isLastPass && (pass.colorAttachments[i] == BuiltinRenderTextureType.CameraTarget);

                    int existingAttachmentIndex = RenderingUtils.FindAttachmentDescriptorIndexInList(currentAttachmentIdx, currentAttachmentDescriptor, activeColorAttachmentDescriptors);

                    if (existingAttachmentIndex == -1)
                    {
                        // add a new attachment

                        activeColorAttachmentDescriptors[currentAttachmentIdx] = currentAttachmentDescriptor;

                        activeColorAttachmentDescriptors[currentAttachmentIdx].ConfigureTarget(pass.colorAttachments[i], false, true);
                        if (needCustomCameraColorClear)
                            activeColorAttachmentDescriptors[currentAttachmentIdx].ConfigureClear(Color.black, 1.0f, 0);

                        pass.attachmentIndices[i] = currentAttachmentIdx;

                        currentAttachmentIdx++;
                        renderPassesAttachmentCount[currentPassHash]++;
                    }
                    else
                    {
                        // attachment was already present
                        pass.attachmentIndices[i] = existingAttachmentIndex;
                    }
                }

                // TODO: this is redundant and is being setup for each attachment. Needs to be done only once per mergeable pass list (we need to make sure mergeable passes use the same depth!)
                activeDepthAttachmentDescriptor = new AttachmentDescriptor(GraphicsFormat.DepthAuto);
                activeDepthAttachmentDescriptor.ConfigureTarget(pass.depthAttachment, !needCustomCameraDepthClear, !isLastPassToBB);
                if (needCustomCameraDepthClear)
                    activeDepthAttachmentDescriptor.ConfigureClear(Color.black, 1.0f, 0);
            }
        }

        public static void SetAttachmentList(ScriptableRenderPass renderPass, ref CameraData cameraData, RenderTargetIdentifier passColorAttachment, RenderTargetIdentifier passDepthAttachment, ClearFlag finalClearFlag, Color finalClearColor,
            Dictionary<Hash128, int[]> mergeableRenderPassesMap, Hash128[] sceneIndexToPassHash,
            Dictionary<Hash128, int> renderPassesAttachmentCount, List<ScriptableRenderPass> activeRenderPassQueue,
            ref AttachmentDescriptor[] activeColorAttachmentDescriptors, ref AttachmentDescriptor activeDepthAttachmentDescriptor)
        {
            int currentSceneIndex = renderPass.sceneIndex;
            Hash128 currentPassHash = sceneIndexToPassHash[currentSceneIndex];
            int[] currentMergeablePasses = mergeableRenderPassesMap[currentPassHash];
            bool isFirstMergeablePass = currentMergeablePasses.First() == currentSceneIndex;

            if (!isFirstMergeablePass)
                return;

            renderPassesAttachmentCount[currentPassHash] = 0;

            int currentAttachmentIdx = 0;
            foreach (var passIdx in currentMergeablePasses)
            {
                if (passIdx == -1)
                    break;
                ScriptableRenderPass pass = activeRenderPassQueue[passIdx];

                for (int i = 0; i < pass.attachmentIndices.Length; ++i)
                    pass.attachmentIndices[i] = -1;

                AttachmentDescriptor currentAttachmentDescriptor;
                var usesTargetTexture = cameraData.targetTexture != null;
                var depthOnly = renderPass.depthOnly || (usesTargetTexture &&
                                                         cameraData.targetTexture.graphicsFormat == GraphicsFormat.DepthAuto);
                // Offscreen depth-only cameras need this set explicitly
                if (depthOnly && usesTargetTexture)
                {
                    passColorAttachment = new RenderTargetIdentifier(cameraData.targetTexture);
                    currentAttachmentDescriptor = new AttachmentDescriptor(GraphicsFormat.DepthAuto);
                }
                else
                    currentAttachmentDescriptor = new AttachmentDescriptor(cameraData.cameraTargetDescriptor.graphicsFormat);

                if (pass.overrideCameraTarget)
                {
                    GraphicsFormat hdrFormat = GraphicsFormat.None;
                    if (cameraData.isHdrEnabled)
                    {
                        if (!Graphics.preserveFramebufferAlpha && RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.B10G11R11_UFloatPack32, FormatUsage.Linear | FormatUsage.Render))
                            hdrFormat = GraphicsFormat.B10G11R11_UFloatPack32;
                        else if (RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.Linear | FormatUsage.Render))
                            hdrFormat = GraphicsFormat.R16G16B16A16_SFloat;
                        else
                            hdrFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);
                    }

                    var defaultFormat = cameraData.isHdrEnabled ? hdrFormat : SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
                    currentAttachmentDescriptor = new AttachmentDescriptor(pass.renderTargetFormat[0] != GraphicsFormat.None ? pass.renderTargetFormat[0] : defaultFormat);
                }

                bool isLastPass = pass.isLastPass;
                var samples = pass.renderTargetSampleCount != -1 ? pass.renderTargetSampleCount : cameraData.cameraTargetDescriptor.msaaSamples;

                var colorAttachmentTarget = (depthOnly || passColorAttachment != BuiltinRenderTextureType.CameraTarget)
                    ? passColorAttachment
                    : (usesTargetTexture ? new RenderTargetIdentifier(cameraData.targetTexture.colorBuffer)
                        : BuiltinRenderTextureType.CameraTarget);

                var depthAttachmentTarget = (passDepthAttachment != BuiltinRenderTextureType.CameraTarget)
                    ? passDepthAttachment
                    : (usesTargetTexture ? new RenderTargetIdentifier(cameraData.targetTexture.depthBuffer)
                        : BuiltinRenderTextureType.Depth);

                // TODO: review the lastPassToBB logic to mak it work with merged passes
                // keep track if this is the current camera's last pass and the RT is the backbuffer (BuiltinRenderTextureType.CameraTarget)
                // knowing isLastPassToBB can help decide the optimal store action as it gives us additional information about the current frame
                bool isLastPassToBB = isLastPass && (colorAttachmentTarget == BuiltinRenderTextureType.CameraTarget);
                currentAttachmentDescriptor.ConfigureTarget(colorAttachmentTarget, ((uint)finalClearFlag & (uint)ClearFlag.Color) == 0, !(samples > 1 && isLastPassToBB));

                // TODO: this is redundant and is being setup for each attachment. Needs to be done only once per mergeable pass list (we need to make sure mergeable passes use the same depth!)
                activeDepthAttachmentDescriptor = new AttachmentDescriptor(GraphicsFormat.DepthAuto);
                activeDepthAttachmentDescriptor.ConfigureTarget(depthAttachmentTarget, ((uint)finalClearFlag & (uint)ClearFlag.Depth) == 0 , !isLastPassToBB);

                if (finalClearFlag != ClearFlag.None)
                {
                    // We don't clear color for Overlay render targets, however pipeline set's up depth only render passes as color attachments which we do need to clear
                    if ((cameraData.renderType != CameraRenderType.Overlay || depthOnly && ((uint)finalClearFlag & (uint)ClearFlag.Color) != 0))
                        currentAttachmentDescriptor.ConfigureClear(finalClearColor, 1.0f, 0);
                    if (((uint)finalClearFlag & (uint)ClearFlag.Depth) != 0)
                        activeDepthAttachmentDescriptor.ConfigureClear(Color.black, 1.0f, 0);
                }

                if (samples > 1)
                    currentAttachmentDescriptor.ConfigureResolveTarget(colorAttachmentTarget); // resolving to the implicit color target's resolve surface TODO: handle m_CameraResolveTarget if present?

                int existingAttachmentIndex = RenderingUtils.FindAttachmentDescriptorIndexInList(currentAttachmentIdx, currentAttachmentDescriptor, activeColorAttachmentDescriptors);

                if (existingAttachmentIndex == -1)
                {
                    // add a new attachment
                    pass.attachmentIndices[0] = currentAttachmentIdx;
                    activeColorAttachmentDescriptors[currentAttachmentIdx] = currentAttachmentDescriptor;
                    currentAttachmentIdx++;
                    renderPassesAttachmentCount[currentPassHash]++;
                }
                else
                {
                    // attachment was already present
                    pass.attachmentIndices[0] = existingAttachmentIndex;
                }
            }
        }

        public static void Configure(CommandBuffer cmd, ScriptableRenderPass renderPass, CameraData cameraData, Hash128[] sceneIndexToPassHash, Dictionary<Hash128, int[]> mergeableRenderPassesMap, List<ScriptableRenderPass> activeRenderPassQueue)
        {
            int currentSceneIndex = renderPass.sceneIndex;
            Hash128 currentPassHash = sceneIndexToPassHash[currentSceneIndex];
            int[] currentMergeablePasses = mergeableRenderPassesMap[currentPassHash];
            bool isFirstMergeablePass = currentMergeablePasses.First() == currentSceneIndex;

            if (isFirstMergeablePass)
            {
                foreach (var passIdx in currentMergeablePasses)
                {
                    if (passIdx == -1)
                        break;
                    ScriptableRenderPass pass = activeRenderPassQueue[passIdx];

                    pass.Configure(cmd, cameraData.cameraTargetDescriptor);
                }
            }
        }

        public static void Execute(ScriptableRenderContext context, ScriptableRenderPass renderPass, CameraData cameraData, ref RenderingData renderingData,  Dictionary<Hash128, int[]> mergeableRenderPassesMap, Hash128[] sceneIndexToPassHash,
            Dictionary<Hash128, int> renderPassesAttachmentCount, List<ScriptableRenderPass> activeRenderPassQueue,
            ref AttachmentDescriptor[] activeColorAttachmentDescriptors, ref AttachmentDescriptor activeDepthAttachmentDescriptor, RenderTargetIdentifier activeDepthAttachment)
        {
            int currentSceneIndex = renderPass.sceneIndex;
            Hash128 currentPassHash = sceneIndexToPassHash[currentSceneIndex];
            int[] currentMergeablePasses = mergeableRenderPassesMap[currentPassHash];

            int validColorBuffersCount = renderPassesAttachmentCount[currentPassHash];

            bool isLastPass = renderPass.isLastPass;
            // TODO: review the lastPassToBB logic to mak it work with merged passes
            // keep track if this is the current camera's last pass and the RT is the backbuffer (BuiltinRenderTextureType.CameraTarget)
            bool isLastPassToBB = isLastPass && (activeColorAttachmentDescriptors[0].loadStoreTarget == BuiltinRenderTextureType.CameraTarget);
            bool useDepth = activeDepthAttachment == RenderTargetHandle.CameraTarget.Identifier() && (!(isLastPassToBB || (isLastPass && cameraData.camera.targetTexture != null)));
            var depthOnly = renderPass.depthOnly || (cameraData.targetTexture != null && cameraData.targetTexture.graphicsFormat == GraphicsFormat.DepthAuto);

            var attachments =
                new NativeArray<AttachmentDescriptor>(useDepth && !depthOnly ? validColorBuffersCount + 1 : 1, Allocator.Temp);

            for (int i = 0; i < validColorBuffersCount; ++i)
                attachments[i] = activeColorAttachmentDescriptors[i];

            if (useDepth && !depthOnly)
                attachments[validColorBuffersCount] = activeDepthAttachmentDescriptor;

            var desc = renderingData.cameraData.cameraTargetDescriptor;
            var sampleCount = desc.msaaSamples;
            int width = renderPass.renderTargetWidth != -1 ? renderPass.renderTargetWidth : desc.width;
            int height = renderPass.renderTargetHeight != -1 ? renderPass.renderTargetHeight : desc.height;
            sampleCount = renderPass.renderTargetSampleCount != -1
                ? renderPass.renderTargetSampleCount
                : sampleCount;

            bool isFirstMergeablePass = currentMergeablePasses[0] == currentSceneIndex;
            int validPassCount = RenderingUtils.GetValidPassIndexCount(currentMergeablePasses);
            bool isLastMergeablePass = currentMergeablePasses[validPassCount - 1] == currentSceneIndex;

            var attachmentIndicesCount = RenderingUtils.GetSubPassAttachmentIndicesCount(renderPass);

            var attachmentIndices = new NativeArray<int>(!depthOnly ? (int)attachmentIndicesCount : 0, Allocator.Temp);
            if (!depthOnly)
            {
                for (int i = 0; i < attachmentIndicesCount; ++i)
                {
                    attachmentIndices[i] = renderPass.attachmentIndices[i];
                }
            }

            if (validPassCount == 1 || isFirstMergeablePass)
            {
                context.BeginRenderPass(width, height, Math.Max(sampleCount, 1), attachments,
                    useDepth ? (!depthOnly ? validColorBuffersCount : 0) : -1);
                attachments.Dispose();

                context.BeginSubPass(attachmentIndices);
            }
            else
            {
                if (!RenderingUtils.AreAttachmentIndicesCompatible(activeRenderPassQueue[currentSceneIndex - 1], activeRenderPassQueue[currentSceneIndex]))
                {
                    context.EndSubPass();
                    context.BeginSubPass(attachmentIndices);
                }
            }

            attachmentIndices.Dispose();

            renderPass.Execute(context, ref renderingData);

            if (validPassCount == 1 || isLastMergeablePass)
            {
                context.EndSubPass();
                context.EndRenderPass();
            }

            for (int i = 0; i < activeColorAttachmentDescriptors.Length; ++i)
            {
                activeColorAttachmentDescriptors[i] = RenderingUtils.emptyAttachment;
            }
            activeDepthAttachmentDescriptor = RenderingUtils.emptyAttachment;
        }
    }
}
