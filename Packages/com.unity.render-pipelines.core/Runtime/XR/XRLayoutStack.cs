using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace UnityEngine.Experimental.Rendering
{
    internal class XRLayoutStack : IDisposable
    {
        readonly Stack<XRLayout> m_Stack = new ();

        public XRLayout New()
        {
            GenericPool<XRLayout>.Get(out var layout);
            m_Stack.Push(layout);
            return layout;
        }

        public XRLayout top => m_Stack.Peek();

        public void Release()
        {
            if (!m_Stack.TryPop(out var value))
                throw new InvalidOperationException($"Calling {nameof(Release)} without calling {nameof(New)} first.");

            value.Clear();
            GenericPool<XRLayout>.Release(value);
        }

        public void Dispose()
        {
            if (m_Stack.Count != 0)
                throw new Exception($"Stack is not empty. Did you skip a call to {nameof(Release)}?");
        }
    }
}
