using System.Collections.Generic;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// An Asset which holds a set of settings to use with a <see cref="Probe Reference Volume"/>.
    /// </summary>
    public sealed class ProbeVolumeBakingSet : ScriptableObject
    {
        internal enum Version
        {
            Initial,
        }

        // Baking Set Data

        [SerializeField] internal bool singleSceneMode = true;
        [SerializeField] internal ProbeVolumeBakingProcessSettings settings;

        [SerializeField] private List<string> m_SceneGUIDs = new List<string>();
        [SerializeField] internal List<string> scenesToNotBake = new List<string>();
        [SerializeField] internal List<string> lightingScenarios = new List<string>();

        internal IReadOnlyList<string> sceneGUIDs => m_SceneGUIDs;

        // Baking Profile

        [SerializeField]
        Version version = CoreUtils.GetLastEnumValue<Version>();

        // TODO: This is here just to find a place where to serialize it. It might not be the best spot.
        [SerializeField]
        internal bool freezePlacement = false;

        /// <summary>
        /// How many levels contains the probes hierarchical structure.
        /// </summary>
        [Range(2, 5)]
        public int simplificationLevels = 3;

        /// <summary>
        /// The size of a Cell in number of bricks.
        /// </summary>
        public int cellSizeInBricks => (int)Mathf.Pow(3, simplificationLevels);

        /// <summary>
        /// The minimum distance between two probes in meters.
        /// </summary>
        [Min(0.1f)]
        public float minDistanceBetweenProbes = 1.0f;

        /// <summary>
        /// Maximum subdivision in the structure.
        /// </summary>
        public int maxSubdivision => simplificationLevels + 1; // we add one for the top subdiv level which is the same size as a cell

        /// <summary>
        /// Minimum size of a brick in meters.
        /// </summary>
        public float minBrickSize => Mathf.Max(0.01f, minDistanceBetweenProbes * 3.0f);

        /// <summary>
        /// Size of the cell in meters.
        /// </summary>
        public float cellSizeInMeters => (float)cellSizeInBricks * minBrickSize;

        /// <summary>
        /// Layer mask filter for all renderers.
        /// </summary>
        public LayerMask renderersLayerMask = -1;

        /// <summary>
        /// Specifies the minimum bounding box volume of renderers to consider placing probes around.
        /// </summary>
        [Min(0)]
        public float minRendererVolumeSize = 0.1f;

        private void OnValidate()
        {
            singleSceneMode &= m_SceneGUIDs.Count <= 1;

            ProbeReferenceVolume.instance.sceneData?.SyncBakingSets();
            ProbeReferenceVolume.instance.sceneData?.AddBakingSet(this);

            if (lightingScenarios.Count == 0)
                lightingScenarios = new List<string>() { ProbeReferenceVolume.defaultLightingScenario };

            if (version != CoreUtils.GetLastEnumValue<Version>())
            {
                // Migration code
            }

            settings.Upgrade();
        }

        internal void Migrate(ProbeVolumeSceneData.BakingSet set)
        {
            singleSceneMode = false;
            settings = set.settings;
            m_SceneGUIDs = set.sceneGUIDs;
            lightingScenarios = set.lightingScenarios;
        }

        /// <summary>
        /// Determines if the Probe Reference Volume Profile is equivalent to another one.
        /// </summary>
        /// <param name ="otherProfile">The profile to compare with.</param>
        /// <returns>Whether the Probe Reference Volume Profile is equivalent to another one.</returns>
        public bool IsEquivalent(ProbeVolumeBakingSet otherProfile)
        {
            return minDistanceBetweenProbes == otherProfile.minDistanceBetweenProbes &&
                cellSizeInMeters == otherProfile.cellSizeInMeters &&
                simplificationLevels == otherProfile.simplificationLevels &&
                renderersLayerMask == otherProfile.renderersLayerMask;
        }

        internal void RemoveScene(string guid)
        {
            m_SceneGUIDs.Remove(guid);
            scenesToNotBake.Remove(guid);
            ProbeReferenceVolume.instance.sceneData.sceneToBakingSet.Remove(guid);
        }

        internal void AddScene(string guid)
        {
            m_SceneGUIDs.Add(guid);
            ProbeReferenceVolume.instance.sceneData.sceneToBakingSet[guid] = this;
        }

        internal void SetScene(string guid, int index)
        {
            scenesToNotBake.Remove(m_SceneGUIDs[index]);
            ProbeReferenceVolume.instance.sceneData.sceneToBakingSet.Remove(m_SceneGUIDs[index]);
            m_SceneGUIDs[index] = guid;
            ProbeReferenceVolume.instance.sceneData.sceneToBakingSet[guid] = this;
        }

        internal bool HasAnySceneWithProbeVolume()
        {
            foreach (var guid in sceneGUIDs)
            {
                if (ProbeReferenceVolume.instance.sceneData.hasProbeVolumes[guid])
                    return true;
            }
            return false;
        }

        internal string CreateScenario(string name)
        {
            if (lightingScenarios.Contains(name))
            {
                string renamed;
                int index = 1;
                do
                    renamed = $"{name} ({index++})";
                while (lightingScenarios.Contains(renamed));
                name = renamed;
            }
            lightingScenarios.Add(name);
            return name;
        }

        internal bool RemoveScenario(string name)
        {
            return lightingScenarios.Remove(name);
        }

        internal ProbeVolumeBakingSet Clone()
        {
            var newSet = Instantiate(this);
            newSet.m_SceneGUIDs.Clear();
            newSet.scenesToNotBake.Clear();
            return newSet;
        }

        #if UNITY_EDITOR
        internal void SetDefaults()
        {
            settings.SetDefaults();
            lightingScenarios = new List<string> { ProbeReferenceVolume.defaultLightingScenario };
        }

        internal void SanitizeScenes()
        {
            // Remove entries in the list pointing to deleted scenes
            for (int i = m_SceneGUIDs.Count - 1; i >= 0; i--)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(m_SceneGUIDs[i]);
                if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(path) == null)
                {
                    ProbeReferenceVolume.instance.sceneData.OnSceneRemovedFromSet(m_SceneGUIDs[i]);
                    UnityEditor.EditorUtility.SetDirty(this);
                    m_SceneGUIDs.RemoveAt(i);
                }
            }
            for (int i = scenesToNotBake.Count - 1; i >= 0; i--)
            {
                if (ProbeReferenceVolume.instance.sceneData.GetBakingSetForScene(scenesToNotBake[i]) != this)
                {
                    UnityEditor.EditorUtility.SetDirty(this);
                    scenesToNotBake.RemoveAt(i);
                }
            }
        }
        #endif
    }
}
