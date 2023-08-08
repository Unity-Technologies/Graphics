using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    internal class ContextContainer : IDisposable
    {
        Item[] m_Items = new Item[64];
        List<uint> m_ActiveItemIndices = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>()
            where T : ContextItem, new()
        {
            var typeId = TypeId<T>.value;
            if (!Contains(typeId))
            {
                throw new InvalidOperationException($"Type {typeof(T).FullName} has not been created yet.");
            }

            return (T) m_Items[typeId].storage;

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Create<T>([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
            where T : ContextItem, new()
        {
            var typeId = TypeId<T>.value;
            if (Contains(typeId))
            {
                throw new InvalidOperationException($"Type {typeof(T).FullName} has already been created. It was previously created in member {m_Items[typeId].memberName} at line {m_Items[typeId].lineNumber} in {m_Items[typeId].filePath}.");
            }

            return CreateAndGetData<T>(typeId, lineNumber, memberName, filePath);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetOrCreate<T>([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
            where T : ContextItem, new()
        {
            var typeId = TypeId<T>.value;
            if (Contains(typeId))
            {
                return (T) m_Items[typeId].storage;
            }

            return CreateAndGetData<T>(typeId, lineNumber, memberName, filePath);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains<T>()
            where T : ContextItem, new()
        {
            var typeId = TypeId<T>.value;
            return Contains(typeId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Contains(uint typeId) => typeId < m_Items.Length && m_Items[typeId].isSet;

        T CreateAndGetData<T>(uint typeId, int lineNumber, string memberName, string filePath)
            where T : ContextItem, new()
        {
            if (m_Items.Length <= typeId)
            {
                var items = new Item[math.max(math.ceilpow2(s_TypeCount), m_Items.Length * 2)];
                for (var i = 0; i < m_Items.Length; i++)
                {
                    items[i] = m_Items[i];
                }
                m_Items = items;
            }

            m_ActiveItemIndices.Add(typeId);
            ref var item = ref m_Items[typeId];
            item.storage ??= new T();
            item.isSet = true;
            item.lineNumber = lineNumber;
            item.memberName = memberName;
            item.filePath = filePath;

            return (T)item.storage;
        }

        public void Dispose()
        {
            foreach (var index in m_ActiveItemIndices)
            {
                ref var item = ref m_Items[index];
                item.storage.Reset();
                item.isSet = false;
            }

            m_ActiveItemIndices.Clear();
        }

        static uint s_TypeCount;

        static class TypeId<T>
        {
            public static uint value = s_TypeCount++;
        }

        struct Item
        {
            public ContextItem storage;
            public bool isSet;
            public int lineNumber;
            public string memberName;
            public string filePath;
        }

    }

    abstract class ContextItem
    {
        public abstract void Reset();
    }
}
