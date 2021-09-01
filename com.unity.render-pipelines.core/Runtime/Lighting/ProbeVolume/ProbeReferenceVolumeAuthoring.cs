using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.IO;
using System;

#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
#endif

namespace UnityEngine.Experimental.Rendering
{
    // TODO: Use this structure in the actual authoring component rather than just a mean to group output parameters.
    internal struct ProbeDilationSettings
    {
        public float dilationDistance;
        public float dilationValidityThreshold;
        public float dilationIterations;
        public bool squaredDistWeighting;
        public float brickSize;   // Not really a dilation setting, but used during dilation.
    }

    internal struct VirtualOffsetSettings
    {
        public bool useVirtualOffset;
        public float outOfGeoOffset;
        public float searchMultiplier;
    }

    [ExecuteAlways]
    [AddComponentMenu("Light/Probe Reference Volume (Experimental)")]
    internal class ProbeReferenceVolumeAuthoring : MonoBehaviour
    {
#if UNITY_EDITOR
        internal static ProbeReferenceVolumeProfile CreateReferenceVolumeProfile(Scene scene, string targetName)
        {
            string path;
            if (string.IsNullOrEmpty(scene.path))
            {
                path = "Assets/";
            }
            else
            {
                var scenePath = Path.GetDirectoryName(scene.path);
                var extPath = scene.name;
                var profilePath = scenePath + Path.DirectorySeparatorChar + extPath;

                if (!AssetDatabase.IsValidFolder(profilePath))
                {
                    var directories = profilePath.Split(Path.DirectorySeparatorChar);
                    string rootPath = "";
                    foreach (var directory in directories)
                    {
                        var newPath = rootPath + directory;
                        if (!AssetDatabase.IsValidFolder(newPath))
                            AssetDatabase.CreateFolder(rootPath.TrimEnd(Path.DirectorySeparatorChar), directory);
                        rootPath = newPath + Path.DirectorySeparatorChar;
                    }
                }

                path = profilePath + Path.DirectorySeparatorChar;
            }

            path += targetName + " Profile.asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            var profile = ScriptableObject.CreateInstance<ProbeReferenceVolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return profile;
        }

#endif

        [SerializeField]
        ProbeReferenceVolumeProfile m_Profile = null;
#if UNITY_EDITOR
        ProbeReferenceVolumeProfile m_PrevProfile = null;
#endif

        internal ProbeReferenceVolumeProfile profile { get { return m_Profile; } }
        internal float brickSize { get { return m_Profile.minBrickSize; } }
        internal float cellSizeInMeters { get { return m_Profile.cellSizeInMeters; } }
        internal int maxSubdivision { get { return m_Profile.maxSubdivision; } }

#if UNITY_EDITOR
        // Dilation
        [SerializeField]
        float m_MaxDilationSampleDistance = 1f;
        [SerializeField]
        int m_DilationIterations = 1;
        [SerializeField]
        bool m_DilationInvSquaredWeight = true;

        [SerializeField]
        bool m_EnableDilation = true;

        // Virtual offset proof of concept.
        [SerializeField]
        bool m_EnableVirtualOffset = true;
        [SerializeField]
        float m_VirtualOffsetGeometrySearchMultiplier = 0.2f;
        [SerializeField]
        float m_VirtualOffsetBiasOutOfGeometry = 0.01f;

        // In some cases Unity will magically popuplate this private field with a correct value even though it should not be serialized.
        // The [NonSerialized] attribute allows to force the asset to be null in case a domain reload happens.
        [NonSerialized]
        ProbeVolumeAsset m_PrevAsset = null;

        [NonSerialized]
        bool m_SentDataToSceneData = false; // TODO: This is temp until we don't have a setting panel.
#endif
        [SerializeField]
        float m_DilationValidityThreshold = 0.25f;

        public ProbeVolumeAsset volumeAsset = null;

        internal void LoadProfileInformation()
        {
            if (m_Profile == null)
                return;

            var refVol = ProbeReferenceVolume.instance;
            refVol.SetTRS(Vector3.zero, Quaternion.identity, m_Profile.minBrickSize);
            refVol.SetMaxSubdivision(m_Profile.maxSubdivision);
            refVol.dilationValidtyThreshold = m_DilationValidityThreshold;
        }

        // TEMP! THIS NEEDS TO BE REMOVED WHEN WE HAVE THE SETTINGS PANEL.
        void SendSceneData(bool force = false)
        {
            if (ProbeReferenceVolume.instance.sceneData == null) return;
            if (!m_SentDataToSceneData || force)
            {
                ProbeReferenceVolume.instance.sceneData.SetBakeSettingsForScene(gameObject.scene, GetDilationSettings(), GetVirtualOffsetSettings());
                ProbeReferenceVolume.instance.sceneData.SetProfileForScene(gameObject.scene, m_Profile);
                m_SentDataToSceneData = true;
            }
        }

        internal void QueueAssetLoading()
        {
            LoadProfileInformation();
        }

        internal void QueueAssetRemoval()
        {
            if (volumeAsset == null)
                return;

#if UNITY_EDITOR
            m_PrevAsset = null;
#endif
        }

        void OnEnable()
        {
#if UNITY_EDITOR
            if (m_Profile == null)
                m_Profile = CreateReferenceVolumeProfile(gameObject.scene, gameObject.name);
#endif
            SendSceneData(force: true);
            QueueAssetLoading();
        }

#if UNITY_EDITOR

        void OnValidate()
        {
            if (!enabled || !gameObject.activeSelf)
                return;

            if (m_Profile != null)
            {
                m_PrevProfile = m_Profile;
            }

            if (volumeAsset != m_PrevAsset && m_PrevAsset != null)
            {
                ProbeReferenceVolume.instance.AddPendingAssetRemoval(m_PrevAsset);
            }

            if (volumeAsset != m_PrevAsset)
            {
                QueueAssetLoading();
            }

            m_PrevAsset = volumeAsset;
            SendSceneData(force: true);
        }

        void OnDisable()
        {
            QueueAssetRemoval();
        }

        void OnDestroy()
        {
            QueueAssetRemoval();
        }

        // IMPORTANT TODO: This is to be deleted when we have the proper setting panel.
        private void Update()
        {
            SendSceneData();
        }

        public ProbeDilationSettings GetDilationSettings()
        {
            ProbeDilationSettings settings;
            settings.dilationValidityThreshold =  m_DilationValidityThreshold;
            settings.dilationDistance = m_EnableDilation ? m_MaxDilationSampleDistance : 0.0f;
            settings.dilationIterations = m_DilationIterations;
            settings.squaredDistWeighting = m_DilationInvSquaredWeight;
            settings.brickSize = brickSize;

            return settings;
        }

        public VirtualOffsetSettings GetVirtualOffsetSettings()
        {
            VirtualOffsetSettings settings;
            settings.useVirtualOffset = m_EnableVirtualOffset;
            settings.searchMultiplier = m_VirtualOffsetGeometrySearchMultiplier;
            settings.outOfGeoOffset = m_VirtualOffsetBiasOutOfGeometry;

            return settings;
        }

#endif
    }
}
