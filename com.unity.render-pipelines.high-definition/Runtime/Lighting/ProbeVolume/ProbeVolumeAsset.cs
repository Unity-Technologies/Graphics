using System;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

namespace UnityEngine.Rendering.HighDefinition
{
    public class ProbeVolumeAsset : ScriptableObject
    {
        public enum AssetVersion
        {
            First,
            // Add new version here and they will automatically be the Current one
            Max,
            Current = Max - 1
        }

        protected int m_Version = (int)AssetVersion.First;
        public int Version { get => m_Version; }

        public SphericalHarmonicsL1[] data = null;
        public float[] dataValidity = null;
        public float[] dataOctahedralDepth = null;

        public int resolutionX;
        public int resolutionY;
        public int resolutionZ;

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Assets/Create/ProbeVolume", false, 204)]
        protected static void CreateAssetFromMenu()
        {
            CreateAsset();
        }

        public static string GetFileName(int id = -1)
        {
            string assetName = "ProbeVolumeData";

            String assetFileName;
            String assetPath;

            if (id == -1)
            {
                assetPath = "Assets";
                assetFileName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(assetName + ".asset");
            }
            else
            {
                String scenePath = SceneManagement.SceneManager.GetActiveScene().path;
                assetPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(scenePath), System.IO.Path.GetFileNameWithoutExtension(scenePath));
                assetFileName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(assetName + id + ".asset");
            }

            assetFileName = System.IO.Path.Combine(assetPath, assetFileName);

            return assetFileName;
        }

        public static ProbeVolumeAsset CreateAsset(int id = -1)
        {
            ProbeVolumeAsset asset = ScriptableObject.CreateInstance<ProbeVolumeAsset>();
            string assetFileName = GetFileName(id);

            UnityEditor.AssetDatabase.CreateAsset(asset, assetFileName);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            return asset;
        }

        protected static Vector3Int[] s_Offsets = new Vector3Int[] {
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

        protected int IndexAt(Vector3Int pos)
        {
            return pos.x + pos.y * resolutionX + pos.z * resolutionX * resolutionY;
        }

        (SphericalHarmonicsL1, float) Sample(SphericalHarmonicsL1[] data, float[] dataValidity, Vector3Int pos)
        {
            if (pos.x < 0 || pos.y < 0 || pos.z < 0 || pos.x >= resolutionX || pos.y >= resolutionY || pos.z >= resolutionZ)
                return (new SphericalHarmonicsL1(), 1);

            int index = IndexAt(pos);

            SphericalHarmonicsL1 sh = data[index];
            float v = dataValidity[index];

            return (sh, v);
        }

        bool OverwriteInvalidProbe(SphericalHarmonicsL1[] dataSrc, SphericalHarmonicsL1[] dataDst, float[] dataValiditySrc, float[] dataValidityDst, Vector3Int pos, float backfaceTolerance)
        {
            (SphericalHarmonicsL1 center, float validityCenter) = Sample(dataSrc, dataValiditySrc, pos);

            int centerIndex = IndexAt(pos);

            dataDst[centerIndex] = center;
            dataValidityDst[centerIndex] = validityCenter;

            if (validityCenter <= backfaceTolerance)
                return true;

            int weights = 0;
            SphericalHarmonicsL1 result = new SphericalHarmonicsL1();
            float validity = 0;

            foreach (Vector3Int offset in s_Offsets)
            {
                Vector3Int samplePos = pos + offset;

                (SphericalHarmonicsL1 sample, float sampleValidity) = Sample(dataSrc, dataValiditySrc, samplePos);

                if (sampleValidity > backfaceTolerance)
                    // invalid sample, don't use
                    continue;

                result.shAr += sample.shAr;
                result.shAg += sample.shAg;
                result.shAb += sample.shAb;

                validity += sampleValidity;

                weights++;
            }

            if (weights > 0)
            {
                result.shAr /= weights;
                result.shAg /= weights;
                result.shAb /= weights;
                validity /= weights;

                dataDst[centerIndex] = result;
                dataValidityDst[centerIndex] = validity;

                return true;
            }

            // Haven't managed to overwrite an invalid probe
            return false;
        }

        void DilateIntoInvalidProbes(float backfaceTolerance, int dilateIterations)
        {
            if (dilateIterations == 0)
                return;

            SphericalHarmonicsL1[] dataBis = new SphericalHarmonicsL1[data.Length];
            float[] dataValidityBis = new float[data.Length];

            int i = 0;
            for (; i < dilateIterations; ++i)
            {
                bool invalidProbesRemaining = false;

                for (int z = 0; z < resolutionZ; ++z)
                    for (int y = 0; y < resolutionY; ++y)
                        for (int x = 0; x < resolutionX; ++x)
                            invalidProbesRemaining |= !OverwriteInvalidProbe(data, dataBis, dataValidity, dataValidityBis, new Vector3Int(x, y, z), backfaceTolerance);

                // Swap buffers
                (data, dataBis) = (dataBis, data);
                (dataValidity, dataValidityBis) = (dataValidityBis, dataValidity);

                if (!invalidProbesRemaining)
                    break;
            }
        }

        public void Dilate()
        {
            DilateIntoInvalidProbes(0.25f, 2);
        }
#endif
    }
}
