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
        HashSet<PlanarReflectionProbe> m_PlanarReflectionProbe_PerCamera_RealtimeUpdate;
        PlanarReflectionProbe[] m_PlanarReflectionProbe_RealtimeUpdate_WorkArray;

        Dictionary<PlanarReflectionProbe, BoundingSphere> m_PlanarReflectionProbeBounds;
        PlanarReflectionProbe[] m_PlanarReflectionProbesArray;
        BoundingSphere[] m_PlanarReflectionProbeBoundsArray;

        ReflectionSystemParameters m_Parameters;

        public ReflectionSystemInternal(ReflectionSystemParameters parameters, ReflectionSystemInternal previous)
        {
            m_Parameters = parameters;

            // Runtime collections
            m_PlanarReflectionProbeBounds = new Dictionary<PlanarReflectionProbe, BoundingSphere>(parameters.maxActivePlanarReflectionProbe);
            m_PlanarReflectionProbesArray = new PlanarReflectionProbe[parameters.maxActivePlanarReflectionProbe];
            m_PlanarReflectionProbeBoundsArray = new BoundingSphere[parameters.maxActivePlanarReflectionProbe];
            m_PlanarReflectionProbe_RealtimeUpdate_WorkArray = new PlanarReflectionProbe[parameters.maxPlanarReflectionProbePerCamera];

            // Persistent collections
            m_PlanarReflectionProbes = new HashSet<PlanarReflectionProbe>();
            m_PlanarReflectionProbe_DirtyBounds = new HashSet<PlanarReflectionProbe>();
            m_PlanarReflectionProbe_RequestRealtimeRender = new HashSet<PlanarReflectionProbe>();
            m_PlanarReflectionProbe_RealtimeUpdate = new HashSet<PlanarReflectionProbe>();
            m_PlanarReflectionProbe_PerCamera_RealtimeUpdate = new HashSet<PlanarReflectionProbe>();

            if (previous != null)
            {
                m_PlanarReflectionProbes.UnionWith(previous.m_PlanarReflectionProbes);
                m_PlanarReflectionProbe_DirtyBounds.UnionWith(m_PlanarReflectionProbes);
                m_PlanarReflectionProbe_RequestRealtimeRender.UnionWith(previous.m_PlanarReflectionProbe_RequestRealtimeRender);
                m_PlanarReflectionProbe_RealtimeUpdate.UnionWith(previous.m_PlanarReflectionProbe_RealtimeUpdate);
                m_PlanarReflectionProbe_PerCamera_RealtimeUpdate.UnionWith(previous.m_PlanarReflectionProbe_PerCamera_RealtimeUpdate);
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
                    {
                        switch (planarProbe.capturePositionMode)
                        {
                            case PlanarReflectionProbe.CapturePositionMode.Static:
                                m_PlanarReflectionProbe_RealtimeUpdate.Add(planarProbe);
                                break;
                            case PlanarReflectionProbe.CapturePositionMode.MirrorCamera:
                                m_PlanarReflectionProbe_PerCamera_RealtimeUpdate.Add(planarProbe);
                                break;
                        }
                        break;
                    }
                }
            }
        }

        public void UnregisterProbe(PlanarReflectionProbe planarProbe)
        {
            m_PlanarReflectionProbes.Remove(planarProbe);
            m_PlanarReflectionProbeBounds.Remove(planarProbe);
            m_PlanarReflectionProbe_DirtyBounds.Remove(planarProbe);
            m_PlanarReflectionProbe_RequestRealtimeRender.Remove(planarProbe);
            m_PlanarReflectionProbe_RealtimeUpdate.Remove(planarProbe);
            m_PlanarReflectionProbe_PerCamera_RealtimeUpdate.Remove(planarProbe);
        }

        public void PrepareCull(Camera camera, ReflectionProbeCullResults results)
        {
            UpdateAllPlanarReflectionProbeBounds();

            var cullingGroup = new CullingGroup();
            cullingGroup.targetCamera = camera;
            cullingGroup.SetBoundingSpheres(m_PlanarReflectionProbeBoundsArray);
            cullingGroup.SetBoundingSphereCount(Mathf.Min(m_PlanarReflectionProbeBounds.Count, m_PlanarReflectionProbeBoundsArray.Length));

            results.PrepareCull(cullingGroup, m_PlanarReflectionProbesArray);
        }

        public void RenderAllRealtimeProbesFor(ReflectionProbeType probeType, Camera viewerCamera)
        {
            if ((probeType & ReflectionProbeType.PlanarReflection) != 0)
            {
                var length = Mathf.Min(m_PlanarReflectionProbe_PerCamera_RealtimeUpdate.Count, m_PlanarReflectionProbe_RealtimeUpdate_WorkArray.Length);
                var index = 0;
                foreach (var p in m_PlanarReflectionProbe_PerCamera_RealtimeUpdate)
                {
                    m_PlanarReflectionProbe_RealtimeUpdate_WorkArray[index] = p;
                    if (++index >= length)
                        break;
                }
#if DEBUG
                var discarded = m_PlanarReflectionProbe_PerCamera_RealtimeUpdate.Count - length;
                if (discarded > 0)
                    Debug.LogWarningFormat("There are more planar probe than supported in a single rendering, {0} probes discardeds", discarded);
#endif

                // 1. Allocate if necessary target texture
                for (var i = 0; i < length; i++)
                {
                    var probe = m_PlanarReflectionProbe_RealtimeUpdate_WorkArray[i];

                    if (!IsRealtimeTextureValid(probe.realtimeTexture))
                    {
                        if (probe.realtimeTexture != null)
                            probe.realtimeTexture.Release();
                        probe.realtimeTexture = NewRenderTarget(probe);
                    }
                }

                // 2. Render
                for (var i = 0; i < length; i++)
                {
                    var probe = m_PlanarReflectionProbe_RealtimeUpdate_WorkArray[i];
                    Render(probe, probe.realtimeTexture, viewerCamera);
                }
            }
        }

        public void RenderAllRealtimeProbes(ReflectionProbeType probeTypes)
        {
            if ((probeTypes & ReflectionProbeType.PlanarReflection) != 0)
            {
                // Discard disabled probes in requested render probes
                m_PlanarReflectionProbe_RequestRealtimeRender.IntersectWith(m_PlanarReflectionProbes);
                // Include all realtime probe modes
                m_PlanarReflectionProbe_RequestRealtimeRender.UnionWith(m_PlanarReflectionProbe_RealtimeUpdate);
                var length = Mathf.Min(m_PlanarReflectionProbe_RequestRealtimeRender.Count, m_PlanarReflectionProbe_RealtimeUpdate_WorkArray.Length);
                m_PlanarReflectionProbe_RequestRealtimeRender.CopyTo(m_PlanarReflectionProbe_RealtimeUpdate_WorkArray);
                m_PlanarReflectionProbe_RequestRealtimeRender.Clear();

                // 1. Allocate if necessary target texture
                for (var i = 0; i < length; i++)
                {
                    var probe = m_PlanarReflectionProbe_RealtimeUpdate_WorkArray[i];

                    if (!IsRealtimeTextureValid(probe.realtimeTexture))
                    {
                        if (probe.realtimeTexture != null)
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
        }

        public RenderTexture NewRenderTarget(PlanarReflectionProbe probe)
        {
            var rt = new RenderTexture(m_Parameters.planarReflectionProbeSize, m_Parameters.planarReflectionProbeSize, 0, RenderTextureFormat.ARGBHalf);
            // No hide and don't save for this one
            rt.useMipMap = true;
            rt.autoGenerateMips = false;
            rt.name = CoreUtils.GetRenderTargetAutoName(m_Parameters.planarReflectionProbeSize, m_Parameters.planarReflectionProbeSize, 1, RenderTextureFormat.ARGBHalf, "PlanarProbeRT");
            rt.Create();
            return rt;
        }

        //public float GetCaptureCameraFOVFor(PlanarReflectionProbe probe, Camera viewerCamera)
        //{
        //    switch (probe.influenceVolume.shapeType)
        //    {
        //        case ShapeType.Box:
        //        {
        //            var captureToWorld = probe.GetCaptureToWorld(viewerCamera);
        //            var influenceToWorld = Matrix4x4.TRS(probe.transform.TransformPoint(probe.influenceVolume.boxBaseOffset), probe.transform.rotation, Vector3.one);
        //            var influenceToCapture = captureToWorld.inverse * influenceToWorld;
        //            var min = influenceToCapture.MultiplyPoint(-probe.influenceVolume.boxBaseSize * 0.5f);
        //            var max = influenceToCapture.MultiplyPoint(probe.influenceVolume.boxBaseSize * 0.5f);
        //            var minAngle = Mathf.Atan2(Mathf.Sqrt(min.x * min.x + min.y * min.y), min.z) * Mathf.Rad2Deg;
        //            var maxAngle = Mathf.Atan2(Mathf.Sqrt(max.x * max.x + max.y * max.y), max.z) * Mathf.Rad2Deg;
        //            return Mathf.Max(minAngle, maxAngle) * 2;
        //        }
        //        default:
        //            throw new NotImplementedException();
        //    }
        //}

        bool IsRealtimeTextureValid(RenderTexture renderTexture)
        {
            return renderTexture != null
                && renderTexture.width == m_Parameters.planarReflectionProbeSize
                && renderTexture.height == m_Parameters.planarReflectionProbeSize
                && renderTexture.format == RenderTextureFormat.ARGBHalf
                && renderTexture.useMipMap;
        }

        public void RequestRealtimeRender(PlanarReflectionProbe probe)
        {
            m_PlanarReflectionProbe_RequestRealtimeRender.Add(probe);
        }

        public void Render(PlanarReflectionProbe probe, RenderTexture target, Camera viewerCamera = null)
        {
            var renderCamera = GetRenderCamera();

            // Copy current frameSettings of this probe to the HDAdditionalData of the render camera
            probe.frameSettings.CopyTo(s_RenderCameraData.GetFrameSettings());

            renderCamera.targetTexture = target;

            SetupCameraForRender(renderCamera, probe, viewerCamera);
            GL.invertCulling = IsProbeCaptureMirrored(probe, viewerCamera);
            renderCamera.Render();
            GL.invertCulling = false;
            renderCamera.targetTexture = null;
            target.IncrementUpdateCount();
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

                var length = m_PlanarReflectionProbeBoundsArray.Length;
                var index = 0;
                foreach (var k in m_PlanarReflectionProbeBounds)
                {
                    m_PlanarReflectionProbeBoundsArray[index] = k.Value;
                    m_PlanarReflectionProbesArray[index] = k.Key;
                    if (++index >= length)
                        break;
                }
            }
        }

        void UpdatePlanarReflectionProbeBounds(PlanarReflectionProbe planarReflectionProbe)
        {
            m_PlanarReflectionProbeBounds[planarReflectionProbe] = planarReflectionProbe.boundingSphere;
        }

        static void SetupCameraForRender(Camera camera, PlanarReflectionProbe probe, Camera viewerCamera = null)
        {
            float nearClipPlane, farClipPlane, aspect, fov;
            Color backgroundColor;
            CameraClearFlags clearFlags;
            Vector3 capturePosition;
            Quaternion captureRotation;
            Matrix4x4 worldToCamera, projection;

            CalculateCaptureCameraProperties(probe,
                out nearClipPlane, out farClipPlane,
                out aspect, out fov, out clearFlags, out backgroundColor,
                out worldToCamera, out projection,
                out capturePosition, out captureRotation, viewerCamera);

            camera.farClipPlane = farClipPlane;
            camera.nearClipPlane = nearClipPlane;
            camera.fieldOfView = fov;
            camera.aspect = aspect;
            camera.clearFlags = clearFlags;
            camera.backgroundColor = camera.backgroundColor;
            camera.projectionMatrix = projection;
            camera.worldToCameraMatrix = worldToCamera;

            var ctr = camera.transform;
            ctr.position = capturePosition;
            ctr.rotation = captureRotation;
        }

        public static void CalculateCaptureCameraViewProj(PlanarReflectionProbe probe, out Matrix4x4 worldToCamera, out Matrix4x4 projection, out Vector3 capturePosition, out Quaternion captureRotation, Camera viewerCamera = null)
        {
            float nearClipPlane, farClipPlane, aspect, fov;
            CameraClearFlags clearFlags;
            Color backgroundColor;
            CalculateCaptureCameraProperties(
                probe,
                out nearClipPlane, out farClipPlane,
                out aspect, out fov, out clearFlags, out backgroundColor,
                out worldToCamera, out projection, out capturePosition, out captureRotation,
                viewerCamera);
        }

        public static void CalculateCaptureCameraProperties(PlanarReflectionProbe probe, out float nearClipPlane, out float farClipPlane, out float aspect, out float fov, out CameraClearFlags clearFlags, out Color backgroundColor, out Matrix4x4 worldToCamera, out Matrix4x4 projection, out Vector3 capturePosition, out Quaternion captureRotation, Camera viewerCamera = null)
        {
            if (viewerCamera != null
                && probe.mode == ReflectionProbeMode.Realtime
                && probe.refreshMode == ReflectionProbeRefreshMode.EveryFrame
                && probe.capturePositionMode == PlanarReflectionProbe.CapturePositionMode.MirrorCamera)
                CalculateMirroredCaptureCameraProperties(probe, viewerCamera, out nearClipPlane, out farClipPlane, out aspect, out fov, out clearFlags, out backgroundColor, out worldToCamera, out projection, out capturePosition, out captureRotation);
            else
                CalculateStaticCaptureCameraProperties(probe, out nearClipPlane, out farClipPlane, out aspect, out fov, out clearFlags, out backgroundColor, out worldToCamera, out projection, out capturePosition, out captureRotation);
        }

        static bool IsProbeCaptureMirrored(PlanarReflectionProbe probe, Camera viewerCamera)
        {
            return viewerCamera != null
                && probe.mode == ReflectionProbeMode.Realtime
                && probe.refreshMode == ReflectionProbeRefreshMode.EveryFrame
                && probe.capturePositionMode == PlanarReflectionProbe.CapturePositionMode.MirrorCamera;
        }

        static void CalculateStaticCaptureCameraProperties(PlanarReflectionProbe probe, out float nearClipPlane, out float farClipPlane, out float aspect, out float fov, out CameraClearFlags clearFlags, out Color backgroundColor, out Matrix4x4 worldToCamera, out Matrix4x4 projection, out Vector3 capturePosition, out Quaternion captureRotation)
        {
            nearClipPlane = probe.captureNearPlane;
            farClipPlane = probe.captureFarPlane;
            aspect = 1f;
            fov = probe.overrideFieldOfView
                ? probe.fieldOfViewOverride
                : 90f;
            clearFlags = CameraClearFlags.Nothing;
            backgroundColor = Color.white;

            capturePosition = probe.transform.TransformPoint(probe.captureLocalPosition);
            captureRotation = Quaternion.LookRotation((Vector3)probe.influenceToWorld.GetColumn(3) - capturePosition, probe.transform.up);

            worldToCamera = GeometryUtils.CalculateWorldToCameraMatrixRHS(capturePosition, captureRotation);
            var clipPlane = GeometryUtils.CameraSpacePlane(worldToCamera, probe.captureMirrorPlanePosition, probe.captureMirrorPlaneNormal);
            projection = Matrix4x4.Perspective(fov, aspect, nearClipPlane, farClipPlane);
            projection = GeometryUtils.CalculateObliqueMatrix(projection, clipPlane);
        }

        static void CalculateMirroredCaptureCameraProperties(PlanarReflectionProbe probe, Camera viewerCamera, out float nearClipPlane, out float farClipPlane, out float aspect, out float fov, out CameraClearFlags clearFlags, out Color backgroundColor, out Matrix4x4 worldToCamera, out Matrix4x4 projection, out Vector3 capturePosition, out Quaternion captureRotation)
        {
            nearClipPlane = viewerCamera.nearClipPlane;
            farClipPlane = viewerCamera.farClipPlane;
            aspect = 1;
            fov = probe.overrideFieldOfView
                ? probe.fieldOfViewOverride
                : Mathf.Max(viewerCamera.fieldOfView, viewerCamera.fieldOfView * viewerCamera.aspect);
            clearFlags = viewerCamera.clearFlags;
            backgroundColor = viewerCamera.backgroundColor;

            var worldToCapture = GeometryUtils.CalculateWorldToCameraMatrixRHS(viewerCamera.transform);
            var reflectionMatrix = GeometryUtils.CalculateReflectionMatrix(probe.captureMirrorPlanePosition, probe.captureMirrorPlaneNormal);
            worldToCamera = worldToCapture * reflectionMatrix;

            var clipPlane = GeometryUtils.CameraSpacePlane(worldToCamera, probe.captureMirrorPlanePosition, probe.captureMirrorPlaneNormal);
            var sourceProj = Matrix4x4.Perspective(fov, aspect, nearClipPlane, farClipPlane);
            projection = GeometryUtils.CalculateObliqueMatrix(sourceProj, clipPlane);

            capturePosition = reflectionMatrix.MultiplyPoint(viewerCamera.transform.position);

            var forward = reflectionMatrix.MultiplyVector(viewerCamera.transform.forward);
            var up = reflectionMatrix.MultiplyVector(viewerCamera.transform.up);
            captureRotation = Quaternion.LookRotation(forward, up);
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

                // We need to setup cameraType before adding additional camera
                s_RenderCamera.cameraType = CameraType.Reflection;

                s_RenderCameraData = go.GetComponent<HDAdditionalCameraData>();
                if (s_RenderCameraData == null || s_RenderCameraData.Equals(null))
                    s_RenderCameraData = go.AddComponent<HDAdditionalCameraData>();

                go.SetActive(false);
            }

            return s_RenderCamera;
        }
    }
}
