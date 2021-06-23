using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    public class BlackBoardContent
    {
    }

    public class RenderGraphBlackBoard
    {
        Dictionary<Type, BlackBoardContent> m_Data = new Dictionary<Type, BlackBoardContent>();

        public T Add<T>() where T : BlackBoardContent, new()
        {
            if (m_Data.TryGetValue(typeof(T), out var value))
            {
                return value as T;
            }
            else
            {
                var newValue = new T();
                m_Data[typeof(T)] = newValue;
                return newValue;
            }
        }

        public T Get<T>() where T : BlackBoardContent, new()
        {
            if (m_Data.TryGetValue(typeof(T), out var value))
            {
                return value as T;
            }
            else
            {
                throw new InvalidOperationException($"Requested black board content of type {typeof(T)} is unavailable.");
            }
        }

        public void Clear()
        {
            m_Data.Clear();
        }
    }
}
