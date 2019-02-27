#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    // This class keep track of every diffusion profile in the project so it can generate unique uint hashes
    // for every asset, which are used to differentiate diffusion profiles in the shader
    [InitializeOnLoad]
    public class DiffusionProfileHashTable
    {
        [System.NonSerialized]
        static Dictionary<int,  uint>           diffusionProfileHashes = new Dictionary<int, uint>();
        [System.NonSerialized]
        static Queue<DiffusionProfileSettings>  diffusionProfileToUpdate = new Queue<DiffusionProfileSettings>();

        // Called at each domain reload to build a list of all diffusion profile hashes so we can check
        // for collisions when we create the hash for a new asset
        static DiffusionProfileHashTable()
        {
            EditorApplication.update += UpdateDiffusionProfileHashes;
        }

        static uint GetDiffusionProfileHash(DiffusionProfileSettings asset)
        {
            uint hash32 = (uint)AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)).GetHashCode();
            uint mantissa = hash32 & 0x7FFFFF;
            uint exponent = 0b10000000; // 0 as exponent

            // only store the first 23 bits so when the hash is converted to float, it doesn't write into
            // the exponent part of the float (which avoids having NaNs, inf or precisions issues)
            return (exponent << 23) | mantissa;
        }

        static uint GenerateUniqueHash(DiffusionProfileSettings asset)
        {
            uint hash = GetDiffusionProfileHash(asset);
            
            while (diffusionProfileHashes.ContainsValue(hash) || hash == DiffusionProfileConstants.DIFFUSION_PROFILE_NEUTRAL_ID)
            {
                Debug.LogWarning("Collision found in asset: " + asset + ", generating a new hash, previous hash: " + hash);
                hash++;
            }

            return hash;
        }

        static void UpdateDiffusionProfileHashes()
        {
            while (diffusionProfileToUpdate.Count != 0)
            {
                var profile = diffusionProfileToUpdate.Dequeue();

                // if the profile to update is destroyed before the next editor frame, it will be null
                if (profile == null)
                    continue;

                // We upgrade from here to be able so the call to AssetDatabase.SaveAssets() does not stall
                // the editor (apparently in some configuration when loading the editor calling SaveAssets()
                // inside OnEnable() just break the editor)
                profile.TryToUpgrade();

                UpdateDiffusionProfileHashNow(profile);

                profile.profile.Validate();
                profile.UpdateCache();
            }
        }

        public static void UpdateDiffusionProfileHashNow(DiffusionProfileSettings profile)
        {
            uint hash = profile.profile.hash;

            // If the hash is 0, then we need to generate a new one (it means that the profile was just created)
            if (hash == 0)
            {
                profile.profile.hash = GenerateUniqueHash(profile);
                EditorUtility.SetDirty(profile);
            }
            // If the asset is not in the list, we regenerate it's hash using the GUID (which leads to the same result every time)
            else if (!diffusionProfileHashes.ContainsKey(profile.GetInstanceID()))
            {
                profile.profile.hash = GenerateUniqueHash(profile);
                EditorUtility.SetDirty(profile);
            }
            else // otherwise, no issue, we don't change the hash and we keep it to check for collisions
                diffusionProfileHashes.Add(profile.GetInstanceID(), profile.profile.hash);
        }

        public static void UpdateUniqueHash(DiffusionProfileSettings asset)
        {
            // Defere the generation of the hash because we can't call AssetDatabase functions outside of editor scope
            diffusionProfileToUpdate.Enqueue(asset);
        }
    }
}
#endif