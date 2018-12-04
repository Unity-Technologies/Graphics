using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    internal static class HDProbeSystem
    {
        static HDProbeSystemInternal s_Instance;

        static HDProbeSystem()
        {
            s_Instance = new HDProbeSystemInternal();
            AssemblyReloadEvents.beforeAssemblyReload += DisposeStaticInstance;
        }

        // Don't set the reference to null
        // Only dispose resources
        static void DisposeStaticInstance() => s_Instance.Dispose();

        public static IList<HDProbe> realtimeViewDependentProbes => s_Instance.realtimeViewDependentProbes;
        public static IList<HDProbe> realtimeViewIndependentProbes => s_Instance.realtimeViewIndependentProbes;
        public static IList<HDProbe> bakedProbes => s_Instance.bakedProbes;
    
        public static void RegisterProbe(HDProbe probe) => s_Instance.RegisterProbe(probe);
        public static void UnregisterProbe(HDProbe probe) => s_Instance.UnregisterProbe(probe);
         
        public static void RenderAndUpdateRealtimeRenderDataIfRequired(
            IList<HDProbe> probes,
            Transform viewerTransform
        )
        {
            for (int i = 0; i < probes.Count; ++i)
            {
                var probe = probes[i];
                if (DoesRealtimeProbeNeedToBeUpdated(probe))
                {
                    RenderAndUpdateRealtimeRenderData(probe, viewerTransform);
                    probe.wasRenderedAfterOnEnable = true;
                    probe.lastRenderedFrame = Time.frameCount;
                }
            }
        }

        public static void RenderAndUpdateRealtimeRenderData(
            HDProbe probe, Transform viewerTransform
        )
        {
            var target = CreateAndSetRenderTargetIfRequired(probe, ProbeSettings.Mode.Realtime);
            Render(probe, viewerTransform, target, out HDProbe.RenderData renderData);
            AssignRenderData(probe, renderData, ProbeSettings.Mode.Realtime);
        }

        public static void Render(
            HDProbe probe, Transform viewerTransform,
            Texture outTarget, out HDProbe.RenderData outRenderData,
            bool forceFlipY = false
        )
        {
            var positionSettings = ProbeCapturePositionSettings.ComputeFrom(probe, viewerTransform);
            HDRenderUtilities.Render(
                probe.settings,
                positionSettings,
                outTarget,
                out CameraSettings cameraSettings, out CameraPositionSettings cameraPosition,
                forceFlipY: forceFlipY
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

        public static void PrepareCull(Camera camera, ReflectionProbeCullResults results)
            => s_Instance.PrepareCull(camera, results);

        public static Texture CreateRenderTargetForMode(HDProbe probe, ProbeSettings.Mode targetMode)
        {
            Texture target = null;
            var hd = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
            var settings = probe.settings;
            switch (targetMode)
            {
                case ProbeSettings.Mode.Realtime:
                    {
                        switch (settings.type)
                        {
                            case ProbeSettings.ProbeType.PlanarProbe:
                                target = HDRenderUtilities.CreatePlanarProbeRenderTarget(
                                    (int)hd.renderPipelineSettings.lightLoopSettings.planarReflectionTextureSize
                                );
                                break;
                            case ProbeSettings.ProbeType.ReflectionProbe:
                                target = HDRenderUtilities.CreateReflectionProbeRenderTarget(
                                    (int)hd.renderPipelineSettings.lightLoopSettings.reflectionCubemapSize
                                );
                                break;
                        }
                        break;
                    }
                case ProbeSettings.Mode.Baked:
                case ProbeSettings.Mode.Custom:
                    {
                        switch (settings.type)
                        {
                            case ProbeSettings.ProbeType.PlanarProbe:
                                target = HDRenderUtilities.CreatePlanarProbeRenderTarget(
                                    (int)hd.renderPipelineSettings.lightLoopSettings.planarReflectionTextureSize
                                );
                                break;
                            case ProbeSettings.ProbeType.ReflectionProbe:
                                target = HDRenderUtilities.CreateReflectionProbeTarget(
                                    (int)hd.renderPipelineSettings.lightLoopSettings.reflectionCubemapSize
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

        static bool DoesRealtimeProbeNeedToBeUpdated(HDProbe probe)
        {
            // Discard (real time, every frame) probe already rendered this frame
            // Discard (real time, OnEnable) probe already rendered after on enable
            if (probe.mode == ProbeSettings.Mode.Realtime)
            {
                switch (probe.realtimeMode)
                {
                    case ProbeSettings.RealtimeMode.EveryFrame:
                        return probe.lastRenderedFrame != Time.frameCount;
                    case ProbeSettings.RealtimeMode.OnEnable:
                        return !probe.wasRenderedAfterOnEnable;
                }
            }
            return true;
        }
    }

    class HDProbeSystemInternal : IDisposable
    {
        List<HDProbe> m_BakedProbes = new List<HDProbe>();
        List<HDProbe> m_RealtimeViewDependentProbes = new List<HDProbe>();
        List<HDProbe> m_RealtimeViewIndependentProbes = new List<HDProbe>();
        int m_PlanarProbeCount = 0;
        PlanarReflectionProbe[] m_PlanarProbes = new PlanarReflectionProbe[32];
        BoundingSphere[] m_PlanarProbeBounds = new BoundingSphere[32];
        CullingGroup m_PlanarProbeCullingGroup = new CullingGroup();

        public IList<HDProbe> bakedProbes
        { get { RemoveDestroyedProbes(m_BakedProbes); return m_BakedProbes; } }
        public IList<HDProbe> realtimeViewDependentProbes
        { get { RemoveDestroyedProbes(m_RealtimeViewDependentProbes); return m_RealtimeViewDependentProbes; } }
        public IList<HDProbe> realtimeViewIndependentProbes
        { get { RemoveDestroyedProbes(m_RealtimeViewIndependentProbes); return m_RealtimeViewIndependentProbes; } }

        internal void RegisterProbe(HDProbe probe)
        {
            var settings = probe.settings;
            switch (settings.mode)
            {
                case ProbeSettings.Mode.Baked:
                    if (!m_BakedProbes.Contains(probe))
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
                        // Grow the arrays
                        if (m_PlanarProbeCount == m_PlanarProbes.Length)
                        {
                            Array.Resize(ref m_PlanarProbes, m_PlanarProbes.Length * 2);
                            Array.Resize(ref m_PlanarProbeBounds, m_PlanarProbeBounds.Length * 2);
                        }
                        m_PlanarProbes[m_PlanarProbeCount] = (PlanarReflectionProbe)probe;
                        m_PlanarProbeBounds[m_PlanarProbeCount] = ((PlanarReflectionProbe)probe).boundingSphere;
                        ++m_PlanarProbeCount;
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
            var index = Array.IndexOf(m_PlanarProbes, probe);
            if (index != -1)
            {
                if (index < m_PlanarProbeCount)
                {
                    m_PlanarProbes[index] = m_PlanarProbes[m_PlanarProbeCount - 1];
                    m_PlanarProbeBounds[index] = m_PlanarProbeBounds[m_PlanarProbeCount - 1];
                    m_PlanarProbes[m_PlanarProbeCount - 1] = null;
                }
                --m_PlanarProbeCount;
            }
        }

        internal void PrepareCull(Camera camera, ReflectionProbeCullResults results)
        {
            // Can happens right before a domain reload
            // The CullingGroup is disposed at that point 
            if (m_PlanarProbeCullingGroup == null)
                return;

            RemoveDestroyedProbes(m_PlanarProbes, m_PlanarProbeBounds, ref m_PlanarProbeCount);

            m_PlanarProbeCullingGroup.targetCamera = camera;
            m_PlanarProbeCullingGroup.SetBoundingSpheres(m_PlanarProbeBounds);
            m_PlanarProbeCullingGroup.SetBoundingSphereCount(m_PlanarProbeCount);

            results.PrepareCull(m_PlanarProbeCullingGroup, m_PlanarProbes);
        }

        static void RemoveDestroyedProbes(List<HDProbe> probes)
        {
            for (int i = probes.Count - 1; i >= 0; --i)
            {
                if (probes[i] == null || probes[i].Equals(null))
                    probes.RemoveAt(i);
            }
        }

        static void RemoveDestroyedProbes(PlanarReflectionProbe[] probes, BoundingSphere[] bounds, ref int count)
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
            }
        }

        public void Dispose()
        {
            m_PlanarProbeCullingGroup.Dispose();
            m_PlanarProbeCullingGroup = null;
        }
    }
}
