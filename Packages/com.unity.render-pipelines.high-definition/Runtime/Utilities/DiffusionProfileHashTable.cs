#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Collections.Generic;
using UnityEditor;
using System;

namespace UnityEditor.Rendering.HighDefinition
{
    // This class keep track of every diffusion profile in the project so it can generate unique uint hashes
    // for every asset, which are used to differentiate diffusion profiles in the shader
    [InitializeOnLoad]
    class DiffusionProfileHashTable
    {
        // In rare cases, the final profile hash can differ from its GUID hash, because collisions are only checked against loaded profiles.
        // 1. Load order. Hash collision resolution depends on load order. The later (colliding) profile is bumped to the next free hash and a warning is logged.
        // 2. Collision cascade. A bumped hash can collide with an existing one that is still unloaded, which is then bumped later on load.
        // 3. Domain reload. The table is cleared and profiles are checked again in arbitrary order, which can trigger a cascade (2.)

        [System.NonSerialized]
        static Dictionary<EntityId,  uint>           diffusionProfileHashes = new Dictionary<EntityId, uint>();
        [System.NonSerialized]
        static Dictionary<uint, EntityId>            diffusionProfileOwners = new Dictionary<uint, EntityId>();

        // Max offset from the GUID hash for a stored hash to be kept.
        // A profile moved further is regenerated on every load.
        internal const uint k_MaxHashOffset = 16;

        // Stable hash to avoid having different result after upgrading mono
        // Source: https://github.com/Unity-Technologies/mono/blob/unity-2021.2-mbe-pre-upgrade/mcs/class/referencesource/mscorlib/system/string.cs#L824
        unsafe static int MonoStringHash(string guid)
        {
            fixed(char *src = guid)
            {
                int hash1 = 5381;
                int hash2 = hash1;

                int     c;
                char *s = src;
                while ((c = s[0]) != 0)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ c;
                    c = s[1];
                    if (c == 0)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ c;
                    s += 2;
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        internal static uint GetDiffusionProfileHash(DiffusionProfileSettings asset)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);

            // In case the diffusion profile is not yet saved on the disk, we don't generate the hash
            if (String.IsNullOrEmpty(assetPath))
                return 0;

            uint hash32 = (uint)MonoStringHash(AssetDatabase.AssetPathToGUID(assetPath));
            uint mantissa = hash32 & 0x7FFFFF;
            uint exponent = 0b10000000; // 0 as exponent

            // only store the first 23 bits so when the hash is converted to float, it doesn't write into
            // the exponent part of the float (which avoids having NaNs, inf or precisions issues)
            return (exponent << 23) | mantissa;
        }

        public static void UpdateDiffusionProfileHashNow(DiffusionProfileSettings profile)
        {
            uint hash = profile.profile.hash;
            uint guidHash = GetDiffusionProfileHash(profile);
            EntityId entityId = profile.GetEntityId();

            // Hash 0 means the profile was just created.
            // Otherwise, the hash is regenerated from the GUID, for example when the asset was duplicated.
            bool isWithinMaxOffset = IsWithinMaxHashOffset(hash, guidHash);
            if (!isWithinMaxOffset || IsHashTaken(hash, entityId))
            {
                // Within the max offset, the stored hash is only regenerated when another profile took it
                bool storedHashTaken = isWithinMaxOffset;
                uint newHash = GenerateUniqueHash(profile, guidHash, entityId, storedHashTaken);
                if (newHash != hash)
                {
                    profile.profile.hash = newHash;
                    EditorUtility.SetDirty(profile);

                    // SetDirty is discarded when called from OnEnable during asset load.
                    // Only a hash changed by a collision needs to be saved, the GUID hash is regenerated on every load.
                    if (newHash != guidHash || storedHashTaken)
                    {
                        EditorApplication.delayCall += () =>
                        {
                            if (profile != null)
                                EditorUtility.SetDirty(profile);
                        };
                    }
                }
            }

            Register(profile, entityId);
        }

        static bool IsWithinMaxHashOffset(uint storedHash, uint guidHash)
        {
            // Unsigned subtraction wraps a stored hash below guidHash, including 0, to a large value
            return storedHash - guidHash < k_MaxHashOffset;
        }

        static bool IsHashTaken(uint hash, EntityId entityId)
        {
            if (!diffusionProfileOwners.TryGetValue(hash, out EntityId owner) || owner == entityId)
                return false;

            // The owner was deleted
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(owner)))
            {
                diffusionProfileOwners.Remove(hash);
                diffusionProfileHashes.Remove(owner);
                return false;
            }

            return true;
        }

        static uint GenerateUniqueHash(DiffusionProfileSettings asset, uint guidHash, EntityId entityId, bool storedHashTaken)
        {
            uint hash = guidHash;
            while (IsHashTaken(hash, entityId))
            {
                hash++;
            }

            if (hash != guidHash || storedHashTaken)
            {
                uint storedHash = asset.profile.hash;
                uint takenHash = storedHashTaken ? storedHash : guidHash;
                string assetPath = AssetDatabase.GetAssetPath(asset);
                string ownerPath = AssetDatabase.GetAssetPath(diffusionProfileOwners[takenHash]);
                string message = $"Collision found in asset: {assetPath}, hash {takenHash} is already used by {ownerPath}, using hash {hash} instead.";
                if (!IsWithinMaxHashOffset(hash, guidHash))
                    message += $" {k_MaxHashOffset} or more diffusion profiles have similar hashes, so the hash of {assetPath} is regenerated on every load and can change. " +
                        $"Duplicate {assetPath} and assign the copy to the materials that use it.";
                else if (storedHash != 0 && storedHash != hash)
                    message += $" Select {assetPath} again on the materials that use it.";
                Debug.LogWarning(message, asset);
            }

            return hash;
        }

        static void Register(DiffusionProfileSettings profile, EntityId entityId)
        {
            uint hash = profile.profile.hash;
            if (hash == 0)
                return;

            if (diffusionProfileHashes.TryGetValue(entityId, out uint previousHash) && previousHash != hash && !IsHashTaken(previousHash, entityId))
                diffusionProfileOwners.Remove(previousHash);

            diffusionProfileHashes[entityId] = hash;
            diffusionProfileOwners[hash] = entityId;
        }

        internal static void Unregister(DiffusionProfileSettings profile)
        {
            EntityId entityId = profile.GetEntityId();
            if (diffusionProfileHashes.Remove(entityId, out uint hash) && !IsHashTaken(hash, entityId))
                diffusionProfileOwners.Remove(hash);
        }
    }
}
#endif
