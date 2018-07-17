using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace RenderGraph
{
    public struct ResourceContext
    {
        public T Get<T>(Resource<T> resource)
        {
            throw new NotImplementedException();
        }
    }
}
