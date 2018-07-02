using System;

namespace RenderGraph
{
    public interface IVirtualResource {}

    public struct VirtualResource<T> : IVirtualResource
    {
    }
}
