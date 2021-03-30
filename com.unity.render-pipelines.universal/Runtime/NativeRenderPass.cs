using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal
{
    public partial class ScriptableRenderer
    {
        private const int kRenderPassMapSize = 10;
        private const int kRenderPassMaxCount = 20;
        private static Dictionary<Hash128, int[]> mergeableRenderPassesMap = new Dictionary<Hash128, int[]>(kRenderPassMapSize);
        private static int[][] mergeableRenderPassesMapArrays;
        private static Hash128[] sceneIndexToPassHash = new Hash128[kRenderPassMaxCount];
        private static Dictionary<Hash128, int> renderPassesAttachmentCount = new Dictionary<Hash128, int>(kRenderPassMapSize);

        private static partial class Profiling
        {
            public static readonly ProfilingSampler setMRTAttachmentsList = new ProfilingSampler($"NativeRenderPass {nameof(SetMRTAttachmentsList)}");
            public static readonly ProfilingSampler setAttachmentList = new ProfilingSampler($"NativeRenderPass {nameof(SetAttachmentList)}");
            public static readonly ProfilingSampler configure = new ProfilingSampler($"NativeRenderPass {nameof(NativeRenderPassConfigure)}");
            public static readonly ProfilingSampler execute = new ProfilingSampler($"NativeRenderPass {nameof(NativeRenderPassExecute)}");
        }

        internal struct RenderPassDescriptor
        {
            internal int w, h, samples, depthID;

            internal RenderPassDescriptor(int width, int height, int sampleCount, int rtID)
            {
                w = width;
                h = height;
                samples = sampleCount;
                depthID = rtID;
            }
        }

        internal static void ResetFrameData()
        {
            if (mergeableRenderPassesMapArrays == null)
                mergeableRenderPassesMapArrays = new int[kRenderPassMapSize][];

            for (int i = 0; i < kRenderPassMapSize; ++i)
            {
                if (mergeableRenderPassesMapArrays[i] == null)
                    mergeableRenderPassesMapArrays[i] = new int[kRenderPassMaxCount];

                for (int j = 0; j < kRenderPassMaxCount; ++j)
                {
                    mergeableRenderPassesMapArrays[i][j] = -1;
                }
            }
        }

        internal static void SetupFrameData(CameraData cameraData, ref List<ScriptableRenderPass> m_ActiveRenderPassQueue, bool isRenderPassEnabled)
        {
            //TODO: edge cases to detect that should affect possible passes to merge
            // - different depth attachment
            // - total number of color attachment > 8
            // - profiling scope
            // Go through all the passes and mark the final one as last pass

            int lastPassIndex = m_ActiveRenderPassQueue.Count - 1;

            // Make sure the list is already sorted!

            mergeableRenderPassesMap.Clear();
            renderPassesAttachmentCount.Clear();

            uint currentHashIndex = 0;
            // reset all the passes last pass flag
            for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
            {
                var renderPass = m_ActiveRenderPassQueue[i];

                // Empty configure to setup dimensions/targets and whatever data is needed for merging
                // We do not execute this at this time, so render targets are still invalid
                var rpDesc = InitializeRenderPassDescriptor(cameraData, renderPass);

                renderPass.isLastPass = false;
                renderPass.sceneIndex = i;

                Hash128 hash = CreateRenderPassHash(rpDesc, currentHashIndex);

                sceneIndexToPassHash[i] = hash;

                bool RPEnabled = renderPass.useNativeRenderPass && isRenderPassEnabled;
                if (!RPEnabled)
                    continue;

                if (!mergeableRenderPassesMap.ContainsKey(hash))
                {
                    mergeableRenderPassesMap.Add(hash, mergeableRenderPassesMapArrays[mergeableRenderPassesMap.Count]);
                    renderPassesAttachmentCount.Add(hash, 0);
                }
                else if (mergeableRenderPassesMap[hash][GetValidPassIndexCount(mergeableRenderPassesMap[hash]) - 1] != (i - 1))
                {
                    // if the passes are not sequential we want to split the current mergeable passes list. So we increment the hashIndex and update the hash

                    currentHashIndex++;
                    hash = CreateRenderPassHash(rpDesc, currentHashIndex);

                    sceneIndexToPassHash[i] = hash;

                    mergeableRenderPassesMap.Add(hash, mergeableRenderPassesMapArrays[mergeableRenderPassesMap.Count]);
                    renderPassesAttachmentCount.Add(hash, 0);
                }

                mergeableRenderPassesMap[hash][GetValidPassIndexCount(mergeableRenderPassesMap[hash])] = i;
            }

            m_ActiveRenderPassQueue[lastPassIndex].isLastPass = true;

            for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
                m_ActiveRenderPassQueue[i].attachmentIndices = new NativeArray<int>(8, Allocator.Temp);
        }

        internal static void SetMRTAttachmentsList(ScriptableRenderPass renderPass, ref CameraData cameraData, uint validColorBuffersCount, bool needCustomCameraColorClear,
            bool needCustomCameraDepthClear, List<ScriptableRenderPass> activeRenderPassQueue, ref AttachmentDescriptor[] activeColorAttachmentDescriptors,
            ref AttachmentDescriptor activeDepthAttachmentDescriptor)
        {
            using (new ProfilingScope(null, Profiling.setMRTAttachmentsList))
            {
                int currentSceneIndex = renderPass.sceneIndex;
                Hash128 currentPassHash = sceneIndexToPassHash[currentSceneIndex];
                int[] currentMergeablePasses = mergeableRenderPassesMap[currentPassHash];

                // Not the first pass
                if (currentMergeablePasses.First() != currentSceneIndex)
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
                        //TODO: what if HDR?
                        AttachmentDescriptor currentAttachmentDescriptor =
                            new AttachmentDescriptor(pass.renderTargetFormat[i] != GraphicsFormat.None ? pass.renderTargetFormat[i] : GetDefaultGraphicsFormat(cameraData));

                        // if this is the current camera's last pass, also check if one of the RTs is the backbuffer (BuiltinRenderTextureType.CameraTarget)
                        isLastPassToBB |= isLastPass && (pass.colorAttachments[i] == BuiltinRenderTextureType.CameraTarget);

                        int existingAttachmentIndex = FindAttachmentDescriptorIndexInList(currentAttachmentIdx,
                            currentAttachmentDescriptor, activeColorAttachmentDescriptors);

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
                    activeDepthAttachmentDescriptor.ConfigureTarget(pass.depthAttachment, !needCustomCameraDepthClear,
                        !isLastPassToBB);
                    if (needCustomCameraDepthClear)
                        activeDepthAttachmentDescriptor.ConfigureClear(Color.black, 1.0f, 0);
                }
            }
        }

        internal static void SetAttachmentList(ScriptableRenderPass renderPass, ref CameraData cameraData, RenderTargetIdentifier passColorAttachment, RenderTargetIdentifier passDepthAttachment, ClearFlag finalClearFlag, Color finalClearColor,
            List<ScriptableRenderPass> activeRenderPassQueue, ref AttachmentDescriptor[] activeColorAttachmentDescriptors, ref AttachmentDescriptor activeDepthAttachmentDescriptor)
        {
            using (new ProfilingScope(null, Profiling.setAttachmentList))
            {
                int currentSceneIndex = renderPass.sceneIndex;
                Hash128 currentPassHash = sceneIndexToPassHash[currentSceneIndex];
                int[] currentMergeablePasses = mergeableRenderPassesMap[currentPassHash];

                // Skip if not the first pass
                if (currentMergeablePasses.First() != currentSceneIndex)
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
                    var depthOnly = renderPass.depthOnly || (usesTargetTexture && cameraData.targetTexture.graphicsFormat == GraphicsFormat.DepthAuto);
                    // Offscreen depth-only cameras need this set explicitly
                    if (depthOnly && usesTargetTexture)
                    {
                        if (cameraData.targetTexture.graphicsFormat == GraphicsFormat.DepthAuto && !pass.overrideCameraTarget)
                                passColorAttachment = new RenderTargetIdentifier(cameraData.targetTexture);
                        else
                                passColorAttachment = renderPass.colorAttachment;
                        currentAttachmentDescriptor = new AttachmentDescriptor(GraphicsFormat.DepthAuto);
                    }
                    else
                        currentAttachmentDescriptor =
                            new AttachmentDescriptor(cameraData.cameraTargetDescriptor.graphicsFormat);

                    if (pass.overrideCameraTarget)
                        currentAttachmentDescriptor = new AttachmentDescriptor(pass.renderTargetFormat[0] != GraphicsFormat.None ? pass.renderTargetFormat[0] : GetDefaultGraphicsFormat(cameraData));

                    bool isLastPass = pass.isLastPass;
                    var samples = pass.renderTargetSampleCount != -1
                        ? pass.renderTargetSampleCount
                        : cameraData.cameraTargetDescriptor.msaaSamples;

                    var colorAttachmentTarget =
                        (depthOnly || passColorAttachment != BuiltinRenderTextureType.CameraTarget)
                        ? passColorAttachment : (usesTargetTexture
                            ? new RenderTargetIdentifier(cameraData.targetTexture.colorBuffer)
                            : BuiltinRenderTextureType.CameraTarget);

                    var depthAttachmentTarget = (passDepthAttachment != BuiltinRenderTextureType.CameraTarget) ?
                        passDepthAttachment : (usesTargetTexture
                            ? new RenderTargetIdentifier(cameraData.targetTexture.depthBuffer)
                            : BuiltinRenderTextureType.Depth);

                    // TODO: review the lastPassToBB logic to mak it work with merged passes
                    // keep track if this is the current camera's last pass and the RT is the backbuffer (BuiltinRenderTextureType.CameraTarget)
                    // knowing isLastPassToBB can help decide the optimal store action as it gives us additional information about the current frame
                    bool isLastPassToBB =
                        isLastPass && (colorAttachmentTarget == BuiltinRenderTextureType.CameraTarget);
                    currentAttachmentDescriptor.ConfigureTarget(colorAttachmentTarget,
                        ((uint)finalClearFlag & (uint)ClearFlag.Color) == 0, !(samples > 1 && isLastPassToBB));

                    // TODO: this is redundant and is being setup for each attachment. Needs to be done only once per mergeable pass list (we need to make sure mergeable passes use the same depth!)
                    activeDepthAttachmentDescriptor = new AttachmentDescriptor(GraphicsFormat.DepthAuto);
                    activeDepthAttachmentDescriptor.ConfigureTarget(depthAttachmentTarget,
                        ((uint)finalClearFlag & (uint)ClearFlag.Depth) == 0, !isLastPassToBB);

                    if (finalClearFlag != ClearFlag.None)
                    {
                        // We don't clear color for Overlay render targets, however pipeline set's up depth only render passes as color attachments which we do need to clear
                        if ((cameraData.renderType != CameraRenderType.Overlay ||
                             depthOnly && ((uint)finalClearFlag & (uint)ClearFlag.Color) != 0))
                            currentAttachmentDescriptor.ConfigureClear(finalClearColor, 1.0f, 0);
                        if (((uint)finalClearFlag & (uint)ClearFlag.Depth) != 0)
                            activeDepthAttachmentDescriptor.ConfigureClear(Color.black, 1.0f, 0);
                    }

                    if (samples > 1)
                        currentAttachmentDescriptor
                            .ConfigureResolveTarget(
                            colorAttachmentTarget);     // resolving to the implicit color target's resolve surface TODO: handle m_CameraResolveTarget if present?

                    int existingAttachmentIndex = FindAttachmentDescriptorIndexInList(currentAttachmentIdx,
                        currentAttachmentDescriptor, activeColorAttachmentDescriptors);

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
        }

        internal static void NativeRenderPassConfigure(CommandBuffer cmd, ScriptableRenderPass renderPass, CameraData cameraData, List<ScriptableRenderPass> activeRenderPassQueue)
        {
            using (new ProfilingScope(null, Profiling.configure))
            {
                int currentSceneIndex = renderPass.sceneIndex;
                Hash128 currentPassHash = sceneIndexToPassHash[currentSceneIndex];
                int[] currentMergeablePasses = mergeableRenderPassesMap[currentPassHash];

                // If it's the first pass, configure the whole merge block
                if (currentMergeablePasses.First() == currentSceneIndex)
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
        }

        internal static void NativeRenderPassExecute(ScriptableRenderContext context, ScriptableRenderPass renderPass, CameraData cameraData, ref RenderingData renderingData, List<ScriptableRenderPass> activeRenderPassQueue,
            ref AttachmentDescriptor[] activeColorAttachmentDescriptors, ref AttachmentDescriptor activeDepthAttachmentDescriptor, RenderTargetIdentifier activeDepthAttachment)
        {
            using (new ProfilingScope(null, Profiling.execute))
            {
                int currentSceneIndex = renderPass.sceneIndex;
                Hash128 currentPassHash = sceneIndexToPassHash[currentSceneIndex];
                int[] currentMergeablePasses = mergeableRenderPassesMap[currentPassHash];

                int validColorBuffersCount = renderPassesAttachmentCount[currentPassHash];

                bool isLastPass = renderPass.isLastPass;
                // TODO: review the lastPassToBB logic to mak it work with merged passes
                // keep track if this is the current camera's last pass and the RT is the backbuffer (BuiltinRenderTextureType.CameraTarget)
                bool isLastPassToBB = isLastPass && (activeColorAttachmentDescriptors[0].loadStoreTarget ==
                    BuiltinRenderTextureType.CameraTarget);
                bool useDepth = activeDepthAttachment == RenderTargetHandle.CameraTarget.Identifier() &&
                    (!(isLastPassToBB || (isLastPass && cameraData.camera.targetTexture != null)));
                var depthOnly = renderPass.depthOnly || (cameraData.targetTexture != null &&
                    cameraData.targetTexture.graphicsFormat == GraphicsFormat.DepthAuto);

                var attachments =
                    new NativeArray<AttachmentDescriptor>(useDepth && !depthOnly ? validColorBuffersCount + 1 : 1,
                        Allocator.Temp);

                for (int i = 0; i < validColorBuffersCount; ++i)
                    attachments[i] = activeColorAttachmentDescriptors[i];

                if (useDepth && !depthOnly)
                    attachments[validColorBuffersCount] = activeDepthAttachmentDescriptor;

                var rpDesc = InitializeRenderPassDescriptor(cameraData, renderPass);

                int validPassCount = GetValidPassIndexCount(currentMergeablePasses);

                var attachmentIndicesCount = GetSubPassAttachmentIndicesCount(renderPass);

                var attachmentIndices =
                    new NativeArray<int>(!depthOnly ? (int)attachmentIndicesCount : 0, Allocator.Temp);
                if (!depthOnly)
                {
                    for (int i = 0; i < attachmentIndicesCount; ++i)
                    {
                        attachmentIndices[i] = renderPass.attachmentIndices[i];
                    }
                }

                if (validPassCount == 1 || currentMergeablePasses[0] == currentSceneIndex) // Check if it's the first pass
                {
                    context.BeginRenderPass(rpDesc.w, rpDesc.h, Math.Max(rpDesc.samples, 1), attachments,
                        useDepth ? (!depthOnly ? validColorBuffersCount : 0) : -1);
                    attachments.Dispose();

                    context.BeginSubPass(attachmentIndices);
                }
                else
                {
                    if (!AreAttachmentIndicesCompatible(activeRenderPassQueue[currentSceneIndex - 1],
                        activeRenderPassQueue[currentSceneIndex]))
                    {
                        context.EndSubPass();
                        context.BeginSubPass(attachmentIndices);
                    }
                }

                attachmentIndices.Dispose();

                renderPass.Execute(context, ref renderingData);

                if (validPassCount == 1 || currentMergeablePasses[validPassCount - 1] == currentSceneIndex) // Check if it's the last pass
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

        internal static uint GetSubPassAttachmentIndicesCount(ScriptableRenderPass pass)
        {
            uint numValidAttachments = 0;

            foreach (var attIdx in pass.attachmentIndices)
            {
                if (attIdx >= 0)
                    ++numValidAttachments;
            }

            return numValidAttachments;
        }

        internal static bool AreAttachmentIndicesCompatible(ScriptableRenderPass lastSubPass, ScriptableRenderPass currentSubPass)
        {
            uint lastSubPassAttCount = GetSubPassAttachmentIndicesCount(lastSubPass);
            uint currentSubPassAttCount = GetSubPassAttachmentIndicesCount(currentSubPass);

            if (currentSubPassAttCount > lastSubPassAttCount)
                return false;

            uint numEqualAttachments = 0;
            for (int currPassIdx = 0; currPassIdx < currentSubPassAttCount; ++currPassIdx)
            {
                for (int lastPassIdx = 0; lastPassIdx < lastSubPassAttCount; ++lastPassIdx)
                {
                    if (currentSubPass.attachmentIndices[currPassIdx] == lastSubPass.attachmentIndices[lastPassIdx])
                        numEqualAttachments++;
                }
            }

            return (numEqualAttachments == currentSubPassAttCount);
        }

        internal static uint GetValidColorAttachmentCount(AttachmentDescriptor[] colorAttachments)
        {
            uint nonNullColorBuffers = 0;
            if (colorAttachments != null)
            {
                foreach (var attachment in colorAttachments)
                {
                    if (attachment != RenderingUtils.emptyAttachment)
                        ++nonNullColorBuffers;
                }
            }
            return nonNullColorBuffers;
        }

        internal static int FindAttachmentDescriptorIndexInList(int attachmentIdx, AttachmentDescriptor attachmentDescriptor, AttachmentDescriptor[] attachmentDescriptors)
        {
            int existingAttachmentIndex = -1;
            for (int i = 0; i < attachmentIdx; ++i)
            {
                AttachmentDescriptor att = attachmentDescriptors[i];

                if (att.loadStoreTarget == attachmentDescriptor.loadStoreTarget)
                {
                    existingAttachmentIndex = i;
                    break;
                }
            }

            return existingAttachmentIndex;
        }

        internal static int GetValidPassIndexCount(int[] array)
        {
            for (int i = 0; i < array.Length; ++i)
                if (array[i] == -1)
                    return i;
            return array.Length - 1;
        }

        internal static Hash128 CreateRenderPassHash(int width, int height, int depthID, int sample, uint hashIndex)
        {
            return new Hash128((uint) width * 10000 + (uint) height, (uint) depthID, (uint) sample, hashIndex);
        }

        internal static Hash128 CreateRenderPassHash(RenderPassDescriptor desc, uint hashIndex)
        {
            return new Hash128((uint) desc.w * 10000 + (uint) desc.h, (uint)desc.depthID, (uint)desc.samples, hashIndex);
        }

        private static RenderPassDescriptor InitializeRenderPassDescriptor(CameraData cameraData, ScriptableRenderPass renderPass)
        {
            var w = renderPass.renderTargetWidth != -1 ? renderPass.renderTargetWidth : cameraData.cameraTargetDescriptor.width;
            var h = renderPass.renderTargetHeight != -1 ? renderPass.renderTargetHeight : cameraData.cameraTargetDescriptor.height;
            var samples = renderPass.renderTargetSampleCount != -1 ? renderPass.renderTargetSampleCount : cameraData.cameraTargetDescriptor.msaaSamples;
            var depthID = renderPass.depthOnly ? renderPass.colorAttachment.GetHashCode() : renderPass.depthAttachment.GetHashCode();
            return new RenderPassDescriptor(w, h, samples, depthID);
        }

        private static GraphicsFormat GetDefaultGraphicsFormat(CameraData cameraData)
        {
            GraphicsFormat hdrFormat = GraphicsFormat.None;
            if (cameraData.isHdrEnabled)
            {
                if (!Graphics.preserveFramebufferAlpha &&
                    RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.B10G11R11_UFloatPack32,
                        FormatUsage.Linear | FormatUsage.Render))
                    hdrFormat = GraphicsFormat.B10G11R11_UFloatPack32;
                else if (RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R16G16B16A16_SFloat,
                    FormatUsage.Linear | FormatUsage.Render))
                    hdrFormat = GraphicsFormat.R16G16B16A16_SFloat;
                else
                    hdrFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);
            }

            return cameraData.isHdrEnabled ? hdrFormat : SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
        }
    }
}
