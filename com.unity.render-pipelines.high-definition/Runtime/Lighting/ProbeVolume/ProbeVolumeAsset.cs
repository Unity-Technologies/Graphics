using System;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.HighDefinition.VolumeGlobalUniqueIDUtils;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    [PreferBinarySerialization]
    internal class ProbeVolumeAsset : ScriptableObject
    {
        [Serializable]
        internal enum AssetVersion
        {
            First,
            AddProbeVolumesAtlasEncodingModes,
            AddOctahedralDepthVarianceFromLightmapper,
            AddRotation,
            RearrangeNeighborData,
            // Add new version here and they will automatically be the Current one
            Max,
            Current = Max - 1
        }

        [SerializeField] protected internal int m_Version = (int)AssetVersion.Current;
        [SerializeField] internal int Version { get => m_Version; }

        // dataSH, dataValidity, and dataOctahedralDepth is from AssetVersion.First. In versions AddProbeVolumesAtlasEncodingModes or greater, this should be null.
        [SerializeField] internal SphericalHarmonicsL1[] dataSH = null;
        [SerializeField] internal float[] dataValidity = null;
        [SerializeField] internal float[] dataOctahedralDepth = null;

        [SerializeField] internal ProbeVolumePayload payload = ProbeVolumePayload.zero;

        [SerializeField] internal int resolutionX;
        [SerializeField] internal int resolutionY;
        [SerializeField] internal int resolutionZ;

        [SerializeField] internal float backfaceTolerance;
        [SerializeField] internal int dilationIterations;

        [SerializeField] internal Quaternion rotation;

        [SerializeField] internal VolumeGlobalUniqueID globalUniqueID;

        internal bool IsDataAssigned()
        {
            return payload.dataSHL01 != null;
        }

        internal VolumeGlobalUniqueID GetID()
        {
            return (globalUniqueID == VolumeGlobalUniqueID.zero) ? new VolumeGlobalUniqueID(0, 0, 0, (ulong)unchecked((uint)GetInstanceID()), 0) : globalUniqueID;
        }

#if UNITY_EDITOR
        // Debug only: Uncomment out if you want to manually create a probe volume asset and type in data into the inspector.
        // This is not a user facing workflow we are supporting.
        // [UnityEditor.MenuItem("Assets/Create/Experimental/Probe Volume", false, 204)]
        // protected static void CreateAssetFromMenu()
        // {
        //     CreateAsset();
        // }

        private static string GetFileName(VolumeGlobalUniqueID globalUniqueID)
        {
            string assetName = "ProbeVolumeData";

            String assetFileName;
            String assetPath;


            String scenePath = SceneManagement.SceneManager.GetActiveScene().path;
            String sceneDir = System.IO.Path.GetDirectoryName(scenePath);
            String sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            assetPath = System.IO.Path.Combine(sceneDir, sceneName);

            if (!UnityEditor.AssetDatabase.IsValidFolder(assetPath))
                UnityEditor.AssetDatabase.CreateFolder(sceneDir, sceneName);

            assetFileName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(string.Format("{0}-{1}{2}", assetName, globalUniqueID.ToString(), ".asset"));

            assetFileName = System.IO.Path.Combine(assetPath, assetFileName);

            return assetFileName;
        }

        internal static ProbeVolumeAsset CreateAsset(VolumeGlobalUniqueID globalUniqueID)
        {
            ProbeVolumeAsset asset = ScriptableObject.CreateInstance<ProbeVolumeAsset>();
            asset.globalUniqueID = globalUniqueID;
            EditorUtility.SetDirty(asset);

            string assetFileName = GetFileName(globalUniqueID);

            UnityEditor.AssetDatabase.CreateAsset(asset, assetFileName);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            return asset;
        }

        protected internal static Vector3Int[] s_Offsets = new Vector3Int[] {
            // middle slice
            new Vector3Int( 1,  0,  0),
            new Vector3Int( 1,  0,  1),
            new Vector3Int( 1,  0, -1),

            new Vector3Int(-1,  0,  0),
            new Vector3Int(-1,  0,  1),
            new Vector3Int(-1,  0, -1),

            new Vector3Int( 0,  0,  1),
            new Vector3Int( 0,  0, -1),


            // upper slice
            new Vector3Int( 0,  1,  0),
            new Vector3Int( 1,  1,  0),
            new Vector3Int( 1,  1,  1),
            new Vector3Int( 1,  1, -1),
            new Vector3Int(-1,  1,  0),
            new Vector3Int(-1,  1,  1),
            new Vector3Int(-1,  1, -1),
            new Vector3Int( 0,  1,  1),
            new Vector3Int( 0,  1, -1),

            // lower slice
            new Vector3Int( 0, -1,  0),
            new Vector3Int( 1, -1,  0),
            new Vector3Int( 1, -1,  1),
            new Vector3Int( 1, -1, -1),
            new Vector3Int(-1, -1,  0),
            new Vector3Int(-1, -1,  1),
            new Vector3Int(-1, -1, -1),
            new Vector3Int( 0, -1,  1),
            new Vector3Int( 0, -1, -1),
        };

        protected internal int ComputeIndex1DFrom3D(Vector3Int pos)
        {
            return pos.x + pos.y * resolutionX + pos.z * resolutionX * resolutionY;
        }

        bool OverwriteInvalidProbe(ref ProbeVolumePayload payloadSrc, ref ProbeVolumePayload payloadDst, Vector3Int index3D, float backfaceTolerance)
        {
            int strideSHL01 = ProbeVolumePayload.GetDataSHL01Stride();
            int strideSHL2 = ProbeVolumePayload.GetDataSHL2Stride();
            int centerIndex = ComputeIndex1DFrom3D(index3D);

            // Account for center sample accumulation weight, already assigned.
            float weights = 1.0f - payloadDst.dataValidity[centerIndex];

            foreach (Vector3Int offset in s_Offsets)
            {
                Vector3Int sampleIndex3D = index3D + offset;

                if (sampleIndex3D.x < 0 || sampleIndex3D.y < 0 || sampleIndex3D.z < 0
                    || sampleIndex3D.x >= resolutionX || sampleIndex3D.y >= resolutionY || sampleIndex3D.z >= resolutionZ)
                {
                    continue;
                }

                int sampleIndex1D = ComputeIndex1DFrom3D(sampleIndex3D);

                float sampleValidity = payloadSrc.dataValidity[sampleIndex1D];
                if (sampleValidity > 0.999f)
                {
                    // Sample will have effectively zero contribution. Early out.
                    continue;
                }

                float sampleWeight = 1.0f - sampleValidity;
                weights += sampleWeight;

                for (int c = 0; c < strideSHL01; ++c)
                {
                    payloadDst.dataSHL01[centerIndex * strideSHL01 + c] += payloadSrc.dataSHL01[sampleIndex1D * strideSHL01 + c] * sampleWeight;
                }
                for (int c = 0; c < strideSHL2; ++c)
                {
                    payloadDst.dataSHL2[centerIndex * strideSHL2 + c] += payloadSrc.dataSHL2[sampleIndex1D * strideSHL2 + c] * sampleWeight;
                }

                payloadDst.dataValidity[centerIndex] += sampleValidity * sampleWeight;
            }

            if (weights > 0.0f)
            {
                float weightsNormalization = 1.0f / weights;
                for (int c = 0; c < strideSHL01; ++c)
                {
                    payloadDst.dataSHL01[centerIndex * strideSHL01 + c] *= weightsNormalization;
                }
                for (int c = 0; c < strideSHL2; ++c)
                {
                    payloadDst.dataSHL2[centerIndex * strideSHL2 + c] *= weightsNormalization;
                }

                payloadDst.dataValidity[centerIndex] *= weightsNormalization;

                return true;
            }
            else
            {
                // Haven't managed to overwrite an invalid probe
                return false;
            }
        }

        void DilateIntoInvalidProbes(float backfaceTolerance, int dilateIterations)
        {
            if (dilateIterations == 0)
                return;

            ProbeVolumePayload payloadBackbuffer = ProbeVolumePayload.zero;
            ProbeVolumePayload.Allocate(ref payloadBackbuffer, ProbeVolumePayload.GetLength(ref payload));

            int i = 0;
            for (; i < dilateIterations; ++i)
            {
                bool invalidProbesRemaining = false;

                // First, copy data from source to destination to seed our center sample.
                ProbeVolumePayload.Copy(ref payload, ref payloadBackbuffer);

                // Foreach probe, gather neighboring probe data, weighted by validity.
                // TODO: "validity" is actually stored as how occluded the surface is, so it is really inverse validity.
                // We should probably rename this to avoid confusion.
                for (int z = 0; z < resolutionZ; ++z)
                {
                    for (int y = 0; y < resolutionY; ++y)
                    {
                        for (int x = 0; x < resolutionX; ++x)
                        {
                            Vector3Int index3D = new Vector3Int(x, y, z);
                            int index1D = ComputeIndex1DFrom3D(index3D);
                            float validity = payloadBackbuffer.dataValidity[index1D];
                            if (validity <= backfaceTolerance)
                            {
                                // "validity" aka occlusion is low enough for our theshold.
                                // No need to gather + filter neighbors.
                                continue;
                            }

                            invalidProbesRemaining |= !OverwriteInvalidProbe(ref payload, ref payloadBackbuffer, index3D, backfaceTolerance);
                        }
                    }
                }

                // Swap buffers
                (payload, payloadBackbuffer) = (payloadBackbuffer, payload);

                if (!invalidProbesRemaining)
                    break;
            }

            ProbeVolumePayload.Dispose(ref payloadBackbuffer);
        }

        internal void Dilate(float backfaceTolerance, int dilationIterations)
        {
            if (backfaceTolerance == this.backfaceTolerance && dilationIterations == this.dilationIterations)
                return;

            // Validity data will be overwritten during dilation as a per-probe quality heuristic.
            // We want to retain original validity data for use bilateral filter on the GPU at runtime.
            float[] validityBackup = new float[payload.dataValidity.Length];
            Array.Copy(payload.dataValidity, validityBackup, payload.dataValidity.Length);

            DilateIntoInvalidProbes(backfaceTolerance, dilationIterations);

            Array.Copy(validityBackup, payload.dataValidity, validityBackup.Length);

            this.backfaceTolerance = backfaceTolerance;
            this.dilationIterations = dilationIterations;
        }

        [ContextMenu("Reserialize")]
        void Reserialize()
        {
            Migrate(null);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
        
        [ContextMenu("Reserialize All")]
        void ReserializeAll()
        {
            const string k_ProgressBarTitle = "Reserializing all Probe Volume assets";
            EditorUtility.DisplayProgressBar(k_ProgressBarTitle, "Searching assets", 1f / 6f);
            var assetGuids = AssetDatabase.FindAssets("t:" + nameof(ProbeVolumeAsset));
            EditorUtility.DisplayProgressBar(k_ProgressBarTitle, "Loading assets", 3f / 6f);
            for (int i = 0; i < assetGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<ProbeVolumeAsset>(path);
                asset.Migrate(null);
                EditorUtility.SetDirty(asset);
            }
            EditorUtility.DisplayProgressBar(k_ProgressBarTitle, "Saving assets", 5f / 6f);
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
        }
#endif

        internal void Migrate(ProbeVolume volume)
        {
#if UNITY_EDITOR
            var isDirty = false;
#endif
            while (Version < (int)AssetVersion.Current)
            {
                var nextVersion = Version + 1;
                bool migrationFailed = false;
                switch ((AssetVersion)nextVersion)
                {
                    case AssetVersion.AddProbeVolumesAtlasEncodingModes:
                        ApplyMigrationAddProbeVolumesAtlasEncodingModes();
                        break;
                    case AssetVersion.AddOctahedralDepthVarianceFromLightmapper:
                        ApplyMigrationAddOctahedralDepthVarianceFromLightmapper();
                        break;
                    case AssetVersion.AddRotation:
                        migrationFailed = !TryApplyMigrationAddRotation(volume);
                        break;
                    case AssetVersion.RearrangeNeighborData:
                        ApplyMigrationRearrangeNeighborData();
                        break;
                }

                if (migrationFailed)
                    break;

                m_Version = nextVersion;
#if UNITY_EDITOR
                isDirty = true;
#endif
            }
            
#if UNITY_EDITOR
            if (isDirty)
                EditorUtility.SetDirty(this);
#endif
        }

        void ApplyMigrationAddProbeVolumesAtlasEncodingModes()
        {
            int probeLength = dataSH.Length;

            ProbeVolumePayload.Allocate(ref payload, probeLength);

            for (int i = 0; i < probeLength; ++i)
            {
                ProbeVolumePayload.SetSphericalHarmonicsL1FromIndex(ref payload, dataSH[i], i);
            }

            dataSH = null;
            dataValidity = null;
            dataOctahedralDepth = null;
        }

        void ApplyMigrationAddOctahedralDepthVarianceFromLightmapper()
        {
            if (payload.dataOctahedralDepth == null)
                return;

            int probeLength = ProbeVolumePayload.GetLength(ref payload);
            var dataOctahedralDepthMigrated = new float[probeLength * ProbeVolumePayload.GetDataOctahedralDepthStride()];

            // Previously, the lightmapper only returned scalar mean depth values for octahedralDepth.
            // Now it returns float2(depthMean, depthMean^2) which can be used to reconstruct a variance estimate.
            int dataOctahedralDepthLengthPrevious = payload.dataOctahedralDepth.Length;
            for (int i = 0; i < dataOctahedralDepthLengthPrevious; ++i)
            {
                float depthMean = payload.dataOctahedralDepth[i];

                // For our migration, simply initialize our depthMeanSquared slots with depthMean * depthMean, which will reconstruct a zero variance estimate.
                // Really, the user will want to rebake to get a real variance estimate. This migration just ensures we do not error out.
                float depthMeanSquared = depthMean * depthMean;
                dataOctahedralDepthMigrated[i * 2 + 0] = depthMean;
                dataOctahedralDepthMigrated[i * 2 + 1] = depthMeanSquared;
            }
            payload.dataOctahedralDepth = dataOctahedralDepthMigrated;
        }

        bool TryApplyMigrationAddRotation(ProbeVolume volume)
        {
            if (volume == null)
            {
                Debug.LogWarning($"Data version of Probe Volume Asset \"{name}\" can't be updated on it's own, rotation is required. Open a scene with an enabled Probe Volume that uses this asset to update it.", this);
                return false;
            }

            rotation = volume.transform.rotation;
            return true;
        }

        void ApplyMigrationRearrangeNeighborData()
        {
            var neighborAxis = payload.neighborAxis;
            if (neighborAxis == null)
                return;

            // Neighbor data was grouped by probe: Probe0_Dir0, .., Probe0_DirN, Probe1_Dir0 .., Probe1_DirN, etc.
            // Rearrange so they are grouped by direction: Dir0_Probe0, .., Dir0_ProbeN, etc.

            var axisCount = neighborAxis.Length;
            var directionCount = ProbeVolumeDynamicGI.s_NeighborAxis.Length;

            var neighborAxisMigrated = new NeighborAxis[axisCount];
            var writeAxis = 0;
            for (int direction = 0; direction < directionCount; direction++)
            {
                // Iterate over probes reading the same direction.
                for (int readAxis = direction; readAxis < axisCount; readAxis += directionCount)
                {
                    neighborAxisMigrated[writeAxis] = neighborAxis[readAxis];
                    writeAxis++;
                }
            }
            payload.neighborAxis = neighborAxisMigrated;
        }
    }
}
