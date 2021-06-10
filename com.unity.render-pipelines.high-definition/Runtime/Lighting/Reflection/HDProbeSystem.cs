using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    internal static class HDProbeSystem
    {
        static HDProbeSystemInternal s_Instance;

        static HDProbeSystem()
        {
            s_Instance = new HDProbeSystemInternal();
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += DisposeStaticInstance;
            UnityEditor.EditorApplication.quitting += DisposeStaticInstance;
#else
            Application.quitting += DisposeStaticInstance;
#endif
        }

        // Don't set the reference to null
        // Only dispose resources
        static void DisposeStaticInstance() => s_Instance.Dispose();

        public static ReflectionSystemParameters Parameters
        {
            get => s_Instance.Parameters;
            set => s_Instance.Parameters = value;
        }

        public static IEnumerable<HDProbe> realtimeViewDependentProbes => s_Instance.realtimeViewDependentProbes;
        public static IEnumerable<HDProbe> realtimeViewIndependentProbes => s_Instance.realtimeViewIndependentProbes;
        public static IEnumerable<HDProbe> bakedProbes => s_Instance.bakedProbes;
        public static int bakedProbeCount => s_Instance.bakedProbeCount;

        public static void RegisterProbe(HDProbe probe) => s_Instance.RegisterProbe(probe);
        public static void UnregisterProbe(HDProbe probe) => s_Instance.UnregisterProbe(probe);

        public static void Render(
            HDProbe probe, Transform viewerTransform,
            Texture outTarget, out HDProbe.RenderData outRenderData,
            bool forceFlipY = false,
            float referenceFieldOfView = 90,
            float referenceAspect = 1
        )
        {
            var positionSettings = ProbeCapturePositionSettings.ComputeFrom(probe, viewerTransform);
            HDRenderUtilities.Render(
                probe.settings,
                positionSettings,
                outTarget,
                out var cameraSettings, out var cameraPosition,
                forceFlipY,
                referenceFieldOfView: referenceFieldOfView,
                referenceAspect: referenceAspect
            );

            outRenderData = new HDProbe.RenderData(cameraSettings, cameraPosition);
        }

        public static void AssignRenderData(
            HDProbe probe,
            HDProbe.RenderData renderData,
            ProbeSettings.Mode targetMode
        )
        {
            switch (targetMode)
            {
                case ProbeSettings.Mode.Baked: probe.bakedRenderData = renderData; break;
                case ProbeSettings.Mode.Custom: probe.customRenderData = renderData; break;
                case ProbeSettings.Mode.Realtime: probe.realtimeRenderData = renderData; break;
            }
        }

        public static HDProbeCullState PrepareCull(Camera camera)
            => s_Instance.PrepareCull(camera);

        public static void QueryCullResults(HDProbeCullState state, ref HDProbeCullingResults results)
            => s_Instance.QueryCullResults(state, ref results);

        public static Texture CreateRenderTargetForMode(HDProbe probe, ProbeSettings.Mode targetMode)
        {
            Texture target = null;
            var hd = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
            var settings = probe.settings;
            switch (targetMode)
            {
                case ProbeSettings.Mode.Realtime:
                {
                    var format = (GraphicsFormat)hd.currentPlatformRenderPipelineSettings.lightLoopSettings.reflectionProbeFormat;

                    switch (settings.type)
                    {
                        case ProbeSettings.ProbeType.PlanarProbe:
                            target = HDRenderUtilities.CreatePlanarProbeRenderTarget(
                                (int)probe.resolution, format
                            );
                            break;
                        case ProbeSettings.ProbeType.ReflectionProbe:
                            target = HDRenderUtilities.CreateReflectionProbeRenderTarget(
                                (int)hd.currentPlatformRenderPipelineSettings.lightLoopSettings.reflectionCubemapSize,
                                format
                            );
                            break;
                    }
                    break;
                }
                case ProbeSettings.Mode.Baked:
                case ProbeSettings.Mode.Custom:
                {
                    // Custom and Baked texture only support float16 for now
                    var format = GraphicsFormat.R16G16B16A16_SFloat;

                    switch (settings.type)
                    {
                        case ProbeSettings.ProbeType.PlanarProbe:
                            target = HDRenderUtilities.CreatePlanarProbeRenderTarget(
                                (int)probe.resolution, format
                            );
                            break;
                        case ProbeSettings.ProbeType.ReflectionProbe:
                            target = HDRenderUtilities.CreateReflectionProbeRenderTarget(
                                (int)hd.currentPlatformRenderPipelineSettings.lightLoopSettings.reflectionCubemapSize, format
                            );
                            break;
                    }
                    break;
                }
            }

            return target;
        }

        static Texture CreateAndSetRenderTargetIfRequired(HDProbe probe, ProbeSettings.Mode targetMode)
        {
            var settings = probe.settings;
            Texture target = probe.GetTexture(targetMode);

            if (target != null)
                return target;

            target = CreateRenderTargetForMode(probe, targetMode);

            probe.SetTexture(targetMode, target);
            return target;
        }
    }

    class HDProbeSystemInternal : IDisposable
    {
        HashSet<HDProbe> m_BakedProbes = new HashSet<HDProbe>();
        HashSet<HDProbe> m_RealtimeViewDependentProbes = new HashSet<HDProbe>();
        HashSet<HDProbe> m_RealtimeViewIndependentProbes = new HashSet<HDProbe>();
        int m_PlanarProbeCount = 0;
        bool m_RebuildPlanarProbeArray;
        HashSet<HDProbe> m_PlanarProbes = new HashSet<HDProbe>();
        PlanarReflectionProbe[] m_PlanarProbesArray = new PlanarReflectionProbe[32];
        BoundingSphere[] m_PlanarProbeBounds = new BoundingSphere[32];
        CullingGroup m_PlanarProbeCullingGroup = new CullingGroup();

        public IEnumerable<HDProbe> bakedProbes
        { get { RemoveDestroyedProbes(m_BakedProbes); return m_BakedProbes; } }
        public IEnumerable<HDProbe> realtimeViewDependentProbes
        { get { RemoveDestroyedProbes(m_RealtimeViewDependentProbes); return m_RealtimeViewDependentProbes; } }
        public IEnumerable<HDProbe> realtimeViewIndependentProbes
        { get { RemoveDestroyedProbes(m_RealtimeViewIndependentProbes); return m_RealtimeViewIndependentProbes; } }

        public int bakedProbeCount => m_BakedProbes.Count;

        public ReflectionSystemParameters Parameters;

        public void Dispose()
        {
            m_PlanarProbeCullingGroup.Dispose();
            m_PlanarProbeCullingGroup = null;
        }

        internal void RegisterProbe(HDProbe probe)
        {
            var settings = probe.settings;
            switch (settings.mode)
            {
                case ProbeSettings.Mode.Baked:
                    m_BakedProbes.Add(probe);
                    break;
                case ProbeSettings.Mode.Realtime:
                    switch (settings.type)
                    {
                        case ProbeSettings.ProbeType.PlanarProbe:
                            if (!m_RealtimeViewDependentProbes.Contains(probe))
                                m_RealtimeViewDependentProbes.Add(probe);
                            break;
                        case ProbeSettings.ProbeType.ReflectionProbe:
                            if (!m_RealtimeViewIndependentProbes.Contains(probe))
                                m_RealtimeViewIndependentProbes.Add(probe);
                            break;
                    }
                    break;
            }

            switch (settings.type)
            {
                case ProbeSettings.ProbeType.PlanarProbe:
                {
                    if (m_PlanarProbes.Add((PlanarReflectionProbe)probe))
                    {
                        // Insert in the array
                        // Grow the arrays
                        if (m_PlanarProbeCount == m_PlanarProbesArray.Length)
                        {
                            Array.Resize(ref m_PlanarProbesArray, m_PlanarProbes.Count * 2);
                            Array.Resize(ref m_PlanarProbeBounds, m_PlanarProbeBounds.Length * 2);
                        }
                        m_PlanarProbesArray[m_PlanarProbeCount] = (PlanarReflectionProbe)probe;
                        m_PlanarProbeBounds[m_PlanarProbeCount] = ((PlanarReflectionProbe)probe).boundingSphere;
                        ++m_PlanarProbeCount;
                    }
                    break;
                }
            }
        }

        internal void UnregisterProbe(HDProbe probe)
        {
            m_BakedProbes.Remove(probe);
            m_RealtimeViewDependentProbes.Remove(probe);
            m_RealtimeViewIndependentProbes.Remove(probe);

            // Remove swap back
            if (m_PlanarProbes.Remove(probe))
            {
                // It is best to rebuild the full array when we need it instead of doing it at each unregister.
                // So we mark it as dirty.
                m_RebuildPlanarProbeArray = true;
            }
        }

        internal HDProbeCullState PrepareCull(Camera camera)
        {
            // Can happens right before a domain reload
            // The CullingGroup is disposed at that point
            if (m_PlanarProbeCullingGroup == null)
                return default;

            RebuildPlanarProbeArrayIfRequired();

            UpdateBoundsAndRemoveDestroyedProbes(m_PlanarProbesArray, m_PlanarProbeBounds, ref m_PlanarProbeCount);

            m_PlanarProbeCullingGroup.targetCamera = camera;
            m_PlanarProbeCullingGroup.SetBoundingSpheres(m_PlanarProbeBounds);
            m_PlanarProbeCullingGroup.SetBoundingSphereCount(m_PlanarProbeCount);

            var stateHash = ComputeStateHashDebug(m_PlanarProbeBounds, m_PlanarProbesArray, m_PlanarProbeCount);

            return new HDProbeCullState(m_PlanarProbeCullingGroup, m_PlanarProbesArray, stateHash);
        }

        void RebuildPlanarProbeArrayIfRequired()
        {
            if (m_RebuildPlanarProbeArray)
            {
                RemoveDestroyedProbes(m_PlanarProbes);

                m_RebuildPlanarProbeArray = false;
                var i = 0;
                foreach (var probe in m_PlanarProbes)
                {
                    m_PlanarProbesArray[i] = (PlanarReflectionProbe)probe;
                    ++i;
                }
                m_PlanarProbeCount = m_PlanarProbes.Count;
            }
        }

        int[] m_QueryCullResults_Indices;
        internal void QueryCullResults(HDProbeCullState state, ref HDProbeCullingResults results)
        {
            Assert.IsNotNull(state.cullingGroup, "Culling was not prepared, please prepare cull before performing it.");
            Assert.IsNotNull(state.hdProbes, "Culling was not prepared, please prepare cull before performing it.");
            var stateHash = ComputeStateHashDebug(m_PlanarProbeBounds, m_PlanarProbesArray, m_PlanarProbeCount);
            Assert.AreEqual(stateHash, state.stateHash, "HDProbes changes since culling was prepared, this will lead to incorrect results.");

            results.Reset();

            Array.Resize(
                ref m_QueryCullResults_Indices,
                Parameters.maxActivePlanarReflectionProbe + Parameters.maxActiveReflectionProbe
            );
            var indexCount = state.cullingGroup.QueryIndices(true, m_QueryCullResults_Indices, 0);
            for (var i = 0; i < indexCount; ++i)
                results.AddProbe(state.hdProbes[m_QueryCullResults_Indices[i]]);
        }

        static void RemoveDestroyedProbes(HashSet<HDProbe> probes)
            => probes.RemoveWhere(p => p == null || p.Equals(null));

        static void UpdateBoundsAndRemoveDestroyedProbes(PlanarReflectionProbe[] probes, BoundingSphere[] bounds, ref int count)
        {
            for (int i = 0; i < count; ++i)
            {
                if (probes[i] == null || probes[i].Equals(null))
                {
                    probes[i] = probes[count - 1];
                    bounds[i] = bounds[count - 1];
                    probes[count - 1] = null;
                    --count;
                }

                if (probes[i])
                {
                    bounds[i] = probes[i].boundingSphere;
                }
            }
        }

        static unsafe Hash128 ComputeStateHashDebug(
            BoundingSphere[] probeBounds,
            HDProbe[] probes,
            int probeCount
        )
        {
#if DEBUG
            var result = new Hash128();
            if (probeBounds != null)
            {
                var h = new Hash128();
                fixed(BoundingSphere* s = &probeBounds[0])
                {
                    var stride = (ulong)UnsafeUtility.SizeOf<BoundingSphere>();
                    var size = stride * (ulong)probeBounds.Length;
                    HashUnsafeUtilities.ComputeHash128(s, size, &h);
                }
                HashUtilities.AppendHash(ref h, ref result);
            }
            if (probes != null)
            {
                var h = new Hash128();
                for (int i = 0; i < probes.Length; ++i)
                {
                    if (probes[i] == null || probes[i].Equals(null))
                        continue;

                    var instanceID = probes[i].GetInstanceID();
                    HashUtilities.ComputeHash128(ref instanceID, ref h);
                    HashUtilities.AppendHash(ref h, ref result);
                }
            }
            return result;
#else
            return default;
#endif
        }
    }
}
