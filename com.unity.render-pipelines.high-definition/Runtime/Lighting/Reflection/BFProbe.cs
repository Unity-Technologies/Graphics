using System;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL]
    public enum BFProbeConfig
    {
        StorageOctSize = 64,
        StorageWidthInProbes = 64,
        StorageHeightInProbes = 64,
        StorageMaxProbeCount = StorageWidthInProbes * StorageHeightInProbes,

        TempCubeSize = 32,
        TempMaxProbeCount = 128,

        CopyThreadGroupSize = 64,
    }

    [ExecuteAlways]
    public class BFProbe : MonoBehaviour
    {

    }
}
