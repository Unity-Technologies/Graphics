using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;


namespace UnityEngine.Experimental.Rendering.RenderGraphModule.NativeRenderPassCompiler
{
    internal unsafe struct PassCommandBufferData : IDisposable
    {
        public void Dispose()
        {
            var chunk = chain.m_Head;

            while (chunk != null)
            {
                var next = chunk->Next;
                UnsafeUtility.Free(chunk, Allocator.Persistent);
                chunk = next;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Chunk
        {
            internal const int kChunkSize = 4 * 1024;

            internal Chunk* Next;
            internal int Used;
            internal readonly int CapacityRemaining => kChunkSize - Used;
            internal int Reserve(int size)
            {
                var ret = Used;
                Used += size;
                return ret;
            }

            internal void Reset() => Used = 0;
        }

        internal struct ChunkChain
        {
            internal Chunk* m_Head;
            internal Chunk* m_Tail;
        }

        internal ChunkChain chain;

        internal void Reset()
        {
            var chunk = chain.m_Head;
            while (chunk != null)
            {
                chunk->Reset();
                chunk = chunk->Next;
            }
            chain.m_Tail = chain.m_Head;
        }
        internal byte* Reserve(int size)
        {
            if (size >= Chunk.kChunkSize || size <= 0)
                return null;

            if (chain.m_Tail == null || chain.m_Tail->CapacityRemaining < size)
            {
                Chunk* newChunk = null;
                if (chain.m_Tail == null || chain.m_Tail->Next == null)
                {
                    newChunk = (Chunk*)UnsafeUtility.Malloc(sizeof(Chunk) + Chunk.kChunkSize, 16, Allocator.Persistent);
                    var prev = chain.m_Tail;
                    newChunk->Next = null;
                    newChunk->Used = 0;

                    if (prev != null) prev->Next = newChunk;
                }
                else
                {
                    newChunk = chain.m_Tail->Next;
                    Debug.Assert(newChunk->Used == 0);
                }

                if (chain.m_Head == null)
                {
                    chain.m_Head = newChunk;
                }

                chain.m_Tail = newChunk;
            }

            var offset = chain.m_Tail->Reserve(size);
            var ptr = (byte*)chain.m_Tail + sizeof(Chunk) + offset;
            return ptr;
        }
    }

    internal enum PassCommand
    {
        CreateResource,
        ReleaseResource,
        BeginRenderPass,
        ExecuteNode,
        NextSubPass,
        EndRenderPass,

        BeginAsyncCompute,
        EndAsyncCompute,
        InsertFence,
        WaitOnFence,
        SetRandomWriteTarget,
        ClearRandomWriteTargets
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BasicCommand
    {
        public PassCommand CommandType;
        public int TotalSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ResourceLifetimeCommand
    {
        internal BasicCommand Header;
        internal ResourceHandle Handle;
        internal bool clear;

        internal static unsafe void AddCreateCommand(ResourceHandle h, bool clear, ref PassCommandBufferData data )
        {
            var cmd = (ResourceLifetimeCommand*)data.Reserve(sizeof(ResourceLifetimeCommand));

            cmd->Header.CommandType = PassCommand.CreateResource;
            cmd->Header.TotalSize = sizeof(ResourceLifetimeCommand);
            cmd->Handle = h;
            cmd->clear = clear;

            //TODO(ddebaets) batching: keep count instead of handles and store handles right after this command
        }

        internal static unsafe void ExecuteCreateResource(in ResourceLifetimeCommand* cmd, ref PassCommandBufferState state)
        {
            state.resources.forceManualClearOfResourceDisabled = cmd->clear == false; //TODO(ddebaets) meehhhh
            state.resources.CreatePooledResource(state.rgContext, cmd->Handle);
            state.resources.forceManualClearOfResourceDisabled = false;
        }


        internal static unsafe void AddReleaseCommand(ResourceHandle h, ref PassCommandBufferData data)
        {
            var cmd = (ResourceLifetimeCommand*)data.Reserve(sizeof(ResourceLifetimeCommand));

            cmd->Header.CommandType = PassCommand.ReleaseResource;
            cmd->Header.TotalSize = sizeof(ResourceLifetimeCommand);
            cmd->Handle = h;

            //TODO(ddebaets) batching: keep count instead of handles and store handles right after this command
        }
        internal static unsafe void ExecuteReleaseResource(in ResourceLifetimeCommand* cmd, ref PassCommandBufferState state)
        {
            state.resources.ReleasePooledResource(state.rgContext, cmd->Handle);
        }
    }



    [StructLayout(LayoutKind.Sequential)]
    internal struct BeginRenderPassCommand
    {
        internal BasicCommand Header;
        internal int w, h, d, s;
        internal bool hasDepth;
        internal int numAttachments;
        internal int numSubpasses;

        internal int debugNameLength;

        internal static unsafe void AddCommand(int w, int h, int d, int s, ref FixedAttachmentArray<NativePassAttachment> attachments, int handleCount, bool hasDepth, ref NativeList<SubPassDescriptor> passes, int passOffset, int passCount, ref PassCommandBufferData data, DynamicArray<Name> passNamesForDebug)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (w == 0 || h == 0 || d == 0 || s == 0 || passCount == 0 || handleCount == 0)
            {
                throw new Exception("Invalid render pass properties. One or more properties are zero.");
            }
#endif
            var utf8CStrDebugNameLength = 0;
            foreach(ref readonly var passName in passNamesForDebug)
            {
                utf8CStrDebugNameLength += passName.utf8ByteCount + 1; // +1 to add '/' between passes or the null terminator at the end
            }

            var payloadSize = sizeof(NativePassAttachment) * handleCount + sizeof(SubPassDescriptor) * passCount + sizeof(byte) * utf8CStrDebugNameLength;
            var totalSize = sizeof(BeginRenderPassCommand) + payloadSize;
            var payloadPtr = data.Reserve(totalSize);
            var cmd = (BeginRenderPassCommand*)payloadPtr;

            cmd->Header.CommandType = PassCommand.BeginRenderPass;
            cmd->Header.TotalSize = totalSize;

            cmd->w = w;
            cmd->h = h;
            cmd->d = d;
            cmd->s = s;
            cmd->hasDepth = hasDepth;
            cmd->numAttachments = handleCount;
            cmd->numSubpasses = passCount;
            cmd->debugNameLength = utf8CStrDebugNameLength;

            var ptr = payloadPtr + sizeof(BeginRenderPassCommand);
            var handlesPtr = (NativePassAttachment*)(ptr);
            for (var i = 0; i < handleCount; ++i)
            {
                handlesPtr[i] = attachments[i];
            }
            ptr += sizeof(NativePassAttachment) * handleCount;

            var passesPtr = (SubPassDescriptor*)(ptr);
            for (var i = 0; i < passCount; ++i)
            {
                passesPtr[i] = passes[passOffset+i];
            }
            ptr += sizeof(SubPassDescriptor) * passCount;

            if(utf8CStrDebugNameLength > 0)
            {
                var debugStrPtr = (byte*)(ptr);
                foreach (ref readonly var passName in passNamesForDebug)
                {
                    System.Text.Encoding.UTF8.GetBytes(passName.name, new Span<byte>(debugStrPtr, passName.utf8ByteCount));
                    debugStrPtr += passName.utf8ByteCount;
                    // Adding '/' in UTF8
                    *debugStrPtr = (byte)(0x2F); 
                    ++debugStrPtr;
                }
                // Rewriting last '/' to be the null terminator
                --debugStrPtr;
                *debugStrPtr = (byte)0; 
            }
        }

        internal static unsafe void Execute(in byte* ptr, ref PassCommandBufferState state)
        {
            var cmd = (BeginRenderPassCommand*)ptr;
            var attachmentsPtr = (NativePassAttachment*)(ptr + +sizeof(BeginRenderPassCommand));
            var subpassPtr = (SubPassDescriptor*)(ptr + sizeof(BeginRenderPassCommand) + cmd->numAttachments * sizeof(NativePassAttachment));

            byte* debugNamePtr = null;
            if(cmd->debugNameLength > 0)
            {
                debugNamePtr = (byte*)(ptr + sizeof(BeginRenderPassCommand) + cmd->numAttachments * sizeof(NativePassAttachment) + sizeof(SubPassDescriptor) * cmd->numSubpasses);
            }
            var debugName = (debugNamePtr != null) ? new ReadOnlySpan<byte>(debugNamePtr, cmd->debugNameLength) : null;

            var subpass = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<SubPassDescriptor>(subpassPtr, cmd->numSubpasses, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safetyHandle = AtomicSafetyHandle.Create();
            AtomicSafetyHandle.SetAllowReadOrWriteAccess(safetyHandle, true);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref subpass, safetyHandle);
#endif
            var depthIDx = (cmd->hasDepth) ? 0 : -1;
            var att = new NativeArray<AttachmentDescriptor>(cmd->numAttachments, Allocator.Temp);
            for (var i = 0; i < cmd->numAttachments; ++i)
            {
                var idx = attachmentsPtr[i].handle.index;
                state.resources.GetRenderTargetInfo(attachmentsPtr[i].handle, out var renderTargetInfo);
                var ad = new AttachmentDescriptor(renderTargetInfo.format);

                // Set up the RT pointers
                if (attachmentsPtr[i].memoryless == false)
                {
                    var rthandle = state.resources.GetTexture(new TextureHandle(attachmentsPtr[i].handle));

                    //HACK: Always set the loadstore target even if StoreAction == DontCare or Resolve
                    //and LoadAction == Clear or DontCare
                    //in these cases you could argue setting the loadStoreTarget to NULL and only set the resolveTarget
                    //but this confuses the backend (on vulkan) and in general is not how the lower level APIs tend to work.
                    //because of the RenderTexture duality where we always bundle store+resolve targets as one RTex
                    //it does become impossible to have a memoryless loadStore texture with a memoryfull resolve
                    //but that is why we mark this as a hack and future work to fix.
                    //The proper (and planned) solution would be to move away from the render texture duality.
                    RenderTargetIdentifier rtidAllSlices = rthandle;
                    rtidAllSlices = new RenderTargetIdentifier(rtidAllSlices, 0, CubemapFace.Unknown, -1);
                    ad.loadStoreTarget = rtidAllSlices;

                    if (attachmentsPtr[i].storeAction == RenderBufferStoreAction.Resolve ||
                        attachmentsPtr[i].storeAction == RenderBufferStoreAction.StoreAndResolve)
                    {
                        ad.resolveTarget = rthandle;
                    }
                }
                // In the memoryless case it's valid to not set both loadStoreTarget/and resolveTarget as the backend will allocate a transient one

                ad.loadAction = attachmentsPtr[i].loadAction;
                ad.storeAction = attachmentsPtr[i].storeAction;

                // Set up clear colors if we have a clear load action
                if (attachmentsPtr[i].loadAction == RenderBufferLoadAction.Clear)
                {
                    ad.clearColor = Color.red;
                    ad.clearDepth = 1.0f;
                    ad.clearStencil = 0;
                    var desc = state.resources.GetTextureResourceDesc(attachmentsPtr[i].handle);
                    if (i == 0 && cmd->hasDepth)
                    {
                        // TODO: There seems to be no clear depth specified ?!?!
                        ad.clearDepth = 1.0f;// desc.clearDepth;
                    }
                    else
                    {
                        ad.clearColor = desc.clearColor;
                    }
                }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (renderTargetInfo.width != cmd->w || renderTargetInfo.height != cmd->h || renderTargetInfo.msaaSamples != cmd->s)
                {
                    throw new Exception("Low level rendergraph error: Attachments in renderpass do not match!");
                }
#endif
                att[i] = ad;
            }

            //PassCommandBufferState.NativeRenderPassSampler.Begin(state.rgContext.cmd);

            state.rgContext.cmd.BeginRenderPass(cmd->w, cmd->h, cmd->d, cmd->s, att, depthIDx, subpass, debugName);

            att.Dispose();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(safetyHandle);
#endif
            CommandBuffer.ThrowOnSetRenderTarget = true;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NextSubpassCommand
    {
        internal BasicCommand Header;

        internal static unsafe void AddCommand(ref PassCommandBufferData data)
        {
            var cmd = (NextSubpassCommand*)data.Reserve(sizeof(NextSubpassCommand));
            cmd->Header.CommandType = PassCommand.NextSubPass;
            cmd->Header.TotalSize = sizeof(NextSubpassCommand);
        }
        internal static void Execute(ref PassCommandBufferState state)
        {
            state.rgContext.cmd.NextSubPass();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EndRenderPassCommand
    {
        internal BasicCommand Header;

        internal static unsafe void AddCommand(ref PassCommandBufferData data)
        {
            var cmd = (EndRenderPassCommand*)data.Reserve(sizeof(EndRenderPassCommand));
            cmd->Header.CommandType = PassCommand.EndRenderPass;
            cmd->Header.TotalSize = sizeof(EndRenderPassCommand);
        }

        internal static void Execute(ref PassCommandBufferState state)
        {
            state.rgContext.cmd.EndRenderPass();
            //PassCommandBufferState.NativeRenderPassSampler.End(state.rgContext.cmd);
            CommandBuffer.ThrowOnSetRenderTarget = false;
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    internal struct ExecuteNodeCommand
    {
        internal BasicCommand Header;
        internal int passID;

        internal static unsafe  void AddCommand(int passID, ref PassCommandBufferData data)
        {
            var cmd = (ExecuteNodeCommand*)data.Reserve(sizeof(ExecuteNodeCommand));
            cmd->Header.CommandType = PassCommand.ExecuteNode;
            cmd->Header.TotalSize = sizeof(ExecuteNodeCommand);
            cmd->passID = passID;
        }

        internal static unsafe void Execute(in ExecuteNodeCommand* cmd, ref PassCommandBufferState state)
        {
            var pass = state.passes[cmd->passID];

#if THROW_ON_SETRENDERTARGET_DEBUG
            if (pass.type == RenderGraphPassType.Raster)
            {
                CommandBuffer.ThrowOnSetRenderTarget = true;
            }
#endif
            try
            {
                state.rgContext.executingPass = pass;

                if (!pass.HasRenderFunc())
                {
                    throw new InvalidOperationException(string.Format("RenderPass {0} was not provided with an execute function.", state.passes[cmd->passID].name));
                }

                using (new ProfilingScope(state.rgContext.cmd, pass.customSampler))
                {
                    pass.Execute(state.rgContext);

                    foreach (var tex in pass.setGlobalsList)
                    {
                        state.rgContext.cmd.SetGlobalTexture(tex.Item2, tex.Item1);
                    }
                }


            }
            finally
            {
#if THROW_ON_SETRENDERTARGET_DEBUG
                if (pass.type == RenderGraphPassType.Raster)
                {
                    CommandBuffer.ThrowOnSetRenderTarget = false;
                }
#endif
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SetRandomWriteTargetCommand
    {
        internal BasicCommand Header;
        internal int Index;
        internal bool PreserveCounterValue;
        internal ResourceHandle Resource;

        internal static unsafe void AddCommand(int index, ResourceHandle res, ref PassCommandBufferData data)
        {
            var cmd = (SetRandomWriteTargetCommand*)data.Reserve(sizeof(SetRandomWriteTargetCommand));
            cmd->Header.CommandType = PassCommand.SetRandomWriteTarget;
            cmd->Header.TotalSize = sizeof(SetRandomWriteTargetCommand);
            cmd->Index = index;
            cmd->Resource = res;
        }
        internal static unsafe void Execute(in SetRandomWriteTargetCommand* cmd, ref PassCommandBufferState state)
        {
            var resource = cmd->Resource;
            if (resource.type == RenderGraphResourceType.Texture)
            {
                var tex = state.resources.GetTexture(new TextureHandle(resource));
                state.rgContext.cmd.SetRandomWriteTarget(cmd->Index, tex);
            }
            else if (resource.type == RenderGraphResourceType.Buffer)
            {
                var buff = state.resources.GetBuffer(new BufferHandle(resource));
                // Default is to preserve the value
                if (cmd->PreserveCounterValue == false)
                {
                    state.rgContext.cmd.SetRandomWriteTarget(cmd->Index, buff, false);
                }
                else
                {
                    state.rgContext.cmd.SetRandomWriteTarget(cmd->Index, buff);
                }

            }
            else
            {
                throw new Exception($"Invalid resource type {resource.type}, expected texture or buffer");
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ClearRandomWriteTargetsCommand
    {
        internal BasicCommand Header;

        internal static unsafe void AddCommand(ref PassCommandBufferData data)
        {
            var cmd = (ClearRandomWriteTargetsCommand*)data.Reserve(sizeof(ClearRandomWriteTargetsCommand));
            cmd->Header.CommandType = PassCommand.ClearRandomWriteTargets;
            cmd->Header.TotalSize = sizeof(ClearRandomWriteTargetsCommand);
        }
        internal static void Execute(ref PassCommandBufferState state)
        {
            state.rgContext.cmd.ClearRandomWriteTargets();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BeginAsyncComputeCommand
    {
        internal BasicCommand Header;

        internal static unsafe  void AddCommand(ref PassCommandBufferData data)
        {
            var cmd = (BeginAsyncComputeCommand*)data.Reserve(sizeof(BeginAsyncComputeCommand));
            cmd->Header.CommandType = PassCommand.BeginAsyncCompute;
            cmd->Header.TotalSize = sizeof(BeginAsyncComputeCommand);
        }

        internal static unsafe void Execute(in BeginAsyncComputeCommand* cmd, ref PassCommandBufferState state)
        {
            state.prevCmdBuffer = state.rgContext.cmd;

            var asyncCmd = CommandBufferPool.Get("async cmd");
            asyncCmd.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);
            state.rgContext.cmd = asyncCmd;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct EndAsyncComputeCommand
    {
        internal BasicCommand Header;

        internal static unsafe  void AddCommand(ref PassCommandBufferData data)
        {
            var cmd = (BeginAsyncComputeCommand*)data.Reserve(sizeof(EndAsyncComputeCommand));
            cmd->Header.CommandType = PassCommand.EndAsyncCompute;
            cmd->Header.TotalSize = sizeof(EndAsyncComputeCommand);
        }

        internal static unsafe void Execute(in EndAsyncComputeCommand* cmd, ref PassCommandBufferState state)
        {
            state.rgContext.renderContext.ExecuteCommandBufferAsync(state.rgContext.cmd, ComputeQueueType.Background);
            CommandBufferPool.Release(state.rgContext.cmd);
            state.rgContext.cmd = state.prevCmdBuffer;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct InsertGraphicsFenceCommand
    {
        internal BasicCommand Header;
        internal int passID;

        internal static unsafe  void AddCommand(int _passID, ref PassCommandBufferData data)
        {
            var cmd = (InsertGraphicsFenceCommand*)data.Reserve(sizeof(InsertGraphicsFenceCommand));
            cmd->Header.CommandType = PassCommand.InsertFence;
            cmd->Header.TotalSize = sizeof(InsertGraphicsFenceCommand);
            cmd->passID = _passID;
        }

        internal static unsafe void Execute(in InsertGraphicsFenceCommand* cmd, ref PassCommandBufferState state)
        {
            var fence = state.rgContext.cmd.CreateAsyncGraphicsFence();
            state.fences[cmd->passID] = fence;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct WaitOnGraphicsFenceCommand
    {
        internal BasicCommand Header;
        internal int passID;

        internal static unsafe  void AddCommand(int _passID, ref PassCommandBufferData data)
        {
            var cmd = (WaitOnGraphicsFenceCommand*)data.Reserve(sizeof(WaitOnGraphicsFenceCommand));
            cmd->Header.CommandType = PassCommand.WaitOnFence;
            cmd->Header.TotalSize = sizeof(WaitOnGraphicsFenceCommand);
            cmd->passID = _passID;
        }

        internal static unsafe void Execute(in WaitOnGraphicsFenceCommand* cmd, ref PassCommandBufferState state)
        {
            var fence = state.fences[cmd->passID];
            state.rgContext.cmd.WaitOnAsyncGraphicsFence(fence);
        }
    }

    internal struct PassCommandBufferState
    {
        internal RenderGraphResourceRegistry resources;
        internal InternalRenderGraphContext rgContext;
        internal List<RenderGraphPass> passes;
        internal CommandBuffer prevCmdBuffer;
        internal Dictionary<int, GraphicsFence> fences;

        enum ProfilingSamplers
        {
            NativeRenderPass
        }

        public static ProfilingSampler NativeRenderPassSampler = ProfilingSampler.Get(ProfilingSamplers.NativeRenderPass);

        internal void Reset(InternalRenderGraphContext _rgContext, RenderGraphResourceRegistry _resources, List<RenderGraphPass> _passes)
        {
            fences ??= new Dictionary<int, GraphicsFence>(8);

            resources = _resources;
            rgContext = _rgContext;
            passes = _passes;

            prevCmdBuffer = null;
            fences.Clear();
        }
    }

    interface IExecutionPolicy
    {
        unsafe void ExecuteInternal(BasicCommand* baseCommand, ref PassCommandBufferState state);
    }
    struct DefaultExecution : IExecutionPolicy
    {
        public unsafe void ExecuteInternal(BasicCommand* baseCommand, ref PassCommandBufferState state)
        {
            switch (baseCommand->CommandType)
            {
                case PassCommand.CreateResource:
                {
                    var cmd = (ResourceLifetimeCommand*)baseCommand;
                    ResourceLifetimeCommand.ExecuteCreateResource(cmd, ref state);
                    break;
                }
                case PassCommand.ReleaseResource:
                {
                    var cmd = (ResourceLifetimeCommand*)baseCommand;
                    ResourceLifetimeCommand.ExecuteReleaseResource(cmd, ref state);
                    break;
                }
                case PassCommand.BeginRenderPass:
                {
                    BeginRenderPassCommand.Execute((Byte*)baseCommand, ref state);
                    break;
                }
                case PassCommand.NextSubPass:
                {
                    NextSubpassCommand.Execute(ref state);
                    break;
                }
                case PassCommand.EndRenderPass:
                {
                    EndRenderPassCommand.Execute(ref state);
                    break;
                }
                case PassCommand.ExecuteNode:
                {
                    var cmd = (ExecuteNodeCommand*)baseCommand;
                    ExecuteNodeCommand.Execute(cmd, ref state);
                    break;
                }
                case PassCommand.BeginAsyncCompute:
                {
                    var cmd = (BeginAsyncComputeCommand*)baseCommand;
                    BeginAsyncComputeCommand.Execute(cmd, ref state);
                    break;
                }
                case PassCommand.EndAsyncCompute:
                {
                    var cmd = (EndAsyncComputeCommand*)baseCommand;
                    EndAsyncComputeCommand.Execute(cmd, ref state);
                    break;
                }
                case PassCommand.InsertFence:
                {
                    var cmd = (InsertGraphicsFenceCommand*)baseCommand;
                    InsertGraphicsFenceCommand.Execute(cmd, ref state);
                    break;
                }
                case PassCommand.WaitOnFence:
                {
                    var cmd = (WaitOnGraphicsFenceCommand*)baseCommand;
                    WaitOnGraphicsFenceCommand.Execute(cmd, ref state);
                    break;
                }
                case PassCommand.SetRandomWriteTarget:
                {
                    var cmd = (SetRandomWriteTargetCommand*)baseCommand;
                    SetRandomWriteTargetCommand.Execute(cmd, ref state);
                    break;
                }
                case PassCommand.ClearRandomWriteTargets:
                {
                    var cmd = (ClearRandomWriteTargetsCommand*)baseCommand;
                    ClearRandomWriteTargetsCommand.Execute(ref state);
                    break;
                }
                default:
                    Debug.LogError($"Unknown command {baseCommand->CommandType}");
                    break;

            }
        }
    }

    struct LoggerExecution : IExecutionPolicy
    {
        public unsafe void ExecuteInternal(BasicCommand* baseCommand, ref PassCommandBufferState state)
        {
            switch (baseCommand->CommandType)
            {
                case PassCommand.CreateResource:
                {
                    var cmd = (ResourceLifetimeCommand*)baseCommand;
                    var name = state.resources.GetRenderGraphResourceName(cmd->Handle);
                    Debug.Log($"Creating resource {name}");
                    break;
                }
                case PassCommand.ReleaseResource:
                {
                    var cmd = (ResourceLifetimeCommand*)baseCommand;
                    var name = state.resources.GetRenderGraphResourceName(cmd->Handle);
                    Debug.Log($"releasing resource {name}");
                    break;
                }
                case PassCommand.BeginRenderPass:
                {
                    var cmd = (BeginRenderPassCommand*)baseCommand;
                    Debug.Log($"BeginRenderPass {cmd->w}x{cmd->h}x{cmd->s}s using {cmd->numAttachments} attachments in {cmd->numSubpasses} passes");
                    break;
                }
                case PassCommand.NextSubPass:
                {
                    Debug.Log($"NextSubPass");
                    break;
                }
                case PassCommand.EndRenderPass:
                {
                    Debug.Log($"EndRenderPAss");
                    break;
                }
                case PassCommand.ExecuteNode:
                {
                    var cmd = (ExecuteNodeCommand*)baseCommand;
                    Debug.Log($"ExecuteNode {state.passes[cmd->passID].name}");
                    break;
                }
                case PassCommand.BeginAsyncCompute:
                {
                    Debug.Log("Begin Async CS");
                    break;
                }
                case PassCommand.EndAsyncCompute:
                {
                    Debug.Log("End Async CS");
                    break;
                }
                case PassCommand.InsertFence:
                {
                    var cmd = (InsertGraphicsFenceCommand*)baseCommand;
                    Debug.Log($"Inserting Fence for pass {cmd->passID}");
                    break;
                }
                case PassCommand.WaitOnFence:
                {
                    var cmd = (WaitOnGraphicsFenceCommand*)baseCommand;
                    Debug.Log($"Waiting on fence inserted at pass {cmd->passID}");
                    break;
                }
                case PassCommand.SetRandomWriteTarget:
                {
                    var cmd = (SetRandomWriteTargetCommand*)baseCommand;
                    Debug.Log($"Setting random write target {cmd->Index}");
                    break;
                }
                case PassCommand.ClearRandomWriteTargets:
                {
                    var cmd = (ClearRandomWriteTargetsCommand*)baseCommand;
                    Debug.Log($"Clearing random write targets");
                    break;
                }
                default:
                    Debug.LogError($"Unknown command {baseCommand->CommandType}");
                    break;

            }
        }
    }

    internal struct PassCommandBuffer<T> : IDisposable
        where T : IExecutionPolicy
    {
        PassCommandBufferData m_Data;
        PassCommandBufferState m_State;
        internal T m_ExecutionPolicy;

        public void Dispose()
        {
            m_Data.Dispose();
        }

        internal void Reset() => m_Data.Reset();

        internal void Execute(InternalRenderGraphContext rgContext, RenderGraphResourceRegistry resources, in List<RenderGraphPass> passes)
        {
            try
            {
                m_State.Reset(rgContext, resources, passes);
                unsafe
                {
                    var chunk = m_Data.chain.m_Head;
                    while (chunk != null && chunk->Used > 0)
                    {
                        var buf = (byte*)chunk + sizeof(PassCommandBufferData.Chunk);
                        var off = 0;
                        while (off < chunk->Used)
                        {
                            var ptr = buf + off;
                            var header = (BasicCommand*)ptr;
                            m_ExecutionPolicy.ExecuteInternal(header, ref m_State);
                            off += header->TotalSize;
                        }
                        chunk = chunk->Next;
                    }

                }
            }
            finally
            {
                CommandBuffer.ThrowOnSetRenderTarget = false;
            }
        }

        internal void CreateResource(ResourceHandle h, bool clear) => ResourceLifetimeCommand.AddCreateCommand(h, clear, ref m_Data);
        internal void ReleaseResource(ResourceHandle h) => ResourceLifetimeCommand.AddReleaseCommand(h, ref m_Data);
        internal void BeginRenderPass(int w, int h, int d, int s,
            ref FixedAttachmentArray<NativePassAttachment> attachments, int attachmentCount, bool hasDepth, ref NativeList<SubPassDescriptor> passes, int passOffset, int passCount, DynamicArray<Name> passNamesForDebug = null)
            => BeginRenderPassCommand.AddCommand(w, h, d, s, ref attachments, attachmentCount, hasDepth, ref passes, passOffset, passCount, ref m_Data, passNamesForDebug);
        internal void EndRenderPass() => EndRenderPassCommand.AddCommand(ref m_Data);
        internal void NextSubPass() => NextSubpassCommand.AddCommand(ref m_Data);
        internal void ExecuteGraphNode(int passID) => ExecuteNodeCommand.AddCommand(passID, ref m_Data);

        internal void BeginAsyncCompute() => BeginAsyncComputeCommand.AddCommand(ref m_Data);
        internal void EndAsyncCompute() => EndAsyncComputeCommand.AddCommand(ref m_Data);
        internal void InsertGraphicsFence(int passID) => InsertGraphicsFenceCommand.AddCommand(passID, ref m_Data);
        internal void WaitOnGraphicsFence(int passID) => WaitOnGraphicsFenceCommand.AddCommand(passID, ref m_Data);

        internal void SetRandomWriteTarget(int index, ResourceHandle h) => SetRandomWriteTargetCommand.AddCommand(index, h, ref m_Data);
        internal void ClearRandomWriteTargets() => ClearRandomWriteTargetsCommand.AddCommand(ref m_Data);
    }
}
