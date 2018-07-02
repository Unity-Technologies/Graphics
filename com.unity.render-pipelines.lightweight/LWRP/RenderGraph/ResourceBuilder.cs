using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace RenderGraph
{
    public struct ResourceBuilder
    {
        public VirtualResource<T> Read<T>()
        {
            throw new System.NotImplementedException();
        }

        public VirtualResource<T> Write<T>()
        {
            throw new System.NotImplementedException();
        }

        public VirtualResource<T> Create<T>()
        {
            throw new System.NotImplementedException();
        }
    }
}
