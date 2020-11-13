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
            // Set to empty on purpose, does not create profiling markers.
            cmd.name = "";
            return cmd;
        }

        /// <summary>
        /// Get a new Command Buffer and assign a name to it.
        /// Named Command Buffers will add profiling makers implicitly for the buffer execution.
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
