using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace RenderGraph
{
    public struct GraphBuilder
    {
        public Resource<AttachmentIdentifier> CreateAttachment(RenderTextureFormat format)
        {
            throw new System.NotImplementedException();
        }

        public T AddNode<T>(T node) where T : struct, IRenderNode
        {
            throw new NotImplementedException();
        }

        public RenderEdge<AttachmentIdentifier, AttachmentIdentifier> Connect(Resource<AttachmentIdentifier> from, Resource<AttachmentIdentifier> to)
        {
            throw new NotImplementedException();
        }
    }
}
