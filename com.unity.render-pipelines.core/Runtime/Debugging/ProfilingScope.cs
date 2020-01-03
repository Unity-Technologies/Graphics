// TProfilingSampler<TEnum>.samples should just be an array. Unfortunately, Enum cannot be converted to int without generating garbage.
// This could be worked around by using Unsafe but it's not available at the moment.
// So in the meantime we use a Dictionary with a perf hit...
//#define USE_UNSAFE

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Profiling;

// TODO: See with Julien, currently our graphic test crash if recorder a enable on test 2204 (but only when run with other test
// in isolation, test run correctly. Commenting the 5 first PRofiling scope of the lightloop.cs file prevent the crash).
//#if UNITY_2020_1_OR_NEWER
#if false
#define UNITY_USE_RECORDER
#endif

namespace UnityEngine.Rendering
{
    class TProfilingSampler<TEnum> : ProfilingSampler where TEnum : Enum
    {
#if USE_UNSAFE
        internal static TProfilingSampler<TEnum>[] samples;
#else
        internal static Dictionary<TEnum, TProfilingSampler<TEnum>> samples = new Dictionary<TEnum, TProfilingSampler<TEnum>>();
#endif
        static TProfilingSampler()
        {
            var names = Enum.GetNames(typeof(TEnum));
#if USE_UNSAFE
            var values = Enum.GetValues(typeof(TEnum)).Cast<int>().ToArray();
            samples = new TProfilingSampler<TEnum>[values.Max() + 1];
#else
            var values = Enum.GetValues(typeof(TEnum));
#endif

            for (int i = 0; i < names.Length; i++)
            {
                var sample = new TProfilingSampler<TEnum>(names[i]);
#if USE_UNSAFE
                samples[values[i]] = sample;
#else
                samples.Add((TEnum)values.GetValue(i), sample);
#endif
            }
        }

        public TProfilingSampler(string name)
            : base(name)
        {
        }
    }

    /// <summary>
    /// Wrapper around CPU and GPU profiling samplers.
    /// Use this along ProfilingScope to profile a piece of code.
    /// </summary>
    public class ProfilingSampler
    {
        public static ProfilingSampler Get<TEnum>(TEnum marker)
            where TEnum : Enum
        {
#if USE_UNSAFE
            return TProfilingSampler<TEnum>.samples[Unsafe.As<TEnum, int>(ref marker)];
#else
            TProfilingSampler<TEnum>.samples.TryGetValue(marker, out var sampler);
            return sampler;
#endif
        }

        public ProfilingSampler(string name)
        {
            sampler = CustomSampler.Create(name, true); // Event markers, command buffer CPU profiling and GPU profiling
            inlineSampler = CustomSampler.Create($"Inl_{name}"); // Profiles code "immediately"
            this.name = name;

#if UNITY_USE_RECORDER
            m_Recorder = sampler.GetRecorder();
            m_Recorder.enabled = false;
            m_InlineRecorder = inlineSampler.GetRecorder();
            m_InlineRecorder.enabled = false;
#endif
        }

        internal bool IsValid() { return (sampler != null && inlineSampler != null); }

        internal CustomSampler sampler { get; private set; }
        internal CustomSampler inlineSampler { get; private set; }
        /// <summary>
        /// Name of the Profiling Sampler
        /// </summary>
        public string name { get; private set; }

#if UNITY_USE_RECORDER
        Recorder m_Recorder;
        Recorder m_InlineRecorder;
#endif

