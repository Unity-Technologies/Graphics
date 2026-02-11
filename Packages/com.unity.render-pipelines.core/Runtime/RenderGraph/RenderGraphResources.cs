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
        // In order to avoid using those, we incorporate the execution index in a hash to make sure the handle is coming from the current execution.
        // If not, it's considered invalid.
        // The validity is stored in a separate field, allowing for a full 32-bit validity check. (2^32 =~ 4B (at 60fps it is more than 2 years) 4,294,967,295 unique execution indices before a collision)
        // The m_VersionIndex field contains both index and version:
        // - Index (lower 16 bits = 65,535 unique indices)
        // - Version (upper 16 bits with one for not versioned. 15 bits = 32767 unique versions).
        //   - Bit 31 is used as a "not versioned" flag for faster checking.
        const uint kIndexMask = 0xFFFF;
        const uint kVersionMask = 0x7FFF0000;
        const int kVersionShift = 16;
        const uint kNotVersionedBit = 0x80000000;

        private readonly uint m_VersionIndex;
        private readonly uint m_Validity;
        private readonly RenderGraphResourceType m_Type;

        static uint s_CurrentValidBit = 1;

        public int index
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return (int)(m_VersionIndex & kIndexMask); }
        }
        public int iType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return (int)type; }
        }
        public int version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (m_VersionIndex & kNotVersionedBit) != 0 ? -1 : (int)((m_VersionIndex & kVersionMask) >> kVersionShift);
            }
        }
        public RenderGraphResourceType type
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return m_Type; }
        }

        internal ResourceHandle(int index, RenderGraphResourceType type, bool shared)
        {
            Debug.Assert(index > 0 && index <= 0xFFFF, "ResourceHandle: Invalid index, values should be >0 && <65536");
            m_VersionIndex = ((uint)index & kIndexMask) | kNotVersionedBit;
            m_Validity = s_CurrentValidBit;
            m_Type = type;
        }

        internal ResourceHandle(in ResourceHandle h, int version)
        {
            Debug.Assert(version >= 0 && version <= 0x7FFF, "ResourceHandle: Invalid version, values should be >=0 && <32768");
            uint versionBits = ((uint)version << kVersionShift) & kVersionMask;
            m_VersionIndex = (h.m_VersionIndex & kIndexMask) | versionBits;
            m_Validity = h.m_Validity;
            m_Type = h.type;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid()
        {
            return m_Validity != 0 && (m_Validity == s_CurrentValidBit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNull()
        {
            if (index == 0)
            {
                // Make sure everything is zero
                Debug.Assert(m_VersionIndex == 0);
                Debug.Assert(m_Validity == 0);
                return true;
            }
            return false;
        }

        static public void NewFrame(int executionIndex)
        {
            uint previousValidBit = s_CurrentValidBit;

            var hasher = HashFNV1A32.Create();
            hasher.Append(executionIndex);
            s_CurrentValidBit = (uint)hasher.value;
            // In case the current valid bit is 0, even though perfectly valid, 0 represents an invalid handle, hence we'll
            // trigger an invalid state incorrectly. To account for this, we actually skip 0 as a viable s_CurrentValidBit and
            // start from 1 again.
            if (s_CurrentValidBit == 0)
            {
                // We need to make sure we don't pick the same value twice.
                uint value = 1;
                while (previousValidBit == value)
                    value++;
                s_CurrentValidBit = value;
            }
        }

        public bool IsVersioned
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (m_VersionIndex & kNotVersionedBit) == 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ResourceHandle hdl)
        {
            return hdl.m_VersionIndex == this.m_VersionIndex && hdl.m_Validity == this.m_Validity && hdl.type == this.type;
        }

        public static bool operator ==(ResourceHandle lhs, ResourceHandle rhs) => lhs.Equals(rhs);

        public static bool operator !=(ResourceHandle lhs, ResourceHandle rhs) => !lhs.Equals(rhs);

        public override bool Equals(object obj) => obj is ResourceHandle other && Equals(other);

        public override int GetHashCode()
        {
            var hashCode = HashFNV1A32.Create();
            hashCode.Append(m_VersionIndex);
            hashCode.Append(m_Validity);
            hashCode.Append(m_Type);
            return hashCode.value;
        }
    }

    class IRenderGraphResource
    {
        public bool imported;
        public bool shared;
        public bool sharedExplicitRelease;
        public bool requestFallBack;
        public uint writeCount;
        public uint readCount;
        public int cachedHash;
        public int transientPassIndex;
        public int sharedResourceLastFrameUsed;
        public bool isBackBuffer;

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
            writeCount = 0;
            readCount = 0;
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
        public virtual uint IncrementWriteCount()
        {
            writeCount++;
            return writeCount;
        }

        // readCount is currently not used in the HDRP Compiler.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void IncrementReadCount()
        {
            readCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool NeedsFallBack()
        {
            return requestFallBack && writeCount == 0;
        }

        public virtual void CreatePooledGraphicsResource(bool forceResourceCreation) { }
        public virtual void CreateGraphicsResource() { }
        public virtual void UpdateGraphicsResource() { }
        public virtual void ReleasePooledGraphicsResource(int frameIndex) { }
        public virtual void ReleaseGraphicsResource() { }
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

        public override void CreatePooledGraphicsResource(bool forceResourceCreation)
        {
            Debug.Assert(m_Pool != null, "RenderGraphResource: CreatePooledGraphicsResource should only be called for regular pooled resources");

            int hashCode = GetDescHashCode();

            if (graphicsResource != null)
                throw new InvalidOperationException($"RenderGraphResource: Trying to create an already created resource ({GetName()}). Resource was probably declared for writing more than once in the same pass.");

            // If the pool doesn't have any available resource that we can use, we will create one
            // In any case, we will update the graphicsResource name based on the RenderGraph resource name
            if (forceResourceCreation || !m_Pool.TryGetResource(hashCode, out graphicsResource))
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
