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

        public void PrepareCull(Camera camera, ReflectionProbeCullResults results)
        {
            UpdateAllPlanarReflectionProbeBounds();

            var cullingGroup = new CullingGroup();
            cullingGroup.targetCamera = camera;
            cullingGroup.SetBoundingSpheres(m_PlanarReflectionProbeBoundsArray);
            cullingGroup.SetBoundingSphereCount(m_PlanarReflectionProbeBounds.Count);

            results.PrepareCull(cullingGroup, m_PlanarReflectionProbesArray);
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
                    probe.realtimeTexture = NewRenderTarget(probe);
                }
            }

            // 2. Render
            for (var i = 0; i < length; i++)
            {
                var probe = m_PlanarReflectionProbe_RealtimeUpdate_WorkArray[i];
                Render(probe, probe.realtimeTexture);
            }
        }

        public RenderTexture NewRenderTarget(PlanarReflectionProbe probe)
        {
            var desc = GetRenderHDCamera(probe).renderTextureDesc;
            desc.width = m_Parameters.planarReflectionProbeSize;
            desc.height = m_Parameters.planarReflectionProbeSize;
            desc.colorFormat = RenderTextureFormat.ARGBHalf;
            var rt = new RenderTexture(desc);
            return rt;
        }

        public float GetCaptureCameraFOVFor(PlanarReflectionProbe probe)
        {
            switch (probe.influenceVolume.shapeType)
            {
                case ShapeType.Box:
                {
                    var captureToWorld = Matrix4x4.TRS(probe.capturePosition, probe.captureRotation, Vector3.one);
                    var influenceToWorld = Matrix4x4.TRS(probe.transform.TransformPoint(probe.influenceVolume.boxBaseOffset), probe.transform.rotation, Vector3.one);
                    var influenceToCapture = captureToWorld.inverse * influenceToWorld;
                    var min = influenceToCapture.MultiplyPoint(-probe.influenceVolume.boxBaseSize * 0.5f);
                    var max = influenceToCapture.MultiplyPoint(probe.influenceVolume.boxBaseSize * 0.5f);
                    var minAngle = Mathf.Atan2(Mathf.Sqrt(min.x * min.x + min.y * min.y), min.z) * Mathf.Rad2Deg;
                    var maxAngle = Mathf.Atan2(Mathf.Sqrt(max.x * max.x + max.y * max.y), max.z) * Mathf.Rad2Deg;
                    return Mathf.Max(minAngle, maxAngle) * 2;
                }
                default:
                    throw new NotImplementedException();
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
            var renderCamera = GetRenderHDCamera(probe);
            renderCamera.camera.targetTexture = target;

            SetupCameraForRender(renderCamera.camera, probe);

            renderCamera.camera.Render();
            renderCamera.camera.targetTexture = null;
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

        void SetupCameraForRender(Camera camera, PlanarReflectionProbe probe)
        {
            var ctr = camera.transform;
            ctr.position = probe.capturePosition;
            ctr.rotation = probe.captureRotation;

            camera.fieldOfView = GetCaptureCameraFOVFor(probe);
            camera.aspect = 1;
            camera.nearClipPlane = probe.captureNearPlane;
            camera.farClipPlane = probe.captureFarPlane;
        }

        static HDCamera GetRenderHDCamera(PlanarReflectionProbe probe)
        {
            var camera = GetRenderCamera();

            probe.frameSettings.CopyTo(s_RenderCameraData.GetFrameSettings());

            return HDCamera.Get(camera, null, probe.frameSettings);
        }

        static Camera GetRenderCamera()
        {
            if (s_RenderCamera == null)
            {
                var go = GameObject.Find("__Probe Render Camera") ?? new GameObject("__Probe Render Camera");
                go.hideFlags = HideFlags.HideAndDontSave;

                s_RenderCamera = go.GetComponent<Camera>();
                if (s_RenderCamera == null || s_RenderCamera.Equals(null))
                    s_RenderCamera = go.AddComponent<Camera>();

                s_RenderCameraData = go.GetComponent<HDAdditionalCameraData>();
                if (s_RenderCameraData == null || s_RenderCameraData.Equals(null))
                    s_RenderCameraData = go.AddComponent<HDAdditionalCameraData>();

                go.SetActive(false);
            }

            return s_RenderCamera;
        }
    }
}
