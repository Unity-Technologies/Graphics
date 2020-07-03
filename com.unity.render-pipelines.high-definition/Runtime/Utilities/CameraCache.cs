using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Persistent camera cache for a specific key.
    ///
    /// Use this to have access to a stable HDCamera over frame. Usually you need this when the
    /// history buffers of the HDCamera are used.
    /// </summary>
    /// <typeparam name="K">The type of the key.</typeparam>
    class CameraCache<K>: IDisposable
    {
        Dictionary<K, (Camera camera, int lastFrame)> m_Cache = new Dictionary<K, (Camera camera, int lastFrame)>();
        K[] cameraKeysCache = new K[0];

        /// <summary> Get or create a camera for the specified key </summary>
        /// <param name="key">The key to look at.</param>
        /// <param name="frameCount">
        /// The current frame count.
        ///
        /// This frame count is assigned to the returned camera to know the age of its last use.
        /// </param>
        /// <returns>
        /// The cached camera if the key was found,
        /// otherwise a new camera that was inserted in the cache during the call.
        /// </returns>
        public Camera GetOrCreate(K key, int frameCount)
        {
            if (m_Cache == null)
                throw new ObjectDisposedException(nameof(CameraCache<K>));

            if (!m_Cache.TryGetValue(key, out var camera) || camera.camera == null || camera.camera.Equals(null))
            {
                camera = (new GameObject().AddComponent<Camera>(), frameCount);
                m_Cache[key] = camera;
            }
            else
            {
                camera.lastFrame = Time.frameCount;
                m_Cache[key] = camera;
            }
            return camera.camera;
        }

        /// <summary> Destroy all cameras that are unused more than <paramref name="frameWindow"/>. </summary>
        /// <param name="frameWindow">The age of the cameras to keep.</param>
        /// <param name="frameCount">The current frame count. Usually <see cref="Time.frameCount"/>.</param>
        public void ClearCamerasUnusedFor(int frameWindow, int frameCount)
        {
            if (m_Cache == null)
                throw new ObjectDisposedException(nameof(CameraCache<K>));
            
            // In case cameraKeysCache length does not matches the current cache length, we resize it:
            if (cameraKeysCache.Length != m_Cache.Count)
                cameraKeysCache = new K[m_Cache.Count];
            
            // Copy keys to remove them from the dictionary (avoids collection modifed while iterating error)
            m_Cache.Keys.CopyTo(cameraKeysCache, 0);
            foreach (var key in cameraKeysCache)
            {
                m_Cache.TryGetValue(key, out var value);
                if ((frameCount - value.lastFrame) > frameWindow)
                {
                    CoreUtils.Destroy(value.camera.gameObject);
                    m_Cache.Remove(key);
                }
            }
        }

        /// <summary>Destroy all cameras in the cache.</summary>
        public void Clear()
        {
            if (m_Cache == null)
                throw new ObjectDisposedException(nameof(CameraCache<K>));

            foreach (var pair in m_Cache)
                CoreUtils.Destroy(pair.Value.camera.gameObject);
            m_Cache.Clear();
        }

        public void Dispose()
        {
            Clear();
            m_Cache = null;
        }
    }
}
