using System;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEditor.Experimental;
using Unity.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

[assembly: InternalsVisibleTo("Unity.Entities.Hybrid.HybridComponents")]
[assembly: InternalsVisibleTo("Unity.Rendering.Hybrid")]

namespace UnityEngine.Rendering.HighDefinition
{
    internal static class VolumeGlobalUniqueIDUtils
    {
        // Unity will only natively serialize ints, but we want to serialize ulong global unique ids.
        // Split up the bits across two ints for serialization.
        [System.Serializable]
        internal struct VolumeGlobalUniqueID : IEquatable<VolumeGlobalUniqueID>
        {
            [SerializeField] private int value_31_0;
            [SerializeField] private int value_63_32;
            [SerializeField] private int value_95_64;
            [SerializeField] private int value_127_96;
            [SerializeField] private int value_159_128;
            [SerializeField] private int value_191_160;
            [SerializeField] private int value_194_192;
            [SerializeField] private int value_226_195;
            [SerializeField] private int value_258_227;

            public VolumeGlobalUniqueID(int identifierType, ulong assetGUID_127_64, ulong assetGUID_63_0, ulong targetObjectId, ulong targetPrefabId)
            {
                Debug.Assert((identifierType >= 0) && (identifierType < ((1 << 2) - 1)));

                ulong maskLo = (1ul << 32) - 1ul;
                value_31_0 = unchecked((int)(maskLo & assetGUID_63_0));
                value_63_32 = unchecked((int)(assetGUID_63_0 >> 32));

                value_95_64 = unchecked((int)(maskLo & assetGUID_127_64));
                value_127_96 = unchecked((int)(assetGUID_127_64 >> 32));

                // To maintain support with a previous version of this ID format, we swizzle targetPrefabId and targetObjectId's location based on whether or not it is a prefab.
                // This swizzle is not necessary in the current format, since we always store both the targetPrefabId and the targetObjectId. It is only necessary so that
                // non-prefab assets saved in the old format still work.
                bool isPrefab = (targetPrefabId != 0);
                ulong targetId = isPrefab ? targetPrefabId : targetObjectId;
                value_159_128 = unchecked((int)(maskLo & targetId));
                value_191_160 = unchecked((int)(targetId >> 32));

                value_194_192 = isPrefab ? 1 : 0;
                value_194_192 |= unchecked((int)(((uint)identifierType) << 1));

                ulong targetOtherId = isPrefab ? targetObjectId : targetPrefabId;
                value_226_195 = unchecked((int)(maskLo & targetOtherId));
                value_258_227 = unchecked((int)(targetOtherId >> 32));
            }

            private void Decode(out int identifierType, out ulong assetGUID_127_64, out ulong assetGUID_63_0, out ulong targetObjectId, out ulong targetPrefabId)
            {
                assetGUID_63_0 = (((ulong)unchecked((uint)value_63_32)) << 32) | (ulong)unchecked((uint)value_31_0);
                assetGUID_127_64 = (((ulong)unchecked((uint)value_127_96)) << 32) | (ulong)unchecked((uint)value_95_64);

                identifierType = (int)(unchecked((uint)value_194_192) >> 1);

                bool isPrefab = (unchecked((uint)value_194_192) & 1u) == 1u;

                ulong targetId = (((ulong)unchecked((uint)value_191_160)) << 32) | (ulong)unchecked((uint)value_159_128);
                ulong targetOtherId = (((ulong)unchecked((uint)value_258_227)) << 32) | (ulong)unchecked((uint)value_226_195);

                targetObjectId = isPrefab ? targetOtherId : targetId;
                targetPrefabId = isPrefab ? targetId : targetOtherId;
            }

            public static readonly VolumeGlobalUniqueID zero = new VolumeGlobalUniqueID()
            {
                value_31_0 = 0,
                value_63_32 = 0,
                value_95_64 = 0,
                value_127_96 = 0,
                value_159_128 = 0,
                value_191_160 = 0,
                value_194_192 = 0,
                value_226_195 = 0,
                value_258_227 = 0
            };

            public bool Equals(VolumeGlobalUniqueID other)
            {
                return (this.value_31_0 == other.value_31_0)
                    && (this.value_63_32 == other.value_63_32)
                    && (this.value_95_64 == other.value_95_64)
                    && (this.value_127_96 == other.value_127_96)
                    && (this.value_159_128 == other.value_159_128)
                    && (this.value_191_160 == other.value_191_160)
                    && (this.value_194_192 == other.value_194_192)
                    && (this.value_226_195 == other.value_226_195)
                    && (this.value_258_227 == other.value_258_227);
            }

            public override bool Equals(object other)
            {
                return other is VolumeGlobalUniqueID globalUniqueID && Equals(globalUniqueID);
            }

            public override int GetHashCode()
            {
                var hash = value_31_0.GetHashCode();
                hash = hash * 23 + value_63_32.GetHashCode();
                hash = hash * 23 + value_63_32.GetHashCode();
                hash = hash * 23 + value_95_64.GetHashCode();
                hash = hash * 23 + value_127_96.GetHashCode();
                hash = hash * 23 + value_191_160.GetHashCode();
                hash = hash * 23 + value_194_192.GetHashCode();
                hash = hash * 23 + value_226_195.GetHashCode();
                hash = hash * 23 + value_258_227.GetHashCode();

                return hash;
            }