        /// <summary>
        /// Set to true to enable recording of profiling sampler timings.
        /// </summary>
        public bool enableRecording
        {
            set
            {
                #if UNITY_USE_RECORDER
                m_Recorder.enabled = value;
                m_InlineRecorder.enabled = value;
                #endif
            }
        }

#if UNITY_USE_RECORDER
        /// <summary>
        /// GPU Elapsed time in milliseconds.
        /// </summary>
        public float gpuElapsedTime => m_Recorder.enabled ? m_Recorder.gpuElapsedNanoseconds / 1000000.0f : 0.0f;
        /// <summary>
        /// Number of times the Profiling Sampler has hit on the GPU
        /// </summary>
        public int gpuSampleCount => m_Recorder.enabled ? m_Recorder.gpuSampleBlockCount : 0;
        /// <summary>
        /// CPU Elapsed time in milliseconds (Command Buffer execution).
        /// </summary>
        public float cpuElapsedTime => m_Recorder.enabled ? m_Recorder.elapsedNanoseconds / 1000000.0f : 0.0f;
        /// <summary>
        /// Number of times the Profiling Sampler has hit on the CPU in the command buffer.
        /// </summary>
        public int cpuSampleCount => m_Recorder.enabled ? m_Recorder.sampleBlockCount : 0;
        /// <summary>
        /// CPU Elapsed time in milliseconds (Direct execution).
        /// </summary>
        public float inlineCpuElapsedTime => m_InlineRecorder.enabled ? m_InlineRecorder.elapsedNanoseconds / 1000000.0f : 0.0f;
        /// <summary>
        /// Number of times the Profiling Sampler has hit on the CPU.
        /// </summary>
        public int inlineCpuSampleCount => m_InlineRecorder.enabled ? m_InlineRecorder.sampleBlockCount : 0;
#else
        /// <summary>
        /// GPU Elapsed time in milliseconds.
        /// </summary>
        public float gpuElapsedTime => 0.0f;
        /// <summary>
        /// Number of times the Profiling Sampler has hit on the GPU
        /// </summary>
        public int gpuSampleCount => 0;
        /// <summary>
        /// CPU Elapsed time in milliseconds (Command Buffer execution).
        /// </summary>
        public float cpuElapsedTime => 0.0f;
        /// <summary>
        /// Number of times the Profiling Sampler has hit on the CPU in the command buffer.
        /// </summary>
        public int cpuSampleCount => 0;
        /// <summary>
        /// CPU Elapsed time in milliseconds (Direct execution).
        /// </summary>
        public float inlineCpuElapsedTime => 0.0f;
        /// <summary>
        /// Number of times the Profiling Sampler has hit on the CPU.
        /// </summary>
        public int inlineCpuSampleCount => 0;
#endif
        // Keep the constructor private
        ProfilingSampler() { }
    }

    /// <summary>
    /// Scoped Profiling markers
    /// </summary>
#if DEVELOPMENT_BUILD || UNITY_EDITOR
    public struct ProfilingScope : IDisposable
    {
        string          m_Name;
        CommandBuffer   m_Cmd;
        bool            m_Disposed;
        CustomSampler   m_Sampler;
        CustomSampler   m_InlineSampler;

        /// <summary>
        /// Profiling Scope constructor
        /// </summary>
        /// <param name="cmd">Command buffer used to add markers and compute execution timings.</param>
        /// <param name="sampler">Profiling Sampler to be used for this scope.</param>
        public ProfilingScope(CommandBuffer cmd, ProfilingSampler sampler)
        {
            m_Name = sampler.name; // Don't use CustomSampler.name because it causes garbage
            m_Cmd = cmd;
            m_Disposed = false;
            m_Sampler = sampler.sampler;
            m_InlineSampler = sampler.inlineSampler;

            if (cmd != null)
                cmd.BeginSample(m_Sampler);
            m_InlineSampler?.Begin();
        }

        /// <summary>
        ///  Dispose pattern implementation
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
                if (m_Cmd != null)
                    m_Cmd.EndSample(m_Sampler);
                m_InlineSampler?.End();
            }

            m_Disposed = true;
        }
}
#else
    public struct ProfilingScope : IDisposable
    {
        public ProfilingScope(CommandBuffer cmd, ProfilingSampler sampler)
        {

        }

        public void Dispose()
        {
        }
    }
#endif


    [System.Obsolete("Please use ProfilingScope")]
    public struct ProfilingSample : IDisposable
    {
        readonly CommandBuffer m_Cmd;
        readonly string m_Name;

        bool m_Disposed;
        CustomSampler m_Sampler;

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
        public ProfilingSample(CommandBuffer cmd, string format, object arg) : this(cmd, string.Format(format, arg))
        {
        }

        // Shortcut to string.Format() with variable amount of arguments - for performance critical
        // code you should pre-build & cache the marker name instead of using this
        public ProfilingSample(CommandBuffer cmd, string format, params object[] args) : this(cmd, string.Format(format, args))
        {
        }

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
