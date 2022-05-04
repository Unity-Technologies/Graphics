using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Rendering.HighDefinition
{
    // Multi-layered camera cache for reflection probes.
    // The goal is to keep a pool of camera GameObjects to avoid reallocating them regularly (especially when doing OnDemand updates)
    // On top of that, we keep a map of cameras currently used for a particular probe/face/position tuple. This allows us to keep frame to frame history coherency for real time probes.
    class ProbeCameraCache<K> : IDisposable
    {
        // Pool of cameras
        Stack<Camera> m_CameraPool = new Stack<Camera>();
        // Map of currently used cameras.
        Dictionary<K, (Camera camera, int lastFrame)> m_Cache = new Dictionary<K, (Camera camera, int lastFrame)>();
        // Only used as temporary container.
        K[] m_TempCameraKeysCache = new K[0];

        internal int cachedActiveCameraCount => m_CameraPool.Count;

        // If the key exists, we can reuse the camera. It means we are rendering the same probe/face
        // If it does not, it means we need a new camera. We either get one from the pool or create a new one.
        public Camera GetOrCreate(K key, int frameCount, CameraType cameraType = CameraType.Game)
        {
            if (m_Cache == null)
                throw new ObjectDisposedException(nameof(ProbeCameraCache<K>));

            if (!m_Cache.TryGetValue(key, out var probeCamera) || probeCamera.camera == null || probeCamera.camera.Equals(null))
            {
                // Key isn't currently used, we try to get an existing or new camera from the pool.
                if (m_CameraPool.Count == 0)
                    probeCamera = (new GameObject().AddComponent<Camera>(), frameCount);
                else
                    probeCamera = (m_CameraPool.Pop(), frameCount);
                probeCamera.camera.cameraType = cameraType;
                m_Cache[key] = probeCamera;
            }
            else
            {
                // Key already exists. Just update the current frame index.
                probeCamera.lastFrame = frameCount;
                m_Cache[key] = probeCamera;
            }
            return probeCamera.camera;
        }

        // Release unused camera keys to the pool if they are not used.
        // This does not clear allocations.
        public void ReleaseCamerasUnusedFor(int frameWindow, int frameCount)
        {
            if (m_Cache == null)
                throw new ObjectDisposedException(nameof(ProbeCameraCache<K>));

            // In case cameraKeysCache length does not matches the current cache length, we resize it:
            if (m_TempCameraKeysCache.Length != m_Cache.Count)
                m_TempCameraKeysCache = new K[m_Cache.Count];

            // Copy keys to remove them from the dictionary (avoids collection modified while iterating error)
            m_Cache.Keys.CopyTo(m_TempCameraKeysCache, 0);
            foreach (var key in m_TempCameraKeysCache)
            {
                if (m_Cache.TryGetValue(key, out var value))
                {
                    if (Math.Abs(frameCount - value.lastFrame) > frameWindow)
                    {
                        if (value.camera != null)
                        {
                            m_CameraPool.Push(value.camera);
                        }
                        m_Cache.Remove(key);
                    }
                }
            }
        }

        /// Destroy all cameras in the cache and pool.
        public void Clear()
        {
            if (m_Cache == null)
                throw new ObjectDisposedException(nameof(ProbeCameraCache<K>));

            foreach (var pair in m_Cache)
            {
                if (pair.Value.camera != null)
                    CoreUtils.Destroy(pair.Value.camera.gameObject);
            }
            m_Cache.Clear();
            foreach(var camera in m_CameraPool)
                CoreUtils.Destroy(camera.gameObject);
            m_CameraPool.Clear();
        }

        public void Dispose()
        {
            Clear();
            m_Cache = null;
            m_CameraPool = null;
        }
    }
}
