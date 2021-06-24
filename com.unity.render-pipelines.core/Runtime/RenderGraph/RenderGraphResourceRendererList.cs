using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering.RendererUtils;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    [DebuggerDisplay("RendererList ({handle})")]
    [BurstCompatible]
    public unsafe struct RendererListHandle
    {
        public struct Key : IEquatable<Key>
        {
            public readonly int Handle;

            public Key(int handle) { Handle = handle; }

            public bool Equals(Key other) => Handle == other.Handle;
            [BurstDiscard] public override bool Equals(object obj) => obj is Key other && Equals(other);
            public override int GetHashCode() => Handle;
        }

        internal Key key => new(handle);
        
        [NativeDisableUnsafePtrRestriction] internal readonly RendererListHandleImpl* Ptr;
        private int Handle;

        internal RendererListHandle(RendererListHandleImpl* impl, Key key)
        {
            Ptr = impl;
            *Ptr = default;
            Handle = key.Handle;
        }

        internal void Clear(Key key)
        {
            *Ptr = default;
            Handle = key.Handle;
        }

        internal int handle => Handle;

        public bool IsValid() => Ptr != null;

        public static implicit operator int(RendererListHandle rendererList) => rendererList.Handle;
        public static implicit operator RendererList(RendererListHandle rendererList) => rendererList.IsValid() ? rendererList.Ptr->ResolvedRendererList : default;
        public static implicit operator HW1371_RendererList(RendererListHandle rendererList) => rendererList.IsValid() ? rendererList.Ptr->ResolvedRendererList : default;
    }

    /// <summary>
    /// Renderer List resource handle.
    /// </summary>
    [BurstCompatible]
    public struct RendererListHandleImpl
    {
        internal RendererList ResolvedRendererList;
    }
    
    
    [BurstCompatible]
    public struct HW1371_RendererList
    {
        internal UIntPtr context;
        internal UInt32  index;
        internal UInt32  frame;

        public bool isValid => ((uint)context | index | frame) > 0;

        public static implicit operator RendererList(HW1371_RendererList rl) => UnsafeUtility.As<HW1371_RendererList, RendererList>(ref rl);
        public static implicit operator HW1371_RendererList(RendererList rl) => UnsafeUtility.As<RendererList, HW1371_RendererList>(ref rl);
    }
}
