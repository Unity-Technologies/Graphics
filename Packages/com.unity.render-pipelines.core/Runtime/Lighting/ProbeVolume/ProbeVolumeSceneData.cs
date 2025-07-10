using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    // Add Profile and baking settings.
    /// <summary> A class containing info about the bounds defined by the probe volumes in various scenes. </summary>
    [System.Serializable]
    [Obsolete("This class is no longer necessary for APV implementation. #from(2023.3)")]
    public class ProbeVolumeSceneData
    {
        internal Object parentAsset = null;

        [SerializeField, FormerlySerializedAs("sceneBounds"), Obsolete("This data is now serialized directly in the baking set asset. #from(2023.3)")]
        internal SerializedDictionary<string, Bounds> obsoleteSceneBounds;
        [SerializeField, FormerlySerializedAs("hasProbeVolumes"), Obsolete("This data is now serialized directly in the baking set asset. #from(2023.3)")]
        internal SerializedDictionary<string, bool> obsoleteHasProbeVolumes;

        /// <summary>
        /// Constructor for ProbeVolumeSceneData.
        /// </summary>
        /// <param name="parentAsset">The asset holding this ProbeVolumeSceneData, it will be dirtied every time scene bounds or settings are changed.</param>
        public ProbeVolumeSceneData(Object parentAsset)
        {
            SetParentObject(parentAsset);
        }

        /// <summary>Set a reference to the object holding this ProbeVolumeSceneData.</summary>
        /// <param name="parent">The object holding this ProbeVolumeSceneData, it will be dirtied every time scene bounds or settings are changed. </param>
        [Obsolete("#from(2023.3)")]
        public void SetParentObject(Object parent)
        {
            parentAsset = parent;
        }
    }
}
