using System;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Scoped profiling sample.
    /// </summary>
    public struct ProfilingSample : IDisposable
    {
        readonly CommandBuffer m_Cmd;
        readonly string m_Name;

        bool m_Disposed;
        CustomSampler m_Sampler;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cmd">Command Buffer used for setting up profiling samples.</param>
        /// <param name="name">Name of the profiling sample.</param>
        /// <param name="sampler">Optional CustomSampler for CPU profiling.</param>
        public ProfilingSample(CommandBuffer cmd, string name, CustomSampler sampler = null)
        {
            m_Cmd = cmd;
            m_Name = name;
            m_Disposed = false;
            if (cmd != null && name != "")
                cmd.BeginSample(name);
            m_Sampler = sampler;
            m_Sampler?.Begin();
        }

        // Shortcut to string.Format() using only one argument (reduces Gen0 GC pressure)
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cmd">Command Buffer used for setting up profiling samples.</param>
        /// <param name="format">Format string.</param>
        /// <param name="arg">Format string arguments.</param>
        public ProfilingSample(CommandBuffer cmd, string format, object arg) : this(cmd, string.Format(format, arg))
        {
        }

        // Shortcut to string.Format() with variable amount of arguments - for performance critical
        // code you should pre-build & cache the marker name instead of using this
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cmd">Command Buffer used for setting up profiling samples.</param>
        /// <param name="format">Format string.</param>
        /// <param name="args">Format string arguments.</param>
        public ProfilingSample(CommandBuffer cmd, string format, params object[] args) : this(cmd, string.Format(format, args))
        {
        }

        /// <summary>
        /// Disposable pattern implementation.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        // Protected implementation of Dispose pattern.
        void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            // As this is a struct, it could have been initialized using an empty constructor so we
            // need to make sure `cmd` isn't null to avoid a crash. Switching to a class would fix
            // this but will generate garbage on every frame (and this struct is used quite a lot).
            if (disposing)
            {
                if (m_Cmd != null && m_Name != "")
                    m_Cmd.EndSample(m_Name);
                m_Sampler?.End();
            }

            m_Disposed = true;
        }
    }
}
