#if SURFACE_CACHE_SUPPORTED || UNITY_EDITOR

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.Universal
{
    public partial class SurfaceCacheGIRendererFeature : ISerializationCallbackReceiver
    {
        private enum Version
        {
            // Set only on newly created objects, or on objects serialized before m_Version existed. The two cases are
            // told apart by whether this value is first seen during serialization or during deserialization.
            Uninitialised = -1,

            Initial,
            AddMaterialResolution,

            Count,
            Latest = Count - 1,
        }

        [SerializeField]
        [HideInInspector]
        private Version m_Version = Version.Uninitialised;

        private void UpgradeIfNeeded()
        {
#if UNITY_EDITOR
            if (m_Version == Version.Latest)
                return;

            if (m_Version == Version.Initial)
            {
                m_MaterialResolution = MaterialResolution.VeryHigh;
                m_Version++;
            }

            EditorApplication.delayCall += () =>
            {
                if (this)
                    EditorUtility.SetDirty(this);
            };
#endif
        }

        /// <inheritdoc/>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (m_Version == Version.Uninitialised)
                m_Version = Version.Latest;
        }

        /// <inheritdoc/>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (m_Version == Version.Uninitialised)
                m_Version = Version.Initial;

            UpgradeIfNeeded();
        }
    }
}

#endif
