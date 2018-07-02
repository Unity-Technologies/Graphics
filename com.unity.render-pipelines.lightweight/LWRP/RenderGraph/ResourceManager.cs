using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace RenderGraph
{
    public struct ResourceManager
    {
        public T Get<T>(VirtualResource<T> virtualResource)
        {
            throw new NotImplementedException();
        }

        public void Create(VirtualResource<AttachmentIdentifier> virtualResource, RenderTextureFormat format)
        {
            throw new NotImplementedException();
        }
    }
}
