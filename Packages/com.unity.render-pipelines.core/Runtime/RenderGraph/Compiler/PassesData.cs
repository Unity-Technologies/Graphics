using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler
{
    // Per pass info on inputs to the pass
    [DebuggerDisplay("PassInputData: Res({resource.index})")]
    internal struct PassInputData
    {
        public ResourceHandle resource;
    }

    // Per pass info on outputs to the pass
    [DebuggerDisplay("PassOutputData: Res({resource.index})")]
    internal struct PassOutputData
    {
        public ResourceHandle resource;
    }

    // Per pass fragment (attachment) info
    [DebuggerDisplay("PassFragmentData: Res({resource.index}):{accessFlags}")]
    internal struct PassFragmentData
    {
        public ResourceHandle resource;
        public AccessFlags accessFlags;
        public int mipLevel;
        public int depthSlice;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            var hash = resource.GetHashCode();
            hash = hash * 23 + accessFlags.GetHashCode();
            hash = hash * 23 + mipLevel.GetHashCode();
            hash = hash * 23 + depthSlice.GetHashCode();
            return hash;
        }

        // If you modify this, check if struct RenderPassSetup::Attachment in "GfxDevice\RenderPassSetup.h" also needs changes
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SameSubResource(in PassFragmentData x, in PassFragmentData y)
        {
            // We ignore the version for now we assume if one pass writes version x and the next y they can
            // be merged in the same native render pass
            // We also do not look at the access flags as they get OR-ed together when adding subpasses to the native pass so the access flags
            // will always cover the required access (and thus possibly more if required by other passes)
            return x.resource.index == y.resource.index && x.mipLevel == y.mipLevel && x.depthSlice == y.depthSlice;
        }
    }

    // Per pass random write texture info
    [DebuggerDisplay("PassRandomWriteData: Res({resource.index}):{index}:{preserveCounterValue}")]
    internal struct PassRandomWriteData
    {
        public ResourceHandle resource;
        public int index;
        public bool preserveCounterValue;

        public override int GetHashCode()
        {
            var hash = resource.GetHashCode();
            hash = hash * 23 + index.GetHashCode();
            return hash;
        }
    }

    internal enum PassMergeState
    {
        None = -1, // this pass is "standalone", it will begin and end the NRP
        Begin = 0, // Only Begin NRP is done by this pass, sequential passes will End the NRP (after 0 or more additional subpasses)
        SubPass = 1, // This pass is a subpass only. Begin has been called by a previous pass and end will be called by a pass after this one
        End = 2 // This pass is both a subpass and will end the current NRP
    }

    // Data per pass
    internal struct PassData
    {
        // Warning, any field must initialized in both constructor and ResetAndInitialize function

        public int passId; // Index of self in the passData list, can we calculate this somehow in c#? would use offsetof in c++
        public RenderGraphPassType type;
        public bool hasFoveatedRasterization;
        public int tag; // Arbitrary per node int used by various graph analysis tools

        public PassMergeState mergeState;
        public int nativePassIndex; // Index of the native pass this pass belongs to
        public int nativeSubPassIndex; // Index of the native subpass this pass belongs to

        public int firstInput; //base+offset in CompilerContextData.inputData (use the InputNodes iterator to iterate this more easily)
        public int numInputs;
        public int firstOutput; //base+offset in CompilerContextData.outputData (use the OutputNodes iterator to iterate this more easily)
        public int numOutputs;
        public int firstFragment; //base+offset in CompilerContextData.fragmentData (use the Fragments iterator to iterate this more easily)
        public int numFragments;
        public int firstFragmentInput; //base+offset in CompilerContextData.fragmentData (use the Fragment inputs iterator to iterate this more easily)
        public int numFragmentInputs;
        public int firstRandomAccessResource; //base+offset in CompilerContextData.randomWriteData (use the Fragment inputs iterator to iterate this more easily)
        public int numRandomAccessResources;
        public int firstCreate; //base+offset in CompilerContextData.createData (use the InputNodes iterator to iterate this more easily)
        public int numCreated;
        public int firstDestroy; //base+offset in CompilerContextData.destroyData (use the InputNodes iterator to iterate this more easily)
        public int numDestroyed;

        public int fragmentInfoWidth;
        public int fragmentInfoHeight;
        public int fragmentInfoVolumeDepth;
        public int fragmentInfoSamples;

        public int waitOnGraphicsFencePassId; // -1 if no fence wait is needed, otherwise the passId to wait on

        public bool asyncCompute;
        public bool hasSideEffects;
        public bool culled;
        public bool beginNativeSubpass; // If true this is the first graph pass of a merged native subpass
        public bool fragmentInfoValid;
        public bool fragmentInfoHasDepth;
        public bool insertGraphicsFence; // Whether this pass should insert a fence into the command buffer

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Name GetName(CompilerContextData ctx) => ctx.GetFullPassName(passId);

        public PassData(in RenderGraphPass pass, int passIndex)
        {
            passId = passIndex;
            type = pass.type;
            asyncCompute = pass.enableAsyncCompute;
            hasSideEffects = !pass.allowPassCulling;
            hasFoveatedRasterization = pass.enableFoveatedRasterization;
            mergeState = PassMergeState.None;
            nativePassIndex = -1;
            nativeSubPassIndex = -1;
            beginNativeSubpass = false;

            culled = false;
            tag = 0;

            firstInput = 0;
            numInputs = 0;
            firstOutput = 0;
            numOutputs = 0;
            firstFragment = 0;
            numFragments = 0;
            firstRandomAccessResource = 0;
            numRandomAccessResources = 0;
            firstFragmentInput = 0;
            numFragmentInputs = 0;
            firstCreate = 0;
            numCreated = 0;
            firstDestroy = 0;
            numDestroyed = 0;

            fragmentInfoValid = false;
            fragmentInfoWidth = 0;
            fragmentInfoHeight = 0;
            fragmentInfoVolumeDepth = 0;
            fragmentInfoSamples = 0;
            fragmentInfoHasDepth = false;

            insertGraphicsFence = false;
            waitOnGraphicsFencePassId = -1;
        }

        // Helper func to reset and initialize existing PassData struct directly in a data container without costly deep copy (~120bytes) when adding it
        public void ResetAndInitialize(in RenderGraphPass pass, int passIndex)
        {
            passId = passIndex;
            type = pass.type;
            asyncCompute = pass.enableAsyncCompute;
            hasSideEffects = !pass.allowPassCulling;
            hasFoveatedRasterization = pass.enableFoveatedRasterization;

            mergeState = PassMergeState.None;
            nativePassIndex = -1;
            nativeSubPassIndex = -1;
            beginNativeSubpass = false;

            culled = false;
            tag = 0;

            firstInput = 0;
            numInputs = 0;
            firstOutput = 0;
            numOutputs = 0;
            firstFragment = 0;
            numFragments = 0;
            firstFragmentInput = 0;
            numFragmentInputs = 0;
            firstRandomAccessResource = 0;
            numRandomAccessResources = 0;
            firstCreate = 0;
            numCreated = 0;
            firstDestroy = 0;
            numDestroyed = 0;

            fragmentInfoValid = false;
            fragmentInfoWidth = 0;
            fragmentInfoHeight = 0;
            fragmentInfoVolumeDepth = 0;
            fragmentInfoSamples = 0;
            fragmentInfoHasDepth = false;

            insertGraphicsFence = false;
            waitOnGraphicsFencePassId = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<PassOutputData> Outputs(CompilerContextData ctx)
            => ctx.outputData.MakeReadOnlySpan(firstOutput, numOutputs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<PassInputData> Inputs(CompilerContextData ctx)
            => ctx.inputData.MakeReadOnlySpan(firstInput, numInputs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<PassFragmentData> Fragments(CompilerContextData ctx)
            => ctx.fragmentData.MakeReadOnlySpan(firstFragment, numFragments);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<PassFragmentData> FragmentInputs(CompilerContextData ctx)
            => ctx.fragmentData.MakeReadOnlySpan(firstFragmentInput, numFragmentInputs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<ResourceHandle> FirstUsedResources(CompilerContextData ctx)
            => ctx.createData.MakeReadOnlySpan(firstCreate, numCreated);

        // Loop over this pass's random write textures returned as PassFragmentData
        public ReadOnlySpan<PassRandomWriteData> RandomWriteTextures(CompilerContextData ctx)
         => ctx.randomAccessResourceData.MakeReadOnlySpan(firstRandomAccessResource, numRandomAccessResources);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<ResourceHandle> LastUsedResources(CompilerContextData ctx)
            => ctx.destroyData.MakeReadOnlySpan(firstDestroy, numDestroyed);

        private void SetupAndValidateFragmentInfo(ResourceHandle h, CompilerContextData ctx)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (h.type != RenderGraphResourceType.Texture) new Exception("Only textures can be used as a fragment attachment.");
#endif

            ref readonly var resInfo = ref ctx.UnversionedResourceData(h);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (resInfo.width == 0 || resInfo.height == 0 || resInfo.msaaSamples == 0) throw new Exception("GetRenderTargetInfo returned invalid results.");
#endif
            if (fragmentInfoValid)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (fragmentInfoWidth != resInfo.width ||
                    fragmentInfoHeight != resInfo.height ||
                    fragmentInfoVolumeDepth != resInfo.volumeDepth ||
                    fragmentInfoSamples != resInfo.msaaSamples)
                    throw new Exception("Mismatch in Fragment dimensions");
#endif
            }
            else
            {
                fragmentInfoWidth = resInfo.width;
                fragmentInfoHeight = resInfo.height;
                fragmentInfoSamples = resInfo.msaaSamples;
                fragmentInfoVolumeDepth = resInfo.volumeDepth;
                fragmentInfoValid = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddFragment(ResourceHandle h, CompilerContextData ctx)
        {
            SetupAndValidateFragmentInfo(h, ctx);
            numFragments++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddFragmentInput(ResourceHandle h, CompilerContextData ctx)
        {
            SetupAndValidateFragmentInfo(h, ctx);
            numFragmentInputs++;
        }

        internal void AddRandomAccessResource()
        {
            // This function is here for orthogonality with AddFragment/AddFragmentInput
            // Random write textures can be arbitrary sizes and do not need to validate or set-up the fragment info
            numRandomAccessResources++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddFirstUse(ResourceHandle h, CompilerContextData ctx)
        {
            // Already registered? Skip it
            foreach (ref readonly var res in FirstUsedResources(ctx))
            {
                if (res.index == h.index && res.type == h.type)
                    return;
            }

            ctx.createData.Add(h);
            int addedIndex = ctx.createData.LastIndex();

            // First item added, set up firstCreate
            if (numCreated == 0)
            {
                firstCreate = addedIndex;
            }

            Debug.Assert(addedIndex == firstCreate + numCreated, "you can only incrementally set-up the Creation lists for all passes, AddCreation is called in an arbitrary non-incremental way");

            numCreated++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddLastUse(ResourceHandle h, CompilerContextData ctx)
        {
            // Already registered? Skip it
            foreach (ref readonly var res in LastUsedResources(ctx))
            {
                if (res.index == h.index && res.type == h.type)
                    return;
            }

            ctx.destroyData.Add(h);
            int addedIndex = ctx.destroyData.LastIndex();

            // First item added, set up firstDestroy
            if (numDestroyed == 0)
            {
                firstDestroy = addedIndex;
            }

            Debug.Assert(addedIndex == firstDestroy + numDestroyed, "you can only incrementally set-up the Destruction lists for all passes, AddCreation is called in an arbitrary non-incremental way");
            numDestroyed++;
        }

        // Is the resource used as a fragment this pass.
        // As it is ambiguous if this is an input our output version, the version is ignored
        // This checks use of both MRT attachment as well as input attachment
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly bool IsUsedAsFragment(ResourceHandle h, CompilerContextData ctx)
        {
            //Only textures can be used as a fragment attachment.
            if (h.type != RenderGraphResourceType.Texture) return false;

            // Only raster passes can have fragment attachments
            if (type != RenderGraphPassType.Raster) return false;

            foreach (ref readonly var fragment in Fragments(ctx))
            {
                if (fragment.resource.index == h.index)
                {
                    return true;
                }
            }

            foreach (ref readonly var fragmentInput in FragmentInputs(ctx))
            {
                if (fragmentInput.resource.index == h.index)
                {
                    return true;
                }
            }

            return false;
        }
    }

    // Data per attachment of a native renderpass
    [DebuggerDisplay("Res({handle.index}) : {loadAction} : {storeAction} : {memoryless}")]
    internal struct NativePassAttachment
    {
        public ResourceHandle handle;
        public UnityEngine.Rendering.RenderBufferLoadAction loadAction;
        public UnityEngine.Rendering.RenderBufferStoreAction storeAction;
        public bool memoryless;
        public int mipLevel;
        public int depthSlice;
    }

    internal enum LoadReason
    {
        InvalidReason,
        LoadImported,
        LoadPreviouslyWritten,
        ClearImported,
        ClearCreated,
        FullyRewritten,

        Count
    }

    [DebuggerDisplay("{reason} : {passId}")]
    internal struct LoadAudit
    {
        public static readonly string[] LoadReasonMessages = {
            "Invalid reason",
            "The resource is imported in the graph and loaded to retrieve the existing buffer contents.",
            "The resource is written by {pass} executed previously in the graph. The data is loaded.",
            "The resource is imported in the graph but was imported with the 'clear on first use' option enabled. The data is cleared.",
            "The resource is created in this pass and cleared on first use.",
            "The pass indicated it will rewrite the full resource contents. Existing contents are not loaded or cleared.",
        };

        public LoadReason reason;
        public int passId;

        public LoadAudit(LoadReason setReason, int setPassId = -1)
        {
#if UNITY_EDITOR
            Debug.Assert(LoadReasonMessages.Length == (int)LoadReason.Count,
                $"Make sure {nameof(LoadReasonMessages)} is in sync with {nameof(LoadReason)}");
#endif

            reason = setReason;
            passId = setPassId;
        }
    }

    internal enum StoreReason
    {
        InvalidReason,
        StoreImported,
        StoreUsedByLaterPass,
        DiscardImported,
        DiscardUnused,
        DiscardBindMs,
        NoMSAABuffer,

        Count
    }

    [DebuggerDisplay("{reason} : {passId} / MSAA {msaaReason} : {msaaPassId}")]
    internal struct StoreAudit
    {
        public static readonly string[] StoreReasonMessages = {
            "Invalid reason",
            "The resource is imported in the graph. The data is stored so results are available outside the graph.",
            "The resource is read by pass {pass} executed later in the graph. The data is stored.",
            "The resource is imported but the import was with the 'discard on last use' option enabled. The data is discarded.",
            "The resource is written by this pass but no later passes are using the results. The data is discarded.",
            "The resource was created as MSAA only resource, the data can never be resolved.",
            "The resource is a single sample resource, there is no multi-sample data to handle.",
        };

        public StoreReason reason;
        public int passId;
        public StoreReason msaaReason;
        public int msaaPassId;

        public StoreAudit(StoreReason setReason, int setPassId = -1, StoreReason setMsaaReason = StoreReason.NoMSAABuffer, int setMsaaPassId = -1)
        {
#if UNITY_EDITOR
            Debug.Assert(StoreReasonMessages.Length == (int)StoreReason.Count,
                $"Make sure {nameof(StoreReasonMessages)} is in sync with {nameof(StoreReason)}");
#endif

            reason = setReason;
            passId = setPassId;
            msaaReason = setMsaaReason;
            msaaPassId = setMsaaPassId;
        }
    }

    internal enum PassBreakReason
    {
        NotOptimized, // Optimize never ran on this pass
        TargetSizeMismatch, // Target Sizes or msaa samples don't match
        NextPassReadsTexture, // The next pass reads data written by this pass as a texture
        NonRasterPass, // The next pass is a non-raster pass
        DifferentDepthTextures, // The next pass uses a different depth texture (and we only allow one in a whole NRP)
        AttachmentLimitReached, // Adding the next pass would have used more attachments than allowed
        SubPassLimitReached, // Addind the next pass would have generated more subpasses than allowed
        EndOfGraph, // The last pass in the graph was reached
        FRStateMismatch, // One pass is using foveated rendering and the other not
        Merged, // I actually got merged

        Count
    }

    [DebuggerDisplay("{reason} : {breakPass}")]
    internal struct PassBreakAudit
    {
        public PassBreakReason reason;
        public int breakPass;

        public PassBreakAudit(PassBreakReason reason, int breakPass)
        {
#if UNITY_EDITOR
            Debug.Assert(BreakReasonMessages.Length == (int)PassBreakReason.Count,
                $"Make sure {nameof(BreakReasonMessages)} is in sync with {nameof(PassBreakReason)}");
#endif

            this.reason = reason;
            this.breakPass = breakPass; // This is not so simple as finding the next pass as it might be culled etc, so we store it to be sure we get the right pass
        }

        public static readonly string[] BreakReasonMessages = {
            "The native render pass optimizer never ran on this pass. Pass is standalone and not merged.",
            "The render target sizes of the next pass do not match.",
            "The next pass reads data output by this pass as a regular texture.",
            "The next pass is not a raster render pass.",
            "The next pass uses a different depth buffer. All passes in the native render pass need to use the same depth buffer.",
            $"The limit of {FixedAttachmentArray<PassFragmentData>.MaxAttachments} native pass attachments would be exceeded when merging with the next pass.",
            $"The limit of {NativePassCompiler.k_MaxSubpass} native subpasses would be exceeded when merging with the next pass.",
            "This is the last pass in the graph, there are no other passes to merge.",
            "The the next pass uses a different foveated rendering state",
            "The next pass got merged into this pass.",
        };
    }

    // Data per native renderpass
    internal struct NativePassData
    {
        public FixedAttachmentArray<LoadAudit> loadAudit;
        public FixedAttachmentArray<StoreAudit> storeAudit;
        public PassBreakAudit breakAudit;

        public FixedAttachmentArray<PassFragmentData> fragments;
        public FixedAttachmentArray<NativePassAttachment> attachments;

        // Index of the first graph pass this native pass encapsulates
        public int firstGraphPass; // Offset+count in context pass array
        public int lastGraphPass;
        public int numGraphPasses;

        public int firstNativeSubPass; // Offset+count in context subpass array
        public int numNativeSubPasses;
        public int width;
        public int height;
        public int volumeDepth;
        public int samples;
        public bool hasDepth;
        public bool hasFoveatedRasterization;

        public NativePassData(ref PassData pass, CompilerContextData ctx)
        {
            firstGraphPass = pass.passId;
            lastGraphPass = pass.passId;
            numGraphPasses = 1;
            firstNativeSubPass = -1;// Set up during compile
            numNativeSubPasses = 0;

            fragments = new FixedAttachmentArray<PassFragmentData>();
            attachments = new FixedAttachmentArray<NativePassAttachment>();

            width = pass.fragmentInfoWidth;
            height = pass.fragmentInfoHeight;
            volumeDepth = pass.fragmentInfoVolumeDepth;
            samples = pass.fragmentInfoSamples;
            hasDepth = pass.fragmentInfoHasDepth;
            hasFoveatedRasterization = pass.hasFoveatedRasterization;

            loadAudit = new FixedAttachmentArray<LoadAudit>();
            storeAudit = new FixedAttachmentArray<StoreAudit>();
            breakAudit = new PassBreakAudit(PassBreakReason.NotOptimized, -1);

            foreach (ref readonly var fragment in pass.Fragments(ctx))
            {
                fragments.Add(fragment);
            }

            foreach (ref readonly var fragment in pass.FragmentInputs(ctx))
            {
                fragments.Add(fragment);
            }

            // Graph pass is added as the first native subpass
            TryMergeNativeSubPass(ctx, ref this, ref pass);
        }

        public void Clear()
        {
            firstGraphPass = 0;
            numGraphPasses = 0;
            attachments.Clear();
            fragments.Clear();
            loadAudit.Clear();
            storeAudit.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsValid()
        {
            return numGraphPasses > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<PassData> GraphPasses(CompilerContextData ctx)
        {
            // When there's no pass being culled, we can directly return a Span of the Native List
            if (lastGraphPass - firstGraphPass + 1 == numGraphPasses)
            {
                return ctx.passData.MakeReadOnlySpan(firstGraphPass, numGraphPasses);
            }

            var actualPasses = new PassData[numGraphPasses];

            for (int i = firstGraphPass, index = 0; i < lastGraphPass + 1; ++i)
            {
                var pass = ctx.passData[i];
                if (!pass.culled)
                {
                    actualPasses[index++] = pass;
                }
            }

            return actualPasses;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void GetGraphPassNames(CompilerContextData ctx, DynamicArray<Name> dest)
        {
            foreach (ref readonly var pass in GraphPasses(ctx))
            {
                dest.Add(pass.GetName(ctx));
            }
        }

        // This function does not modify the current render graph state, it only evaluates and returns the correct PassBreakAudit
        public static PassBreakAudit CanMerge(CompilerContextData contextData, int activeNativePassId, int passIdToMerge)
        {
            ref readonly var passToMerge = ref contextData.passData.ElementAt(passIdToMerge);

            // Non raster passes (low level, compute,...) will break the native pass chain
            // as they may need to do SetRendertarget or non-fragment work
            if (passToMerge.type != RenderGraphPassType.Raster)
            {
                return new PassBreakAudit(PassBreakReason.NonRasterPass, passIdToMerge);
            }

            ref readonly var nativePass = ref contextData.nativePassData.ElementAt(activeNativePassId);

            // If a pass has no fragment attachments a lot of the tests can be skipped
            // You could argue that a raster pass with no fragments is not allowed but why not?
            // it allow us to set some legacy unity state from fragment passes without breaking NRP
            // Allowing this would means something like
            // Fill Gbuffer - Set Shadow Globals - Do light pass would break as the shadow globals pass
            // is a 0x0 pass with no rendertargets that just sets shadow global texture pointers
            // By allowing this to be merged into the NRP we can actually ensure these passes are merged
            bool hasFragments = (passToMerge.numFragments > 0 || passToMerge.numFragmentInputs > 0);
            if (hasFragments)
            {
                // Easy early outs, sizes mismatch
                if (nativePass.width != passToMerge.fragmentInfoWidth ||
                    nativePass.height != passToMerge.fragmentInfoHeight ||
                    nativePass.volumeDepth != passToMerge.fragmentInfoVolumeDepth ||
                    nativePass.samples != passToMerge.fragmentInfoSamples)
                {
                    return new PassBreakAudit(PassBreakReason.TargetSizeMismatch, passIdToMerge);
                }

                // Easy early outs, different depth buffers we only allow a single depth for the whole NRP for now ?!?
                // Depth buffer is by-design always at index 0
                if (nativePass.hasDepth && passToMerge.fragmentInfoHasDepth)
                {
                    ref readonly var firstFragment = ref contextData.fragmentData.ElementAt(passToMerge.firstFragment);
                    if (nativePass.fragments[0].resource.index != firstFragment.resource.index)
                    {
                        return new PassBreakAudit(PassBreakReason.DifferentDepthTextures, passIdToMerge);
                    }
                }

                // We do not support foveation state changes within the renderpass due to platform limitation
                if (nativePass.hasFoveatedRasterization != passToMerge.hasFoveatedRasterization)
                {
                    return new PassBreakAudit(PassBreakReason.FRStateMismatch, passIdToMerge);
                }
            }

            // Check the non-fragment inputs of this pass, if they are generated by the current open native pass we can't merge
            // as we need to commit the pixels to the texture
            foreach (ref readonly var input in passToMerge.Inputs(contextData))
            {
                var inputResource = input.resource;
                var writingPassId = contextData.resources[inputResource].writePassId;
                // Is the writing pass enclosed in the current native renderpass
                if (writingPassId >= nativePass.firstGraphPass && writingPassId < nativePass.lastGraphPass + 1)
                {
                    // If it's not used as a fragment, it's used as some sort of texture read of load so we need so sync it out
                    if (!passToMerge.IsUsedAsFragment(inputResource, contextData))
                    {
                        return new PassBreakAudit(PassBreakReason.NextPassReadsTexture, passIdToMerge);
                    }
                }
            }

            // Gather which attachments to add to the current renderpass
            var attachmentsToTryAdding = new FixedAttachmentArray<PassFragmentData>();

            // We can't have more than the maximum amount of attachments in a given native renderpass
            int currAvailableAttachmentSlots = FixedAttachmentArray<PassFragmentData>.MaxAttachments - nativePass.fragments.size;

            foreach (ref readonly var fragment in passToMerge.Fragments(contextData))
            {
                bool alreadyAttached = false;

                for (int i = 0; i < nativePass.fragments.size; ++i)
                {
                    if (PassFragmentData.SameSubResource(nativePass.fragments[i], fragment))
                    {
                        alreadyAttached = true;
                        break;
                    }
                }

                // This fragment is not attached to the native renderpass yet, we will need to attach it
                if (!alreadyAttached)
                {
                    // We already reached the maximum amount of attachments in this renderpass
                    // We can't add any new attachment, just start a new renderpass
                    if (currAvailableAttachmentSlots == 0)
                    {
                        return new PassBreakAudit(PassBreakReason.AttachmentLimitReached, passIdToMerge);
                    }
                    else
                    {
                        attachmentsToTryAdding.Add(fragment);
                        currAvailableAttachmentSlots--;
                    }
                }
            }

            foreach (ref readonly var fragmentInput in passToMerge.FragmentInputs(contextData))
            {
                bool alreadyAttached = false;

                for (int i = 0; i < nativePass.fragments.size; ++i)
                {
                    if (PassFragmentData.SameSubResource(nativePass.fragments[i], fragmentInput))
                    {
                        alreadyAttached = true;
                        break;
                    }
                }

                // This fragment input is not attached to the native renderpass yet, we will need to attach it
                if (!alreadyAttached)
                {
                    // We already reached the maximum amount of attachments in this native renderpass
                    // We can't add any new attachment, just start a new renderpass
                    if (currAvailableAttachmentSlots == 0)
                    {
                        return new PassBreakAudit(PassBreakReason.AttachmentLimitReached, passIdToMerge);
                    }
                    else
                    {
                        attachmentsToTryAdding.Add(fragmentInput);
                        currAvailableAttachmentSlots--;
                    }
                }
            }

            bool canMergeNativeSubPass = CanMergeNativeSubPass(contextData, nativePass, passToMerge);
            if (!canMergeNativeSubPass && nativePass.numGraphPasses + 1 > NativePassCompiler.k_MaxSubpass)
            {
                return new PassBreakAudit(PassBreakReason.SubPassLimitReached, passIdToMerge);
            }

            // All is good! Pass can be merged into active native pass
            return new PassBreakAudit(PassBreakReason.Merged, passIdToMerge);
        }

        // This function follows the structure of TryMergeNativeSubPass but only tests if the new native subpass can be
        // merged with the last one, allowing for early returns. It does not modify the state
        static bool CanMergeNativeSubPass(CompilerContextData contextData, NativePassData nativePass, PassData passToMerge)
        {
            // We have no output attachments, this is an "empty" raster pass doing only non-rendering command so skip it.
            if (passToMerge.numFragments == 0 && passToMerge.numFragmentInputs == 0)
            {
                return true;
            }

            if (nativePass.numNativeSubPasses == 0)
            {
                return false; // nothing to merge with
            }

            ref readonly var fragmentList = ref nativePass.fragments;
            ref readonly var lastPass = ref contextData.nativeSubPassData.ElementAt(nativePass.firstNativeSubPass +
                nativePass.numNativeSubPasses - 1);

            // NOTE: Not all graph subpasses get an actual native pass:
            // - There could be passes that do only non-raster ops (like setglobal) and have no attachments. They don't get a native pass
            // - Renderpasses that use exactly the same rendertargets at the previous pass use the same native pass. This is because
            //   nextSubpass is expensive on some platforms (even if its' essentially a no-op as it's using the same attachments).
            SubPassFlags flags = SubPassFlags.None;

            // If depth ends up being bound only because of merging we explicitly say that we will not write to it
            // which could have been implied by leaving the flag to None
            if (!passToMerge.fragmentInfoHasDepth && nativePass.hasDepth)
            {
                flags = SubPassFlags.ReadOnlyDepth;
            }

            // MRT attachments
            {
                int fragmentIdx = 0;
                int colorOffset = (passToMerge.fragmentInfoHasDepth) ? -1 : 0;
                int colorOutputsLength = passToMerge.numFragments + colorOffset;

                if (colorOutputsLength != lastPass.colorOutputs.Length)
                {
                    return false;
                }

                foreach (ref readonly var graphPassFragment in passToMerge.Fragments(contextData))
                {
                    // Check if we're handling the depth attachment
                    if (passToMerge.fragmentInfoHasDepth && fragmentIdx == 0)
                    {
                        flags = (graphPassFragment.accessFlags.HasFlag(AccessFlags.Write))
                            ? SubPassFlags.None
                            : SubPassFlags.ReadOnlyDepth;
                    }
                    // It's a color attachment
                    else
                    {
                        // Find the index of this subpass's attachment in the native renderpass attachment list
                        int colorAttachmentIdx = -1;
                        for (int fragmentId = 0; fragmentId < fragmentList.size; ++fragmentId)
                        {
                            if (PassFragmentData.SameSubResource(fragmentList[fragmentId], graphPassFragment))
                            {
                                colorAttachmentIdx = fragmentId;
                                break;
                            }
                        }

                        if (colorAttachmentIdx < 0 || colorAttachmentIdx != lastPass.colorOutputs[fragmentIdx + colorOffset])
                        {
                            return false;
                        }
                    }

                    fragmentIdx++;
                }
            }

            // FB-fetch attachments
            {
                int inputIndex = 0;
                int inputsLength = passToMerge.numFragmentInputs;

                if (inputsLength != lastPass.inputs.Length)
                {
                    return false;
                }

                foreach (ref readonly var graphFragmentInput in passToMerge.FragmentInputs(contextData))
                {
                    // Find the index of this subpass's attachment in the native renderpass attachment list
                    int inputAttachmentIdx = -1;
                    for (int fragmentId = 0; fragmentId < fragmentList.size; ++fragmentId)
                    {
                        if (PassFragmentData.SameSubResource(fragmentList[fragmentId], graphFragmentInput))
                        {
                            inputAttachmentIdx = fragmentId;
                            break;
                        }
                    }

                    // need to keep this - same comment as above
                    if (inputAttachmentIdx < 0 || inputAttachmentIdx != lastPass.inputs[inputIndex])
                    {
                        return false;
                    }

                    inputIndex++;
                }
            }

            // last check for flags
            return flags == lastPass.flags;
        }

        // Try to merge the new graph pass into the last native sub pass or create a new one in the current native pass.
        // Modifies the state
        public static void TryMergeNativeSubPass(CompilerContextData contextData, ref NativePassData nativePass, ref PassData passToMerge)
        {
            ref readonly var fragmentList = ref nativePass.fragments;

            // Only done once per native pass (on creation), should stay -1 if no fragments
            if (nativePass is { numNativeSubPasses: 0, fragments: { size: > 0 } })
            {
                nativePass.firstNativeSubPass = contextData.nativeSubPassData.Length;
            }

            // Fill out the subpass descriptors for the native renderpasses
            // NOTE: Not all graph subpasses get an actual native pass:
            // - There could be passes that do only non-raster ops (like setglobal) and have no attachments. They don't get a native pass
            // - Renderpasses that use exactly the same rendertargets at the previous pass use the same native pass. This is because
            //   nextSubpass is expensive on some platforms (even if its' essentially a no-op as it's using the same attachments).
            SubPassDescriptor desc = new SubPassDescriptor();

            // We have no output attachments, this is an "empty" raster pass doing only non-rendering command so skip it.
            if (passToMerge.numFragments == 0 && passToMerge.numFragmentInputs == 0)
            {
                // We always merge it into the currently active
                passToMerge.nativeSubPassIndex = nativePass.numNativeSubPasses - 1;
                passToMerge.beginNativeSubpass = false;
                return;
            }

            // If depth ends up being bound only because of merging we explicitly say that we will not write to it
            // which could have been implied by leaving the flag to None
            if (!passToMerge.fragmentInfoHasDepth && nativePass.hasDepth)
            {
                desc.flags = SubPassFlags.ReadOnlyDepth;
            }

            // MRT attachments
            {
                int fragmentIdx = 0;
                int colorOffset = (passToMerge.fragmentInfoHasDepth) ? -1 : 0;

                desc.colorOutputs = new AttachmentIndexArray(passToMerge.numFragments + colorOffset);

                foreach (ref readonly var graphPassFragment in passToMerge.Fragments(contextData))
                {
                    // Check if we're handling the depth attachment
                    if (passToMerge.fragmentInfoHasDepth && fragmentIdx == 0)
                    {
                        desc.flags = (graphPassFragment.accessFlags.HasFlag(AccessFlags.Write))
                            ? SubPassFlags.None
                            : SubPassFlags.ReadOnlyDepth;
                    }
                    // It's a color attachment
                    else
                    {
                        // Find the index of this subpass's attachment in the native renderpass attachment list
                        int colorAttachmentIdx = -1;
                        for (int fragmentId = 0; fragmentId < fragmentList.size; ++fragmentId)
                        {
                            if (PassFragmentData.SameSubResource(fragmentList[fragmentId], graphPassFragment))
                            {
                                colorAttachmentIdx = fragmentId;
                                break;
                            }
                        }
                        Debug.Assert(colorAttachmentIdx >= 0); // If this is not the case it means we are using an attachment in a sub pass that is not part of the native pass !?!? clear bug

                        // Set up the color indexes
                        desc.colorOutputs[fragmentIdx + colorOffset] = colorAttachmentIdx;
                    }

                    fragmentIdx++;
                }
            }

            // FB-fetch attachments
            {
                int inputIndex = 0;

                desc.inputs = new AttachmentIndexArray(passToMerge.numFragmentInputs);
                foreach (ref readonly var fragmentInput in passToMerge.FragmentInputs(contextData))
                {
                    // Find the index of this subpass's attachment in the native renderpass attachment list
                    int inputAttachmentIdx = -1;
                    for (int fragmentId = 0; fragmentId < fragmentList.size; ++fragmentId)
                    {
                        if (PassFragmentData.SameSubResource(fragmentList[fragmentId], fragmentInput))
                        {
                            inputAttachmentIdx = fragmentId;
                            break;
                        }
                    }
                    Debug.Assert(inputAttachmentIdx >= 0); // If this is not the case it means we are using an attachment in a sub pass that is not part of the native pass !?!? clear bug

                    // Set up the color indexes
                    desc.inputs[inputIndex] = inputAttachmentIdx;

                    inputIndex++;
                }
            }

            if (nativePass.numNativeSubPasses == 0
                || !NativePassCompiler.IsSameNativeSubPass(ref desc, ref contextData.nativeSubPassData.ElementAt(
                    nativePass.firstNativeSubPass +
                    nativePass.numNativeSubPasses - 1)))
            {
                contextData.nativeSubPassData.Add(desc);
                int idx = contextData.nativeSubPassData.LastIndex();
                Debug.Assert(idx == nativePass.firstNativeSubPass + nativePass.numNativeSubPasses);

                nativePass.numNativeSubPasses++;
                passToMerge.beginNativeSubpass = true;
            }
            else
            {
                passToMerge.beginNativeSubpass = false;
            }

            passToMerge.nativeSubPassIndex = nativePass.numNativeSubPasses - 1;
        }

        // In the case where we add a new graph pass with depth to a native pass that didn't have it, we need to update
        // inputs in all the previous native subpasses of the native pass
        static void UpdateNativeSubPassesAttachments(CompilerContextData contextData, ref NativePassData nativePass)
        {
            int lastVisitedNativeSubpassIdx = -1;
            ref readonly var fragmentList = ref nativePass.fragments;

            var countPasses = nativePass.lastGraphPass - nativePass.firstGraphPass + 1;

            // Do not iterate over the last graph pass as it is the one we are currently adding
            for (var graphPassIdx = 0; graphPassIdx < countPasses - 1; ++graphPassIdx)
            {
                // We only check the first graph pass of each existing native subpass - if other graph passes
                // have been merged into a native subpass, it's because they had the same attachments.
                ref readonly var currGraphPass =
                    ref contextData.passData.ElementAt(nativePass.firstGraphPass + graphPassIdx);

                // Already updated this native subpass
                if (currGraphPass.nativeSubPassIndex + nativePass.firstNativeSubPass == lastVisitedNativeSubpassIdx)
                {
                    continue;
                }

                // Shouldn't be necessary since we only check the first graph pass of each existing native subpass
                // But let's be safe and check anyway if the pass has been culled or not.
                if (currGraphPass.culled)
                {
                    continue;
                }

                lastVisitedNativeSubpassIdx = currGraphPass.nativeSubPassIndex + nativePass.firstNativeSubPass;
                ref var nativeSubPassDescriptor =
                    ref contextData.nativeSubPassData.ElementAt(lastVisitedNativeSubpassIdx);

                // If depth ends up being bound only because of merging we explicitly say that we will not write to it
                // which could have been implied by leaving the flag to None
                if (!currGraphPass.fragmentInfoHasDepth && nativePass.hasDepth)
                {
                    nativeSubPassDescriptor.flags = SubPassFlags.ReadOnlyDepth;
                }

                // MRT attachments
                {
                    int fragmentIdx = 0;
                    int colorOffset = (currGraphPass.fragmentInfoHasDepth) ? -1 : 0;

                    nativeSubPassDescriptor.colorOutputs =
                        new AttachmentIndexArray(currGraphPass.numFragments + colorOffset);

                    foreach (ref readonly var graphPassFragment in currGraphPass.Fragments(contextData))
                    {
                        // Check if we're handling the depth attachment
                        if (currGraphPass.fragmentInfoHasDepth && fragmentIdx == 0)
                        {
                            nativeSubPassDescriptor.flags = (graphPassFragment.accessFlags.HasFlag(AccessFlags.Write))
                                ? SubPassFlags.None
                                : SubPassFlags.ReadOnlyDepth;
                        }
                        // It's a color attachment
                        else
                        {
                            // Find the index of this subpass's attachment in the native renderpass attachment list
                            int colorAttachmentIdx = -1;
                            for (int fragmentId = 0; fragmentId < fragmentList.size; ++fragmentId)
                            {
                                if (fragmentList[fragmentId].resource.index == graphPassFragment.resource.index)
                                {
                                    colorAttachmentIdx = fragmentId;
                                    break;
                                }
                            }

                            Debug.Assert(colorAttachmentIdx >=
                                         0); // If this is not the case it means we are using an attachment in a sub pass that is not part of the native pass !?!? clear bug

                            // Set up the color indexes
                            nativeSubPassDescriptor.colorOutputs[fragmentIdx + colorOffset] = colorAttachmentIdx;
                        }

                        fragmentIdx++;
                    }
                }

            }
        }

        public static PassBreakAudit TryMerge(CompilerContextData contextData, int activeNativePassId, int passIdToMerge)
        {
            var passBreakAudit = CanMerge(contextData, activeNativePassId, passIdToMerge);

            // Pass cannot be merged into active native pass
            if (passBreakAudit.reason != PassBreakReason.Merged)
                return passBreakAudit;

            ref var passToMerge = ref contextData.passData.ElementAt(passIdToMerge);
            ref var nativePass = ref contextData.nativePassData.ElementAt(activeNativePassId);

            passToMerge.mergeState = PassMergeState.SubPass;
            if (passToMerge.nativePassIndex >= 0)
                contextData.nativePassData.ElementAt(passToMerge.nativePassIndex).Clear();
            passToMerge.nativePassIndex = activeNativePassId;

            nativePass.numGraphPasses++;
            nativePass.lastGraphPass = passIdToMerge;

            // Depth needs special handling if the native pass doesn't have depth and merges with a pass that does
            // as we require the depth attachment to be at index 0
            if (!nativePass.hasDepth && passToMerge.fragmentInfoHasDepth)
            {
                nativePass.hasDepth = true;
                nativePass.fragments.Add(contextData.fragmentData[passToMerge.firstFragment]);
                var size = nativePass.fragments.size;
                if (size > 1)
                    (nativePass.fragments[0], nativePass.fragments[size-1]) = (nativePass.fragments[size-1], nativePass.fragments[0]);

                // Must update indices from Native subPasses created before
                UpdateNativeSubPassesAttachments(contextData, ref nativePass);
            }

            // Update versions and flags of existing attachments and
            // add any new attachments
            foreach (ref readonly var newAttach in passToMerge.Fragments(contextData))
            {
                bool alreadyAttached = false;

                for (int i = 0; i < nativePass.fragments.size; ++i)
                {
                    ref var existingAttach = ref nativePass.fragments[i];
                    if (PassFragmentData.SameSubResource(existingAttach, newAttach))
                    {
                        // Update the attached version access flags and version
                        existingAttach.accessFlags |= newAttach.accessFlags;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        if (existingAttach.resource.version > newAttach.resource.version)
                            throw new Exception("Adding an older version while a higher version is already registered with the pass.");
#endif
                        existingAttach.resource.version = newAttach.resource.version;
                        alreadyAttached = true;
                        break;
                    }
                }

                if (!alreadyAttached)
                {
                    nativePass.fragments.Add(newAttach);
                }
            }

            foreach (ref readonly var newAttach in passToMerge.FragmentInputs(contextData))
            {
                bool alreadyAttached = false;

                for (int i = 0; i < nativePass.fragments.size; ++i)
                {
                    ref var existingAttach = ref nativePass.fragments[i];
                    if (PassFragmentData.SameSubResource(existingAttach, newAttach))
                    {
                        // Update the attached version access flags and version
                        existingAttach.accessFlags |= newAttach.accessFlags;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        if (existingAttach.resource.version > newAttach.resource.version)
                            throw new Exception("Adding an older version while a higher version is already registered with the pass.");
#endif
                        existingAttach.resource.version = newAttach.resource.version;
                        alreadyAttached = true;
                        break;
                    }
                }

                if (!alreadyAttached)
                {
                    nativePass.fragments.Add(newAttach);
                }
            }

            TryMergeNativeSubPass(contextData, ref nativePass, ref passToMerge);

            SetPassStatesForNativePass(contextData, activeNativePassId);

            return passBreakAudit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPassStatesForNativePass(CompilerContextData contextData, int nativePassId)
        {
            ref readonly var nativePass = ref contextData.nativePassData.ElementAt(nativePassId);
            if (nativePass.numGraphPasses > 1)
            {
                contextData.passData.ElementAt(nativePass.firstGraphPass).mergeState = PassMergeState.Begin;

                var countPasses = nativePass.lastGraphPass - nativePass.firstGraphPass + 1;

                for (int i = 1; i < countPasses; i++)
                {
                    var indexPass = nativePass.firstGraphPass + i;

                    // This pass was culled and should not be considere
                    if (contextData.passData.ElementAt(indexPass).culled)
                    {
                        contextData.passData.ElementAt(indexPass).mergeState = PassMergeState.None;
                        continue;
                    }

                    contextData.passData.ElementAt(nativePass.firstGraphPass + i).mergeState = PassMergeState.SubPass;
                }

                contextData.passData.ElementAt(nativePass.lastGraphPass).mergeState = PassMergeState.End;
            }
            else
            {
                contextData.passData.ElementAt(nativePass.firstGraphPass).mergeState = PassMergeState.None;
            }
        }
    }
}
