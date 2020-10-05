using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    // A collection of profiling samplers, cache, helpers and convenience functions for Universal profiling.
    // Keeps most of the profiling related things organized in one place.
    // Static global state, mostly read/sometimes write, non-thread safe (like most things in Universal).
    //
    // Examples:
    // Single line scopes with cached & static samplers:
    // using var profScope = UniversalProfiling.GetCpuScope(myStaticSampler);
    // using var profScope = UniversalProfiling.GetCpuScope(URPProfileId.DrawOpaqueObjects);  // Tracked by perf framework
    // using var profScope = UniversalProfiling.GetCpuScope(MethodBase.GetCurrentMethod());
    // using var profScope = UniversalProfiling.GetCpuScope("example name");
    // using var profScope = UniversalProfiling.GetGpuCpuScope(cmd, "example gpu name");
    // Scoped scope with a static sampler:
    // using( UniversalProfiling.GetGpuCpuScope( cmd, UniversalProfiling.Pipeline.ExampleStaticSampler ) ) { ... }

    internal static class UniversalProfiling
    {
        private static Dictionary<string, ProfilingSampler> m_StringSamplerCache = new Dictionary<string, ProfilingSampler>();
        private static Dictionary<int, ProfilingSampler>    m_HashSamplerCache   = new Dictionary<int, ProfilingSampler>();

        // A static (fast) fallback profiling name.
        public static readonly ProfilingSampler m_UnknownSampler = new ProfilingSampler("Unknown");

        static UniversalProfiling()
        {
            // TODO: with Net 5.0 support
            // m_StringSamplerCache.EnsureCapacity(64);
        }

        public static void Clear()
        {
            m_StringSamplerCache.Clear();
            m_HashSamplerCache.Clear();
        }

        // Creates a ProfilingScope for immediate CPU profiling only.
        // "Inl_" CPU time outside of any command buffers.
        public static ProfilingScope GetCpuScope(ProfilingSampler scopeSampler)
        {
            return new ProfilingScope(null, scopeSampler);
        }

        // Creates a ProfilingScope for GPU and CPU profiling into a command buffer.
        // Which show the GPU and the CPU time for executing the command buffer.
        // Also creates "Inl_" CPU time scope outside of the command buffer.
        public static ProfilingScope GetGpuCpuScope(CommandBuffer cmd, ProfilingSampler scopeSampler)
        {
            return new ProfilingScope(cmd, scopeSampler);
        }

        // Creates an enum named "immediate" CPU scope.
        // Use 'URPProfileId' enum type to track the scope in the performance test framework.
        public static ProfilingScope GetCpuScope<TEnum>(TEnum scopeId)
            where TEnum : Enum
        {
            return new ProfilingScope(null, ProfilingSampler.Get(scopeId));
        }

        // Creates an enum named GPU + CPU scope into a command buffer.
        // Use 'URPProfileId' enum type to track the scope in the performance test framework.
        public static ProfilingScope GetGpuCpuScope<TEnum>(CommandBuffer cmd, TEnum scopeId)
            where TEnum : Enum
        {
            return new ProfilingScope(cmd, ProfilingSampler.Get(scopeId));
        }

        // Creates a cached string named "immediate" CPU scope
        public static ProfilingScope GetCpuScope(string scopeName)
        {
            return new ProfilingScope(null, TryGetOrAddSampler(scopeName));
        }

        // Creates a cached string named GPU + CPU scope into a command buffer
        public static ProfilingScope GetGpuCpuScope(CommandBuffer cmd, string scopeName)
        {
            return new ProfilingScope(cmd, TryGetOrAddSampler(scopeName));
        }

        // Creates a cached "immediate" CPU scope using a reflected method
        public static ProfilingScope GetCpuScope(System.Reflection.MethodBase method)
        {
            return new ProfilingScope(null, TryGetOrAddSampler(method));
        }

        // Creates a cached GPU + CPU scope into a command buffer using a reflected method
        public static ProfilingScope GetGpuCpuScope(CommandBuffer cmd, System.Reflection.MethodBase method)
        {
            return new ProfilingScope(cmd, TryGetOrAddSampler(method));
        }

        // Creates a cached CPU "immediate" scope using a a generic hash key and two-part dynamic name of form "categoryName.detailName".
        public static ProfilingScope GetCpuScope(int hashKey, string categoryName, string detailName)
        {
            return new ProfilingScope(null, TryGetOrAddSampler(hashKey, categoryName, detailName));
        }

        // Creates a cached GPU + CPU command-buffer scope using a generic hash key and two-part dynamic name of form "categoryName.detailName".
        public static ProfilingScope GetGpuCpuScope(CommandBuffer cmd, int hashKey, string categoryName, string detailName)
        {
            return new ProfilingScope(cmd, TryGetOrAddSampler(hashKey, categoryName, detailName));
        }

        // Get a cached ProfilingSampler or add into the cache if not found.
        // NOTE:
        // Caching has a tiny overhead (hashing).
        // Typically no allocations, but might allocate once on a cache miss for Add operation.
        public static ProfilingSampler TryGetOrAddSampler(string samplerName)
        {
            //#define UNIVERSAL_PROFILING_MINIMAL
            #if UNIVERSAL_PROFILING_MINIMAL
                // Disable caching and return a static sampler for all scopes.
                // Useful for testing hashing impact
                return m_UnknownSampler;
            #else
                ProfilingSampler ps = null;
                bool exists = m_StringSamplerCache.TryGetValue(samplerName, out ps);
                if (!exists)
                {
                    ps = new ProfilingSampler(samplerName);
                    m_StringSamplerCache.Add(samplerName, ps);
                }

                return ps;
            #endif
        }

        // Get a cached ProfilingSampler, or add into the cache if not found, using a hash value for any object.
        // Adds a sampler with a name of form `categoryName + dynamicName` using a generic key value.
        // NOTE:
        // Caching has some overhead (hashing (twice)).
        // Typically no allocations, but might allocate once on a cache miss for Add operation.
        //
        // <param name"hashKey"> Generic integer used as the cache key. Typically a hashed value.
        // <param name"categoryName"> Shared part of the dynamic name in "CategoryName" + "DetailName".
        // <param name"detailName"> Unique detail part of the dynamic name in "CategoryName" + "DetailName".
        public static ProfilingSampler TryGetOrAddSampler(int hashKey, string categoryName, string detailName)
        {
            ProfilingSampler ps = null;
            bool exists = m_HashSamplerCache.TryGetValue(hashKey, out ps);
            if (!exists)
            {
                ps = new ProfilingSampler( categoryName + "." + detailName);
                m_HashSamplerCache.Add(hashKey, ps);
            }

            return ps;
        }

        // Get a cached ProfilingSampler from a reflected method
        // For example: using var profScope = UniversalProfiling.GetCPUScope(MethodBase.GetCurrentMethod());
        public static ProfilingSampler TryGetOrAddSampler(System.Reflection.MethodBase method)
        {
            ProfilingSampler ps = null;
            var hashKey = method.GetHashCode();
            bool exists = m_HashSamplerCache.TryGetValue(hashKey, out ps);
            if (!exists)
            {
                // NOTE: everything except hashKey in methodBase allocates!
                var type = method.ReflectedType;
                ps = new ProfilingSampler( (type == null ? "" : type.Name) + "." + method.Name);
                m_HashSamplerCache.Add(hashKey, ps);
            }

            return ps;
        }

        // Specialization for camera loop to avoid allocations.
        public static ProfilingSampler TryGetOrAddCameraSampler(Camera camera)
        {
            ProfilingSampler ps = null;
            int cameraId = camera.GetHashCode();
            bool exists = m_HashSamplerCache.TryGetValue(cameraId, out ps);
            if (!exists)
            {
                // NOTE: camera.name allocates!
                ps = new ProfilingSampler( nameof(UniversalRenderPipeline.RenderSingleCamera) + ": " + camera.name);
                m_HashSamplerCache.Add(cameraId, ps);
            }

            return ps;
        }

        // Static samplers for absolute control & speed. No allocations, no overhead.
        // Suitable for loops or repeatedly called functions with a static name.
        // Roughly categorized using structs.
        public static class Pipeline
        {
            const string Name = nameof(UniversalRenderPipeline);
            public static readonly ProfilingSampler BeginFrameRendering  = new ProfilingSampler("BeginFrameRendering");
            public static readonly ProfilingSampler EndFrameRendering    = new ProfilingSampler("EndFrameRendering");
            public static readonly ProfilingSampler BeginCameraRendering = new ProfilingSampler("BeginCameraRendering");
            public static readonly ProfilingSampler EndCameraRendering   = new ProfilingSampler("EndCameraRendering");

        public static class Renderer
        {
                const string Name = nameof(ScriptableRenderer);
                public static readonly ProfilingSampler SetupCullingParameters = new ProfilingSampler($"{Name}.{nameof(ScriptableRenderer.SetupCullingParameters)}");
                public static readonly ProfilingSampler Setup                  = new ProfilingSampler($"{Name}.{nameof(ScriptableRenderer.Setup)}");
        };

        public static class Context
        {
                const string Name = nameof(Context);
                public static readonly ProfilingSampler Submit = new ProfilingSampler($"{Name}.{nameof(ScriptableRenderContext.Submit)}");
        };

        public static class XR
        {
            public static readonly ProfilingSampler MirrorView = new ProfilingSampler("XR Mirror View");
        };
        };

        public static class Renderer
        {
        public static class RenderPass
        {
                const string Name = nameof(ScriptableRenderPass);
                public static readonly ProfilingSampler Configure = new ProfilingSampler($"{Name}.{nameof(ScriptableRenderPass.Configure)}");
            }
        }
    }
}
