using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace UnityEngine.Rendering.RenderGraphModule
{
    // RendererList is a different case so not represented here.
    internal enum RenderGraphResourceType
    {
        Texture = 0,
        Buffer,
        AccelerationStructure,
        Count
    }

    // For performance reasons, ResourceHandle is readonly.
    // To update an existing instance with a new version, recreate it using its copy constructor
    internal readonly struct ResourceHandle : IEquatable<ResourceHandle>
    {
        // Note on handles validity.
        // PassData classes used during render graph passes are pooled and because of that, when users don't fill them completely,
        // they can contain stale handles from a previous render graph execution that could still be considered valid if we only checked the index.
        // In order to avoid using those, we incorporate the execution index in a 16 bits hash to make sure the handle is coming from the current execution.
        // If not, it's considered invalid.
        // We store this validity mask in the upper 16 bits of the index.
        const uint kValidityMask = 0xFFFF0000;
        const uint kIndexMask = 0xFFFF;

        private readonly uint m_Value;
        private readonly int m_Version;
        private readonly RenderGraphResourceType m_Type;

        static uint s_CurrentValidBit = 1 << 16;
        static uint s_SharedResourceValidBit = 0x7FFF << 16;

        public int index
        { 
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return (int)(m_Value & kIndexMask); }
        }
        public int iType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return (int)type; }
        }
        public int version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return m_Version; }
        }
        public RenderGraphResourceType type
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return m_Type; }
        }

        internal ResourceHandle(int value, RenderGraphResourceType type, bool shared)
        {
            Debug.Assert(value <= 0xFFFF);
            m_Value = ((uint)value & kIndexMask) | (shared ? s_SharedResourceValidBit : s_CurrentValidBit);
            m_Type = type;
            m_Version = -1;
        }

        internal ResourceHandle(in ResourceHandle h, int version)
        {
            this.m_Value = h.m_Value;
            this.m_Type = h.type;
            this.m_Version = version;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid()
        {
            var validity = m_Value & kValidityMask;
            return validity != 0 && (validity == s_CurrentValidBit || validity == s_SharedResourceValidBit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNull()
        {
            if (index == 0)
            {
                // Make sure everything is zero
                Debug.Assert(m_Value == 0);
                Debug.Assert(m_Version == 0);
                return true;
            }
            return false;
        }

        static public void NewFrame(int executionIndex)
        {
            uint previousValidBit = s_CurrentValidBit;
            // Scramble frame count to avoid collision when wrapping around.
            s_CurrentValidBit = (uint)(((executionIndex >> 16) ^ (executionIndex & 0xffff) * 58546883) << 16);
            // In case the current valid bit is 0, even though perfectly valid, 0 represents an invalid handle, hence we'll
            // trigger an invalid state incorrectly. To account for this, we actually skip 0 as a viable s_CurrentValidBit and
            // start from 1 again.
            // In the same spirit, s_SharedResourceValidBit is reserved for shared textures so we should never use it otherwise
            // resources could be considered valid at frame N+1 (because shared) even though they aren't.
            if (s_CurrentValidBit == 0 || s_CurrentValidBit == s_SharedResourceValidBit)
            {
                // We need to make sure we don't pick the same value twice.
                uint value = 1;
                while (previousValidBit == (value << 16))
                    value++;
                s_CurrentValidBit = (value << 16);
            }
        }

        public bool IsVersioned
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return m_Version >= 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ResourceHandle hdl)
        {
            return hdl.m_Value == this.m_Value && hdl.m_Version == this.m_Version && hdl.type == this.type;
        }
    }

    class IRenderGraphResource
    {
        public bool imported;
        public bool shared;
        public bool sharedExplicitRelease;
        public bool requestFallBack;
        public bool forceRelease;
        public uint writeCount;
        public uint readCount;
        public int cachedHash;
        public int transientPassIndex;
        public int sharedResourceLastFrameUsed;
        public int version;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Reset(IRenderGraphResourcePool _ = null)
        {
            imported = false;
            shared = false;
            sharedExplicitRelease = false;
            cachedHash = -1;
            transientPassIndex = -1;
            sharedResourceLastFrameUsed = -1;
            requestFallBack = false;
            forceRelease = false;
            writeCount = 0;
            readCount = 0;
            version = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual string GetName()
        {
            return "";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool IsCreated()
        {
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void IncrementWriteCount()
        {
            writeCount++;
        }

        // readCount is currently not used in the HDRP Compiler.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void IncrementReadCount()
        {
            readCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual int NewVersion()
        {
            version++;
            return version;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool NeedsFallBack()
        {
            return requestFallBack && writeCount == 0;
        }

        public virtual void CreatePooledGraphicsResource() { }
        public virtual void CreateGraphicsResource() { }
        public virtual void UpdateGraphicsResource() { }
        public virtual void ReleasePooledGraphicsResource(int frameIndex) { }
        public virtual void ReleaseGraphicsResource() { }
        public virtual void LogCreation(RenderGraphLogger logger) { }
        public virtual void LogRelease(RenderGraphLogger logger) { }
        public virtual int GetSortIndex() { return 0; }
        public virtual int GetDescHashCode() { return 0; }
    }

    [DebuggerDisplay("Resource ({GetType().Name}:{GetName()})")]
    abstract class RenderGraphResource<DescType, ResType>
        : IRenderGraphResource
        where DescType : struct
        where ResType : class
    {
        public DescType desc;
        public bool validDesc; // Does the descriptor contain valid data (this is not always the case for imported resources)
        public ResType graphicsResource;

        protected RenderGraphResourcePool<ResType> m_Pool;

        protected RenderGraphResource()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Reset(IRenderGraphResourcePool pool = null)
        {
            base.Reset();
            m_Pool = pool as RenderGraphResourcePool<ResType>;
            graphicsResource = null;
            validDesc = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool IsCreated()
        {
            return graphicsResource != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void ReleaseGraphicsResource()
        {
            graphicsResource = null;
        }

        public override void CreatePooledGraphicsResource()
        {
            Debug.Assert(m_Pool != null, "RenderGraphResource: CreatePooledGraphicsResource should only be called for regular pooled resources");

            int hashCode = GetDescHashCode();

            if (graphicsResource != null)
                throw new InvalidOperationException($"RenderGraphResource: Trying to create an already created resource ({GetName()}). Resource was probably declared for writing more than once in the same pass.");

            // If the pool doesn't have any available resource that we can use, we will create one
            // In any case, we will update the graphicsResource name based on the RenderGraph resource name
            if (!m_Pool.TryGetResource(hashCode, out graphicsResource))
            {
                CreateGraphicsResource();
            }
            else
            {
                UpdateGraphicsResource();
            }

            cachedHash = hashCode;
            m_Pool.RegisterFrameAllocation(cachedHash, graphicsResource);
        }

        public override void ReleasePooledGraphicsResource(int frameIndex)
        {
            if (graphicsResource == null)
                throw new InvalidOperationException($"RenderGraphResource: Tried to release a resource ({GetName()}) that was never created. Check that there is at least one pass writing to it first.");

            // Shared resources don't use the pool
            if (m_Pool != null)
            {
                m_Pool.ReleaseResource(cachedHash, graphicsResource, frameIndex);
                m_Pool.UnregisterFrameAllocation(cachedHash, graphicsResource);
            }

            Reset();
        }
    }
}