            public override string ToString()
            {
                Decode(out int identifierType, out ulong assetGUID_127_64, out ulong assetGUID_63_0, out ulong targetObjectId, out ulong targetPrefabId);

                return string.Format("V1-{0}-{1:x16}{2:x16}-{3}-{4}", identifierType, assetGUID_127_64, assetGUID_63_0, targetObjectId, targetPrefabId);
            }

            public static bool operator ==(VolumeGlobalUniqueID lhs, VolumeGlobalUniqueID rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator !=(VolumeGlobalUniqueID lhs, VolumeGlobalUniqueID rhs)
            {
                return !lhs.Equals(rhs);
            }
        }

#if UNITY_EDITOR
        internal interface IVolumeGlobalUniqueIDOwnerEditorOnly
        {
            void InitializeDuplicate();
            VolumeGlobalUniqueID GetGlobalUniqueID();
            void SetGlobalUniqueID(VolumeGlobalUniqueID id);
        }

        internal static void InitializeGlobalUniqueIDEditorOnly<T>(T owner) where T : UnityEngine.Object, IVolumeGlobalUniqueIDOwnerEditorOnly
        {
            if (UnityEditor.EditorApplication.isPlaying) { return; }

            UnityEditor.GlobalObjectId globalObjectId = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(owner);
            VolumeGlobalUniqueID globalUniqueIDGoal = ParseVolumeGlobalUniqueIDFromGlobalObjectId(globalObjectId);

            if (owner.GetGlobalUniqueID() == VolumeGlobalUniqueID.zero)
            {
                owner.SetGlobalUniqueID(globalUniqueIDGoal);
            }
            else if (owner.GetGlobalUniqueID() != globalUniqueIDGoal)
            {
                // Encountered a Probe Volume who's serialized globalUniqueId does not match it's actual globalUniqueId.
                // This occurs if a Probe Volume is duplicated.
                owner.SetGlobalUniqueID(globalUniqueIDGoal);
                owner.InitializeDuplicate();
            }
        }

        private static readonly string GLOBAL_OBJECT_ID_ASSET_GUID_PREFIX = "GlobalObjectId_V1-0-";

        internal static VolumeGlobalUniqueID ParseVolumeGlobalUniqueIDFromGlobalObjectId(UnityEditor.GlobalObjectId globalObjectId)
        {
            // https://docs.unity3d.com/ScriptReference/GlobalObjectId.html
            // GlobalObjectId_V1-0-00000000000000000000000000000000-0-0
            string globalObjectIdString = globalObjectId.ToString();

            int assetGUIDStringStart = GLOBAL_OBJECT_ID_ASSET_GUID_PREFIX.Length;

            if (!TryParseUlongFromHexSubstringNoGCAlloc(out ulong assetGUID_127_64, globalObjectIdString, assetGUIDStringStart, assetGUIDStringStart + 15))
            {
                Debug.AssertFormat(false, "Failed to parse hi digits of GLobalObjectId string {0}", globalObjectIdString);
            }

            if (!TryParseUlongFromHexSubstringNoGCAlloc(out ulong assetGUID_63_0, globalObjectIdString, assetGUIDStringStart + 16, assetGUIDStringStart + 31))
            {
                Debug.AssertFormat(false, "Failed to parse lo digits of GLobalObjectId string {0}", globalObjectIdString);
            }

            return new VolumeGlobalUniqueID(globalObjectId.identifierType, assetGUID_127_64, assetGUID_63_0, globalObjectId.targetObjectId, globalObjectId.targetPrefabId);
        }

        private static bool TryParseUlongFromHexSubstringNoGCAlloc(out ulong res, string s, int start, int end)
        {
            Debug.Assert(start >= 0 && start < s.Length);
            Debug.Assert(end >= start && end < s.Length);

            // ulong can only store up to 16 hex digits.
            Debug.Assert((end - start) < 16);

            res = 0;
            for (int i = start; i <= end; ++i)
            {
                ulong digit = 0;
                switch (s[i])
                {
                    case '0': digit = 0; break;
                    case '1': digit = 1; break;
                    case '2': digit = 2; break;
                    case '3': digit = 3; break;
                    case '4': digit = 4; break;
                    case '5': digit = 5; break;
                    case '6': digit = 6; break;
                    case '7': digit = 7; break;
                    case '8': digit = 8; break;
                    case '9': digit = 9; break;
                    case 'a': digit = 10; break;
                    case 'b': digit = 11; break;
                    case 'c': digit = 12; break;
                    case 'd': digit = 13; break;
                    case 'e': digit = 14; break;
                    case 'f': digit = 15; break;
                    case 'A': digit = 10; break;
                    case 'B': digit = 11; break;
                    case 'C': digit = 12; break;
                    case 'D': digit = 13; break;
                    case 'E': digit = 14; break;
                    case 'F': digit = 15; break;
                    default: res = 0; return false;
                }

                res = res * 16ul + digit;
            }

            return true;
        }
#endif
    }
}