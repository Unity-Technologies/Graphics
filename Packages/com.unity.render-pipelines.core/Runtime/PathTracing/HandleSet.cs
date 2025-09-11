using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.PathTracing.Core
{
    // The type parameter T is only used as a tag, preventing different kinds of handles from being mixed together.
    internal readonly struct Handle<T>
    {
        public readonly UInt64 Value;

        public Handle(UInt64 value)
        {
            Value = value;
        }

        public static readonly Handle<T> Invalid = new(0xFFFFFFFFFFFFFFFF);

        public bool IsValid() => Value >= 0;

        internal int ToInt()
        {
            Debug.Assert(UnsafeUtility.SizeOf<EntityId>() == sizeof(int),
                "If this assert is firing, the size of EntityId has changed. This function should no longer be used.");

            return (int)Value;
        }

        // Value type semantics
        public override int GetHashCode() => Value.GetHashCode();
        public override bool Equals(object obj) => obj is Handle<T> other && other.Value == Value;
        public override string ToString() => $"Handle<{typeof(T).Name}>({Value})";
        public static bool operator ==(Handle<T> a, Handle<T> b) => a.Value == b.Value;
        public static bool operator !=(Handle<T> a, Handle<T> b) => a.Value != b.Value;
    }

    // Keeps track of allocated instance handles. Reuses freed handles.
    internal class HandleSet<T>
    {
        private readonly Stack<Handle<T>> _freeHandles = new();
        private UInt64 _nextHandleIndex;

        public Handle<T> Add()
        {
            if (_freeHandles.Count > 0)
                return _freeHandles.Pop();

            return new Handle<T>(_nextHandleIndex++);
        }

        public void Remove(Handle<T> handle)
        {
            Debug.Assert(!_freeHandles.Contains(handle));
            _freeHandles.Push(handle);
        }

        public void Clear()
        {
            _freeHandles.Clear();
            _nextHandleIndex = 0;
        }
    }
}
