using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule.NativeRenderPassCompiler
{
    // Per pass info on inputs to the pass
    internal struct PassInputData
    {
        public ResourceHandle resource;
    }

    // Per pass info on outputs to the pass
    internal struct PassOutputData
    {
        public ResourceHandle resource;
    }

    // Per pass fragment (attachment) info
    internal struct PassFragmentData
    {
        public ResourceHandle resource;
        public IBaseRenderGraphBuilder.AccessFlags accessFlags;

        public override int GetHashCode()
        {
            var hash = resource.GetHashCode();
            hash = hash * 23 + accessFlags.GetHashCode();
            return hash;
        }

        public static bool EqualForMerge(PassFragmentData x, PassFragmentData y)
        {
            // We ignore the version for now we assume if one pass writes version x and the next y they can
            // be merged in the same native render pass
            return x.resource.index == y.resource.index && x.accessFlags == y.accessFlags;
        }
    }

    internal enum PassMergeState
    {
        None = -1, // this pass is "standalone", it will begin and end the NRP
        Begin = 0, // Only Begin NRP is done by this pass, sequential passes will End the NRP (after 0 or more additional subpasses)
        SubPass = 1, // This pass is a subpass only. Begin has been called by a previous pass and end will be called be a pass after this one
        End = 2 // This pass is both a subpass and will end the current NRP
    }

    // Data per pass
    internal struct PassData
    {
        public readonly int passId; // Index of self in the passData list, can we calculate this somehow in c#? would use offsetof in c++
        public readonly RenderGraphPassType type;
        public readonly bool asyncCompute;
        public bool hasSideEffects;
        public bool culled;
        public int tag; // Arbitrary per node int used by various graph analysis tools

        public bool isSource;
        public bool isSink;
        public int firstInput; //base+offset in CompilerContextData.inputData (use the InputNodes iterator to iterate this more easily)
        public int numInputs;
        public int firstOutput; //base+offset in CompilerContextData.outputData (use the OutputNodes iterator to iterate this more easily)
        public int numOutputs;
        public int firstFragment; //base+offset in CompilerContextData.fragmentData (use the Fragments iterator to iterate this more easily)
        public int numFragments;
        public int firstFragmentInput; //base+offset in CompilerContextData.fragmentData (use the Fragment inputs iterator to iterate this more easily)
        public int numFragmentInputs;
        public int firstCreate; //base+offset in CompilerContextData.createData (use the InputNodes iterator to iterate this more easily)
        public int numCreated;
        public int firstDestroy; //base+offset in CompilerContextData.destroyData (use the InputNodes iterator to iterate this more easily)
        public int numDestroyed;

        public bool fragmentInfoValid;
        public int fragmentInfoWidth;
        public int fragmentInfoHeight;
        public int fragmentInfoVolumeDepth;
        public int fragmentInfoSamples;
        public bool fragmentInfoHasDepth;
        
        public bool insertGraphicsFence; // Whether this pass should insert a fence into the command buffer
        public int waitOnGraphicsFencePassId; // -1 if no fence wait is needed, otherwise the passId to wait on

        public PassMergeState mergeState;
        public int nativePassIndex; // Index of the native pass this pass belongs to
        public int nativeSubPassIndex; // Index of the native subpass this pass belongs to
        public bool beginNativeSubpass; // If true this is the first graph pass of a merged native subpass

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetName(CompilerContextData ctx) => ctx.GetPassName(passId);

        public PassData(ref List<RenderGraphPass> passes, int passIndex)
        {
            passId = passIndex;
            type = passes[passIndex].type;
            asyncCompute = passes[passIndex].enableAsyncCompute;
            hasSideEffects = !passes[passIndex].allowPassCulling;

            mergeState = PassMergeState.None;
            nativePassIndex = -1;
            nativeSubPassIndex = -1;
            beginNativeSubpass = false;

            culled = false;
            tag = 0;

            isSource = false;
            isSink = false;
            firstInput = 0;
            numInputs = 0;
            firstOutput = 0;
            numOutputs = 0;
            firstFragment = 0;
            numFragments = 0;
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

        public string identifier => "pass_" + passId;

        public ReadOnlySpan<PassOutputData> Outputs(CompilerContextData ctx)
            => ctx.outputData.MakeReadOnlySpan(firstOutput, numOutputs);

        public ReadOnlySpan<PassInputData> Inputs(CompilerContextData ctx)
            => ctx.inputData.MakeReadOnlySpan(firstInput, numInputs);

        public ReadOnlySpan<PassFragmentData> Fragments(CompilerContextData ctx)
            => ctx.fragmentData.MakeReadOnlySpan(firstFragment, numFragments);

        public ReadOnlySpan<PassFragmentData> FragmentInputs(CompilerContextData ctx)
            => ctx.fragmentData.MakeReadOnlySpan(firstFragmentInput, numFragmentInputs);

        public ReadOnlySpan<ResourceHandle> FirstUsedResources(CompilerContextData ctx)
            => ctx.createData.MakeReadOnlySpan(firstCreate, numCreated);

        public ReadOnlySpan<ResourceHandle> LastUsedResources(CompilerContextData ctx)
            => ctx.destroyData.MakeReadOnlySpan(firstDestroy, numDestroyed);

        private void SetupAndValidateFragmentInfo(ResourceHandle h, CompilerContextData ctx)
        {
            //resources.GetRenderTargetInfo(h, out var info);
            var resInfo = ctx.UnversionedResourceData(h);
            if (resInfo.width == 0 || resInfo.height == 0 || resInfo.msaaSamples == 0) throw new Exception("GetRenderTargetInfo returned invalid results.");

            if (fragmentInfoValid)
            {
                if (fragmentInfoWidth != resInfo.width ||
                    fragmentInfoHeight != resInfo.height ||
                    fragmentInfoVolumeDepth != resInfo.volumeDepth ||
                    fragmentInfoSamples != resInfo.msaaSamples)
                    throw new Exception("Mismatch in Fragment dimensions");
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

        internal void AddFragment(ResourceHandle h, CompilerContextData ctx)
        {
            SetupAndValidateFragmentInfo(h, ctx);
            numFragments++;
        }

        internal void AddFragmentInput(ResourceHandle h, CompilerContextData ctx)
        {
            SetupAndValidateFragmentInfo(h, ctx);
            numFragmentInputs++;
        }

        internal void AddFirstUse(ResourceHandle h, CompilerContextData ctx)
        {
            // Already registered? Skip it
            foreach (ref readonly var res in FirstUsedResources(ctx))
            {
                if (res.index == h.index)
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

        internal void AddLastUse(ResourceHandle h, CompilerContextData ctx)
        {
            // Already registered? Skip it
            foreach (ref readonly var res in LastUsedResources(ctx))
            {
                if (res.index == h.index)
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
        internal bool IsUsedAsFragment(ResourceHandle h, CompilerContextData ctx)
        {
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

        // Is the resource used the first time in the graph this pass. Resources are always created at version 0 so the passed in version is ignored
        // This is usually when a resource is created but for imported resources it's when it's first used by this graph
        internal bool IsFirstUsed(ResourceHandle h, CompilerContextData ctx)
        {
            foreach (ref readonly var res in FirstUsedResources(ctx))
            {
                if (res.index == h.index)
                {
                    return true;
                }
            }

            return false;
        }
    }

    // Data per attachment of a native renderpass
    internal struct NativePassAttachment
    {
        public ResourceHandle handle;
        public UnityEngine.Rendering.RenderBufferLoadAction loadAction;
        public UnityEngine.Rendering.RenderBufferStoreAction storeAction;
        public bool memoryless;
    }

    internal enum LoadReason
    {
        InvalidReason,
        LoadImported,
        LoadPreviouslyWritten,
        ClearImported,
        ClearCreated,
        FullyRewritten
    }

    internal struct LoadAudit
    {
        public static string[] LoadReasonMessages = new string[] {
            "Invalid reason",
            "The resource is imported in the graph and loaded to fetch the existing buffer contents.",
            "The resource is written by '{pass}' exexuted previously in the graph. The data is loaded.",
            "The resource is imported in the graph but was imported with the 'clear on first use' option enabled. The data is cleared.",
            "The resource is created this pass and cleared on first use.",
            "The pass indicated it will rewrite the full resource contents. Existing contents are not loaded or cleared.",
        };

        public LoadReason reason;
        public int passId;

        public LoadAudit(LoadReason setReason, int setPassId = -1)
        {
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
        NoMSAABuffer
    }

    internal struct StoreAudit
    {
        public static string[] StoreReasonMessages = new string[] {
            "Invalid reason",
            "The resource is imported in the graph. The data is stored so results are available outside the graph.",
            "The resource is read by pass '{pass}' executed later in the graph. The data is stored.",
            "The resource is imported but the import was with the `discard on last use` option enabled. The data is discarded.",
            "The resource is written by this pass but no later passes are using the results. The data is discarded.",
            "The resource was created as MSAA only resource, the data can never be resolved.",
            "The resource is an single sample resource, there is no multi-sample data to handle.",
        };

        public StoreReason reason;
        public int passId;
        public StoreReason msaaReason;
        public int msaaPassId;

        public StoreAudit(StoreReason setReason, int setPassId = -1, StoreReason setMsaaReason = StoreReason.NoMSAABuffer, int setMsaaPassId = -1)
        {
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
        DepthBufferUseMismatch, // One pass is using depth buffer and the other not
        NextPassReadsTexture, // The next pass reads data written by this pass as a texture
        NonRasterPass, // The next pass is a non-raster pass
        DifferentDepthTextures, // The next pass uses a different depth texture (and we only allow one in a whole NRP)
        AttachmentLimitReached, // Adding the next pass would have used more attachments than allowed
        EndOfGraph, // The last pass in the graph was reached
        Merged // I actually got merged
    }

    internal struct PassBreakAudit
    {
        public PassBreakReason reason;
        public int breakPass;

        public PassBreakAudit(PassBreakReason reason, int breakPass)
        {
            this.reason=reason;
            this.breakPass=breakPass; // This is not so simple as finding the next pass as it might be culled etc, so we store it to be sure we get the right pass
        }

        public static string[] BreakReasonMessages = new string[] {
            "The native render pass optimizer never ran on this pass. Pass is standalone and not merged.",
            "The render target sizes of the next pass do not match.",
            "This pass has a different depth buffer enable state than the next pass. (One used depth the other not)",
            "The next data output by this pass as a regular texture.",
            "The next pass is not a raster pass.",
            "The next pass uses a different depth buffer. All passes in the native render pass need to use the same depth buffer.",
            "The limit of 8 native pass attachments would be exceeded when merging with the next pass.",
            "This is the last pass in the graph, there are no other passes to merge.",
            "The next pass got merged into this pass.",
        };
    }

    // Data per native renderpass
    internal struct NativePassData
    {
        // Index of the first graph pass this native pass encapsulates
        public int firstGraphPass; // Offset+count in context pass array
        public int numGraphPasses;

        public int firstNativeSubPass; // Offset+count in context subpass array
        public int numNativeSubPasses;

        public FixedAttachmentArray<PassFragmentData> fragments;
        public FixedAttachmentArray<NativePassAttachment> attachments;
        public FixedAttachmentArray<LoadAudit> loadAudit;
        public FixedAttachmentArray<StoreAudit> storeAudit;
        public int width;
        public int height;
        public int samples;
        public bool hasDepth;
        public PassBreakAudit breakAudit;

        public NativePassData(ref PassData pass, CompilerContextData ctx)
        {
            firstGraphPass = pass.passId;
            numGraphPasses = 1;
            firstNativeSubPass = -1;// Set up during compile
            numNativeSubPasses = 0;
            fragments = new FixedAttachmentArray<PassFragmentData>();
            attachments = new FixedAttachmentArray<NativePassAttachment>();
            loadAudit = new FixedAttachmentArray<LoadAudit>();
            storeAudit = new FixedAttachmentArray<StoreAudit>();
            width = pass.fragmentInfoWidth;
            height = pass.fragmentInfoHeight;
            samples = pass.fragmentInfoSamples;
            hasDepth = pass.fragmentInfoHasDepth;
            breakAudit = new PassBreakAudit(PassBreakReason.NotOptimized, -1);

            foreach (ref readonly var fragment in pass.Fragments(ctx))
            {
                fragments.Add(fragment);
            }

            foreach (ref readonly var fragment in pass.FragmentInputs(ctx))
            {
                fragments.Add(fragment);
            }
        }

        public void Clear()
        {
            firstGraphPass = 0;
            numGraphPasses = 0;
            attachments.Clear();
        }

        public bool IsValid()
        {
            return numGraphPasses > 0;
        }

        public ReadOnlySpan<PassData> GraphPasses(CompilerContextData ctx) => ctx.passData.MakeReadOnlySpan(firstGraphPass, numGraphPasses);

        public void GetDebugName(CompilerContextData ctx, DynamicString dest)
        {
            bool first = true;

            foreach (ref readonly var pass in GraphPasses(ctx))
            {
                if (!first)
                {
                    dest.Append("|");
                }
                dest.Append(pass.GetName(ctx));
                first = false;
            }
        }

        //NOTE NOTE NOTE
        // if testOnly == true this not NOT modify the state in any way, only evaluate it and return the correct PassBreakAudit
        public static PassBreakAudit TryMerge(CompilerContextData contextData, int activeNativePassId, int passIdToAdd, bool testOnly )
        {
            ref var passToAdd = ref contextData.passData.ElementAt(passIdToAdd);

            // Non raster passes (low level, compute,...) will break the native pass chain
            // as they may need to do SetRendertarget or non-fragment work
            if (passToAdd.type != RenderGraphPassType.Raster)
            {
                return new PassBreakAudit(PassBreakReason.NonRasterPass, passIdToAdd);
            }

            ref var nativePass = ref contextData.nativePassData.ElementAt(activeNativePassId);

            // If a pass has no fragment attachments a lot of the tests can be skipped
            // You could argue that a raster pass with no fragments is not allowed but why not?
            // it allow us to set some legacy unity state from fragment passes without breaking NRP
            // Allowing this would means something like
            // Fill Gbuffer - Set Shadow Globals - Do light pass would break as the shadow globals pass
            // is a 0x0 pass with no rendertargets that just sets shadow global texture pointers
            // By allowing this to be merged into the NRP we can actually ensure these passes are merged
            bool hasFragments = (passToAdd.numFragments > 0 || passToAdd.numFragmentInputs > 0);
            if (hasFragments)
            {
                // Easy early outs, sizes mismatch
                if (nativePass.width != passToAdd.fragmentInfoWidth ||
                    nativePass.height != passToAdd.fragmentInfoHeight ||
                    nativePass.samples != passToAdd.fragmentInfoSamples)
                {
                    return new PassBreakAudit(PassBreakReason.TargetSizeMismatch, passIdToAdd);
                }

                // Not same depth enabled state
                if (nativePass.hasDepth != passToAdd.fragmentInfoHasDepth)
                {
                    return new PassBreakAudit(PassBreakReason.DepthBufferUseMismatch, passIdToAdd);
                }

                // Easy early outs, different depth buffers we only allow a single depth for the whole NRP for now ?!?
                // Depth buffer is by-design always at index 0
                if (nativePass.hasDepth)
                {
                    ref var firstFragment = ref contextData.fragmentData.ElementAt(passToAdd.firstFragment);
                    if (nativePass.fragments[0].resource.index != firstFragment.resource.index)
                    {
                        return new PassBreakAudit(PassBreakReason.DifferentDepthTextures, passIdToAdd);
                    }
                }
            }

            // Check the non-fragment inputs of this pass, if they are generated by the current open native pass we can't merge
            // as we need to commit the pixels to the texture
            bool breakPass = false;
            foreach (ref readonly var input in passToAdd.Inputs(contextData))
            {
                var inputResource = input.resource;
                var writingPassId = contextData.resources[inputResource].writePass;
                // Is the writing pass enclosed in the current native renderpass
                if (writingPassId >= nativePass.firstGraphPass && writingPassId < nativePass.firstGraphPass + nativePass.numGraphPasses)
                {
                    // If it's not used as a fragment it's used as some sort of texture read of load so we need so sync it out
                    if (!passToAdd.IsUsedAsFragment(inputResource, contextData))
                    {
                        breakPass = true;
                        break;
                    }
                }
            }
            if (breakPass)
            {
                return new PassBreakAudit(PassBreakReason.NextPassReadsTexture, passIdToAdd);
            }

            // Gather which attachments to add to the current pass
            // Update versions and flags of the attachments that are already used by the pass
            var attachmentsToTryAdding = new FixedAttachmentArray<PassFragmentData>();

            foreach (ref readonly var fragment in passToAdd.Fragments(contextData))
            {
                bool alreadyAttached = false;
                for (int i = 0; i < nativePass.fragments.size; ++i)
                {
                    if (nativePass.fragments[i].resource.index == fragment.resource.index)
                    {
                        alreadyAttached = true;
                        break;
                    }
                }

                if (!alreadyAttached)
                {
                    attachmentsToTryAdding.Add(fragment);
                }
            }

            foreach (ref readonly var fragmentInput in passToAdd.FragmentInputs(contextData))
            {
                bool alreadyAttached = false;
                for (int i = 0; i < nativePass.fragments.size; ++i)
                {
                    if (nativePass.fragments[i].resource.index == fragmentInput.resource.index)
                    {
                        alreadyAttached = true;
                        break;
                    }
                }

                if (!alreadyAttached)
                {
                    attachmentsToTryAdding.Add(fragmentInput);
                }
            }

            // If we would have more than the maximum amount of attachments just start a new native renderpass
            if (nativePass.fragments.size + attachmentsToTryAdding.size > FixedAttachmentArray<PassFragmentData>.MaxAttachments)
            {
                return new PassBreakAudit(PassBreakReason.AttachmentLimitReached, passIdToAdd);
            }

            // All is good! Merge them
            if (!testOnly)
            {
                passToAdd.mergeState = PassMergeState.SubPass;
                if (passToAdd.nativePassIndex >= 0)
                    contextData.nativePassData.ElementAt(passToAdd.nativePassIndex).Clear();
                passToAdd.nativePassIndex = activeNativePassId;
                nativePass.numGraphPasses++;

                // Update versions and flags of existing attachments and
                // add any new attachments
                foreach (ref readonly var newAttach in passToAdd.Fragments(contextData))
                {
                    bool alreadyAttached = false;
                    for (int i = 0; i < nativePass.fragments.size; ++i)
                    {
                        ref var existingAttach = ref nativePass.fragments[i];
                        if (existingAttach.resource.index == newAttach.resource.index)
                        {
                            // Update the attached version access flags and version
                            existingAttach.accessFlags |= newAttach.accessFlags;
                            if (existingAttach.resource.version > newAttach.resource.version)
                                throw new Exception("Adding a older version while a higher version is already registered with the pass.");
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

                foreach (ref readonly var newAttach in passToAdd.FragmentInputs(contextData))
                {
                    bool alreadyAttached = false;
                    for (int i = 0; i < nativePass.fragments.size; ++i)
                    {
                        ref var existingAttach = ref nativePass.fragments[i];
                        if (existingAttach.resource.index == newAttach.resource.index)
                        {
                            // Update the attached version access flags and version
                            existingAttach.accessFlags |= newAttach.accessFlags;
                            if (existingAttach.resource.version > newAttach.resource.version)
                                throw new Exception("Adding a older version while a higher version is already registered with the pass.");
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

                SetPassStatesForNativePass(contextData, activeNativePassId);
            }
            return new PassBreakAudit(PassBreakReason.Merged, passIdToAdd);
        }

        public static void SetPassStatesForNativePass(CompilerContextData contextData, int nativePassId)
        {
            ref var nativePass = ref contextData.nativePassData.ElementAt(nativePassId);
            if (nativePass.numGraphPasses > 1)
            {
                contextData.passData.ElementAt(nativePass.firstGraphPass).mergeState = PassMergeState.Begin;
                for (int i = 1; i < nativePass.numGraphPasses - 1; i++)
                {
                    contextData.passData.ElementAt(nativePass.firstGraphPass + i).mergeState = PassMergeState.SubPass;
                }
                contextData.passData.ElementAt(nativePass.firstGraphPass + nativePass.numGraphPasses - 1).mergeState = PassMergeState.End;
            }
            else
            {
                contextData.passData.ElementAt(nativePass.firstGraphPass).mergeState = PassMergeState.None;
            }
        }
    }
}
