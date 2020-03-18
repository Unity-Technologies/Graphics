using System.Collections.Generic;
using UnityEngine.Events;

namespace UnityEngine.Rendering
{
    public static class CommandBufferPool
    {
        static ObjectPool<CommandBuffer> s_BufferPool = new ObjectPool<CommandBuffer>(null, x => x.Clear());

        public static CommandBuffer Get()
        {
            var cmd = s_BufferPool.Get();
            cmd.name = "Unnamed Command Buffer";
            return cmd;
        }

        public static CommandBuffer Get(string name)
        {
            var cmd = s_BufferPool.Get();
            cmd.name = name;
            return cmd;
        }

        public static void Release(CommandBuffer buffer)
        {
            s_BufferPool.Release(buffer);
        }
    }
}
