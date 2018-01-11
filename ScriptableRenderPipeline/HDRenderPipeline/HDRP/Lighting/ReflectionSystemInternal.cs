using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline.Internal
{
    class ReflectionSystemInternal
    {
        static Camera s_RenderCamera = null;
        static HDAdditionalCameraData s_RenderCameraData;

        HashSet<PlanarReflectionProbe> m_PlanarReflectionProbes;
        HashSet<PlanarReflectionProbe> m_PlanarReflectionProbe_DirtyBounds;
        HashSet<PlanarReflectionProbe> m_PlanarReflectionProbe_RequestRealtimeRender;
        HashSet<PlanarReflectionProbe> m_PlanarReflectionProbe_RealtimeUpdate;
        PlanarReflectionProbe[] m_PlanarReflectionProbe_RealtimeUpdate_WorkArray;

        Dictionary<PlanarReflectionProbe, BoundingSphere> m_PlanarReflectionProbeBounds;
        PlanarReflectionProbe[] m_PlanarReflectionProbesArray;
        BoundingSphere[] m_PlanarReflectionProbeBoundsArray;

        ReflectionSystemParameters m_Parameters;

        public ReflectionSystemInternal(ReflectionSystemParameters parameters, ReflectionSystemInternal previous)
        {
            m_Parameters = parameters;

            m_PlanarReflectionProbes = new HashSet<PlanarReflectionProbe>();
            m_PlanarReflectionProbe_DirtyBounds = new HashSet<PlanarReflectionProbe>();
            m_PlanarReflectionProbeBounds = new Dictionary<PlanarReflectionProbe, BoundingSphere>(parameters.maxPlanarReflectionProbes);
            m_PlanarReflectionProbesArray = new PlanarReflectionProbe[parameters.maxPlanarReflectionProbes];
            m_PlanarReflectionProbeBoundsArray = new BoundingSphere[parameters.maxPlanarReflectionProbes];
            m_PlanarReflectionProbe_RequestRealtimeRender = new HashSet<PlanarReflectionProbe>();
            m_PlanarReflectionProbe_RealtimeUpdate = new HashSet<PlanarReflectionProbe>();
            m_PlanarReflectionProbe_RealtimeUpdate_WorkArray = new PlanarReflectionProbe[parameters.maxPlanarReflectionProbes];

            if (previous != null)
            {
                m_PlanarReflectionProbes.UnionWith(previous.m_PlanarReflectionProbes);
                m_PlanarReflectionProbe_DirtyBounds.UnionWith(m_PlanarReflectionProbes);
            }
        }

        public void RegisterProbe(PlanarReflectionProbe planarProbe)
        {
            m_PlanarReflectionProbes.Add(planarProbe);
            SetProbeBoundsDirty(planarProbe);

            if (planarProbe.mode == ReflectionProbeMode.Realtime)
            {
                switch (planarProbe.refreshMode)
                {
                    case ReflectionProbeRefreshMode.OnAwake:
                        m_PlanarReflectionProbe_RequestRealtimeRender.Add(planarProbe);
                        break;
                    case ReflectionProbeRefreshMode.EveryFrame:
                        m_PlanarReflectionProbe_RealtimeUpdate.Add(planarProbe);
                        break;
                }
            }
        }

        public void UnregisterProbe(PlanarReflectionProbe planarProbe)
        {
            m_PlanarReflectionProbes.Remove(planarProbe);
            m_PlanarReflectionProbe_DirtyBounds.Remove(planarProbe);
            m_PlanarReflectionProbe_RequestRealtimeRender.Remove(planarProbe);
            m_PlanarReflectionProbe_RealtimeUpdate.Remove(planarProbe);
        }

        public void Cull(Camera camera, ReflectionProbeCullResults results)
        {
            UpdateAllPlanarReflectionProbeBounds();

            var cullingGroup = new CullingGroup();
            cullingGroup.targetCamera = camera;
            cullingGroup.SetBoundingSpheres(m_PlanarReflectionProbeBoundsArray);
            cullingGroup.SetBoundingSphereCount(m_PlanarReflectionProbeBounds.Count);

            results.CullPlanarReflectionProbes(cullingGroup, m_PlanarReflectionProbesArray);
            
            cullingGroup.Dispose();
        }

        public void RenderAllRealtimeProbes()
        {
            // Discard disabled probes in requested render probes
            m_PlanarReflectionProbe_RequestRealtimeRender.IntersectWith(m_PlanarReflectionProbes);
            // Include all realtime probe modes
            m_PlanarReflectionProbe_RequestRealtimeRender.UnionWith(m_PlanarReflectionProbe_RealtimeUpdate);
            var length = Mathf.Min(m_PlanarReflectionProbe_RequestRealtimeRender.Count, m_PlanarReflectionProbe_RealtimeUpdate_WorkArray.Length);
            m_PlanarReflectionProbe_RequestRealtimeRender.CopyTo(m_PlanarReflectionProbe_RealtimeUpdate_WorkArray);
            m_PlanarReflectionProbe_RequestRealtimeRender.Clear();

            // 1. Allocate if necessary target texture
            var camera = GetRenderCamera();
            for (var i = 0; i < length; i++)
            {
                var probe = m_PlanarReflectionProbe_RealtimeUpdate_WorkArray[i];
                var hdCamera = HDCamera.Get(camera, null, probe.frameSettings);
                if (!IsRealtimeTextureValid(probe.realtimeTexture, hdCamera))
                {
                    if( probe.realtimeTexture != null)
                        probe.realtimeTexture.Release();
                    var desc = hdCamera.renderTextureDesc;
                    desc.width = m_Parameters.planarReflectionProbeSize;
                    desc.height = m_Parameters.planarReflectionProbeSize;
                    desc.colorFormat = RenderTextureFormat.ARGBHalf;
                    probe.realtimeTexture = new RenderTexture(desc);
                }
            }

            // 2. Render
            for (var i = 0; i < length; i++)
            {
                var probe = m_PlanarReflectionProbe_RealtimeUpdate_WorkArray[i];
                Render(probe, probe.realtimeTexture);
            }
        }

        bool IsRealtimeTextureValid(RenderTexture renderTexture, HDCamera hdCamera)
        {
            return renderTexture != null
                && renderTexture.width == m_Parameters.planarReflectionProbeSize
                && renderTexture.height == m_Parameters.planarReflectionProbeSize
                && renderTexture.format == RenderTextureFormat.ARGBHalf;
        }

        public void RequestRealtimeRender(PlanarReflectionProbe probe)
        {
            m_PlanarReflectionProbe_RequestRealtimeRender.Add(probe);
        }

        public void Render(PlanarReflectionProbe probe, RenderTexture target)
        {
            var renderCamera = GetRenderCamera(probe);
            renderCamera.targetTexture = target;

            SetupCameraForRender(renderCamera, probe);

            renderCamera.Render();
            renderCamera.targetTexture = null;
        }

        void SetProbeBoundsDirty(PlanarReflectionProbe planarProbe)
        {
            m_PlanarReflectionProbe_DirtyBounds.Add(planarProbe);
        }

        void UpdateAllPlanarReflectionProbeBounds()
        {
            if (m_PlanarReflectionProbe_DirtyBounds.Count > 0)
            {
                m_PlanarReflectionProbe_DirtyBounds.IntersectWith(m_PlanarReflectionProbes);
                foreach (var planarReflectionProbe in m_PlanarReflectionProbe_DirtyBounds)
                    UpdatePlanarReflectionProbeBounds(planarReflectionProbe);

                m_PlanarReflectionProbeBounds.Values.CopyTo(m_PlanarReflectionProbeBoundsArray, 0);
                m_PlanarReflectionProbeBounds.Keys.CopyTo(m_PlanarReflectionProbesArray, 0);
            }
        }

        void UpdatePlanarReflectionProbeBounds(PlanarReflectionProbe planarReflectionProbe)
        {
            m_PlanarReflectionProbeBounds[planarReflectionProbe] = planarReflectionProbe.boundingSphere;
        }

        static void SetupCameraForRender(Camera camera, PlanarReflectionProbe probe)
        {
            camera.transform.position = probe.capturePosition;

            throw new NotImplementedException();
        }

        static Camera GetRenderCamera(PlanarReflectionProbe probe)
        {
            var camera = GetRenderCamera();

            probe.frameSettings.CopyTo(s_RenderCameraData.GetFrameSettings());

            return camera;
        }

        static Camera GetRenderCamera()
        {
            if (s_RenderCamera == null)
            {
                s_RenderCamera = new GameObject("Probe Render Camera").
                    AddComponent<Camera>();
                s_RenderCameraData = s_RenderCamera.gameObject.AddComponent<HDAdditionalCameraData>();
                s_RenderCamera.gameObject.SetActive(false);
            }

            return s_RenderCamera;
        }
    }
}
