using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    class ObjectPool<T> where T : new()
    {
        private readonly Stack<T> m_Stack = new Stack<T>();
        private readonly List<T> m_Loaned = new List<T>();
        private readonly UnityAction<T> m_ActionOnGet;
        private readonly UnityAction<T> m_ActionOnRelease;

        public ObjectPool(UnityAction<T> actionOnGet, UnityAction<T> actionOnRelease)
        {
            m_ActionOnGet = actionOnGet;
            m_ActionOnRelease = actionOnRelease;
        }

        public T Get()
        {
            T element;
            if (m_Stack.Count == 0)
            {
                element = new T();
            }
            else
            {
                element = m_Stack.Pop();
            }
            m_Loaned.Add(element);
            if (m_ActionOnGet != null)
                m_ActionOnGet(element);
            return element;
        }

        public void ReleaseAll()
        {
            foreach (var element in m_Loaned)
            {
                if (m_ActionOnRelease != null)
                    m_ActionOnRelease(element);
                m_Stack.Push(element);
            }
            m_Loaned.Clear();
        }
    }
    
    public class CommandBufferPool
    {
        private static ObjectPool<CommandBuffer> m_BufferPool = new ObjectPool<CommandBuffer>(null, x => x.Clear());

        public static CommandBuffer Get()
        {
            var cmd = m_BufferPool.Get();
            cmd.name = "Unnamed Command Buffer";
            return cmd;
        }
        public static CommandBuffer Get(string name)
        {
            var cmd = m_BufferPool.Get();
            cmd.name = name;
            return cmd;
        }

        public static void EndOfFrame()
        {
            m_BufferPool.ReleaseAll();
        }
    }
}
