using System.Collections.Generic;
using UnityEngine.Events;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Command Buffer Pool
    /// </summary>
    public static class CommandBufferPool
    {
        static ObjectPool<CommandBuffer> s_BufferPool = new ObjectPool<CommandBuffer>(null, x => x.Clear());

        /// <summary>
        /// Get a new Command Buffer.
        /// </summary>
        /// <returns></returns>
        public static CommandBuffer Get()
        {
            var cmd = s_BufferPool.Get();
            cmd.name = "Unnamed Command Buffer";
            return cmd;
        }

        /// <summary>
        /// Get a new Command Buffer and assign a name to it.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static CommandBuffer Get(string name)
        {
            var cmd = s_BufferPool.Get();
            cmd.name = name;
            return cmd;
        }

        /// <summary>
        /// Release a Command Buffer.
        /// </summary>
        /// <param name="buffer"></param>
        public static void Release(CommandBuffer buffer)
        {
            s_BufferPool.Release(buffer);
        }
    }
}
