#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Unity.Collections;
using UnityEditor;
using static UnityEngine.Rendering.HighDefinition.VolumeGlobalUniqueIDUtils;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A manager to enqueue extra probe rendering outside of probe volumes.
    /// </summary>
    public class AdditionalGIBakeRequestsManager
    {
        private static AdditionalGIBakeRequestsManager s_Instance = new AdditionalGIBakeRequestsManager();
        /// <summary>
        /// Get the manager that governs the additional light probe rendering requests.
        /// </summary>
        public static AdditionalGIBakeRequestsManager instance { get { return s_Instance; } }

        private AdditionalGIBakeRequestsManager()
        {
            if (!Application.isPlaying)
            {
                lightmapperBakeIDFromBakeID.Clear();
                lightmapperBakeIDNext = 0;
            }
        }

        ~AdditionalGIBakeRequestsManager()
        {
            if (!Application.isPlaying)
            {
                lightmapperBakeIDFromBakeID.Clear();
                lightmapperBakeIDNext = 0;
            }
        }

        // Lightmapper API uses ints as keys, but we want to use full, stable, GlobalObjectIds as keys.
        // Rather than hashing and hoping we don't collide, lets handle this robustly by
        // keeping a dictionary of VolumeGlobalUniqueID->int bit keys.
        private Dictionary<VolumeGlobalUniqueID, int> lightmapperBakeIDFromBakeID = new Dictionary<VolumeGlobalUniqueID, int>(32);
        private int lightmapperBakeIDNext = 0;

        internal void SetAdditionalBakedProbes(VolumeGlobalUniqueID bakeID, Vector3[] positions)
        {
            if (TryGetLightmapperBakeIDFromBakeID(bakeID, out int lightmapperBakeID))
            {
                UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(lightmapperBakeID, null);

                // When baking, the lightmapper hashes its state (i.e: the list of all AdditionalBakedProbes requests)
                // and only bakes data if this hash is not changed.
                // By storing a generation ID inside of our lightmapperBakeID, we ensure that Sets will always look like a completely new bake request to the lightmapper.
                // The lightmapper will always bake it.
                // Without storing this generation index, if we clear our bake request by setting positions to NULL, then set our bake request with valid data,
                // then bake, the lightmapper will treat the new bake request as an already completed old one, and skip doing any work.
                // In the future, after proving out this generation based approach, it would be a good idea to move this generation tracking code into the lightmapper,
                // so that users dont need to do this bookkeeping for the lightmapper - they can simply set and clear requests and always get the correct, fresh results.
                IncrementLightmapperBakeIDGeneration(bakeID, out lightmapperBakeID);

                if (positions != null)
                {
                    UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(lightmapperBakeID, positions);
                }                
            }
        }

        internal bool GetAdditionalBakedProbes(VolumeGlobalUniqueID bakeID, NativeArray<SphericalHarmonicsL2> outBakedProbeSH, NativeArray<float> outBakedProbeValidity, NativeArray<float> outBakedProbeOctahedralDepth)
        {
            bool success = false;
            if (TryGetLightmapperBakeIDFromBakeID(bakeID, out int lightmapperBakeID))
            {
                success = UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(lightmapperBakeID, outBakedProbeSH, outBakedProbeValidity, outBakedProbeOctahedralDepth);
            }
            return success;
        }

        private bool TryGetLightmapperBakeIDFromBakeID(VolumeGlobalUniqueID bakeID, out int lightmapperBakeID)
        {
            bool success = false;
            if (lightmapperBakeIDFromBakeID.TryGetValue(bakeID, out lightmapperBakeID))
            {
                success = true;
            }
            // Leave the whole top bit free. We shouldn't encounter it in practice, avoiding it allows us to not worry about handling the signed case.
            else if (lightmapperBakeIDNext == ((1 << 23) - 1))
            {
                success = false;
                lightmapperBakeID = -1;
                Debug.LogWarningFormat("Error: Used up all lightmapper bake IDs. This should never happen. Somehow all {0} ids have been used up. This must be the result of a bug. Unlikely that you created and baked {0} unique bake requests. Quit and reopen unity to flush all IDs.", (1 << 23) - 1);
            }
            else
            {
                success = true;
                lightmapperBakeID = lightmapperBakeIDNext << 8;
                ++lightmapperBakeIDNext;
                lightmapperBakeIDFromBakeID.Add(bakeID, lightmapperBakeID);
            }

            return success;
        }

        private void IncrementLightmapperBakeIDGeneration(VolumeGlobalUniqueID bakeID, out int lightmapperBakeID)
        {
            lightmapperBakeID = -1;
            if (lightmapperBakeIDFromBakeID.TryGetValue(bakeID, out lightmapperBakeID))
            {
                IncrementLightmapperBakeIDGeneration(ref lightmapperBakeID);
                lightmapperBakeIDFromBakeID[bakeID] = lightmapperBakeID;
            }
            else
            {
                Debug.Assert(false);
            }
        }

        private static void IncrementLightmapperBakeIDGeneration(ref int lightmapperBakeID)
        {
            const int MASK = 255;
            int generationIndex = lightmapperBakeID & MASK;
            generationIndex = (generationIndex == MASK) ? 0 : (generationIndex + 1);

            lightmapperBakeID &= ~MASK;
            lightmapperBakeID |= generationIndex;
        }
    }
}
#endif
