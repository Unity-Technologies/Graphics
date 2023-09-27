using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// ContextContainer is a Dictionary like storage where the key is a generic parameter and the value is of the same type.
    /// </summary>
    public class ContextContainer : IDisposable
    {
        Item[] m_Items = new Item[64];
        List<uint> m_ActiveItemIndices = new();

        /// <summary>
        /// Retrives a T of class <c>ContextContainerItem</c> if it was previously created without it being disposed.
        /// </summary>
        /// <typeparam name="T">Is the class which you are trying to fetch. T has to inherit from <c>ContextContainerItem</c></typeparam>
        /// <returns>The value created previously using <![CDATA[Create<T>]]> .</returns>
        /// <exception cref="InvalidOperationException">This is thown if the value isn't previously created.</exception>
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

        /// <summary>
        /// Creates the value of type T.
        /// </summary>
        /// <typeparam name="T">Is the class which you are trying to fetch. T has to inherit from <c>ContextContainerItem</c></typeparam>
         /// <returns>The value of type T created inside the <c>ContextContainer</c>.</returns>
        /// <exception cref="InvalidOperationException">Thown if you try to create the value of type T agian after it is already created.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if CONTEXT_CONTAINER_ALLOCATOR_DEBUG
        public T Create<T>([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
#else
        public T Create<T>()
#endif
            where T : ContextItem, new()
        {
            var typeId = TypeId<T>.value;
            if (Contains(typeId))
            {
#if CONTEXT_CONTAINER_ALLOCATOR_DEBUG
                throw new InvalidOperationException($"Type {typeof(T).FullName} has already been created. It was previously created in member {m_Items[typeId].memberName} at line {m_Items[typeId].lineNumber} in {m_Items[typeId].filePath}.");
#else
                throw new InvalidOperationException($"Type {typeof(T).FullName} has already been created.");
#endif
            }

#if CONTEXT_CONTAINER_ALLOCATOR_DEBUG
            return CreateAndGetData<T>(typeId, lineNumber, memberName, filePath);
#else
            return CreateAndGetData<T>(typeId);
#endif
        }

        /// <summary>
        /// Creates the value of type T if the value is not previously created otherwise try to get the value of type T.
        /// </summary>
        /// <typeparam name="T">Is the class which you are trying to fetch. T has to inherit from <c>ContextContainerItem</c></typeparam>
        /// <returns>Returns the value of type T which is created or retrived.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if CONTEXT_CONTAINER_ALLOCATOR_DEBUG
        public T GetOrCreate<T>([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
#else
        public T GetOrCreate<T>()
#endif
            where T : ContextItem, new()
        {
            var typeId = TypeId<T>.value;
            if (Contains(typeId))
            {
                return (T) m_Items[typeId].storage;
            }


#if CONTEXT_CONTAINER_ALLOCATOR_DEBUG
            return CreateAndGetData<T>(typeId, lineNumber, memberName, filePath);
#else
            return CreateAndGetData<T>(typeId);
#endif
        }

        /// <summary>
        /// Check if the value of type T has previously been created.
        /// </summary>
        /// <typeparam name="T">Is the class which you are trying to fetch. T has to inherit from <c>ContextContainerItem</c></typeparam>
        /// <returns>Returns true if the value exists and false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains<T>()
            where T : ContextItem, new()
        {
            var typeId = TypeId<T>.value;
            return Contains(typeId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Contains(uint typeId) => typeId < m_Items.Length && m_Items[typeId].isSet;

#if CONTEXT_CONTAINER_ALLOCATOR_DEBUG
        T CreateAndGetData<T>(uint typeId, int lineNumber, string memberName, string filePath)
#else
        T CreateAndGetData<T>(uint typeId)
#endif
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
#if CONTEXT_CONTAINER_ALLOCATOR_DEBUG
            item.lineNumber = lineNumber;
            item.memberName = memberName;
            item.filePath = filePath;
#endif

            return (T)item.storage;
        }

        /// <summary>
        /// Call Dispose to remove the created values.
        /// </summary>
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
#if CONTEXT_CONTAINER_ALLOCATOR_DEBUG
            public int lineNumber;
            public string memberName;
            public string filePath;
#endif
        }

    }

    /// <summary>
    /// This is needed to add the data to <c>ContextContainer</c> and will control how the data are removed when calling Dispose on the <c>ContextContainer</c>.
    /// </summary>
    public abstract class ContextItem
    {
        /// <summary>
        /// Resets the object so it can be used as a new instance next time it is created.
        /// To avoid memory allocations and generating garbage, the system reuses objects.
        /// This function should clear the object so it can be reused without leaking any
        /// information (e.g. pointers to objects that will no longer be valid to access).
        /// So it is important the implementation carefully clears all relevant members.
        /// Note that this is different from a Dispose or Destructor as the object in not
        /// freed but reset. This can be useful when havin large sub-allocated objects like
        /// arrays or lists which can be cleared and reused without re-allocating.
        /// </summary>
        public abstract void Reset();
    }
}
