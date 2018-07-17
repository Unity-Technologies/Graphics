using System;
using ICSharpCode.NRefactory.Ast;

namespace RenderGraph
{
    public interface IResource {}

    public struct Resource<T> : IResource
    {
        int id;

        public static implicit operator Resource<T>(int id)
        {
            return new Resource<T> { id = id };
        }
    }
}
