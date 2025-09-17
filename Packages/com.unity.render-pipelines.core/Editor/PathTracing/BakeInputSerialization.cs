using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

// The types defined in this file should match the types defined in BakeInput.h.
namespace UnityEditor.PathTracing.LightBakerBridge
{
    internal interface IBakeInputVisitable
    {
        void Transfer(IBakeInputVisitor visitor);
    }

    internal delegate void TransferFunction<T>(IBakeInputVisitor visitor, ref T result);

    internal interface IBakeInputVisitor
    {
        // Booleans and strings need special handling as they are not blittable types.
        public void TransferBoolean(ref bool result);

        public void TransferString(ref string result);

        public void TransferArray<T>(ref T[] array, TransferFunction<T> transfer);

        public void TransferBlittable<T>(ref T result)
            where T : unmanaged;

        public void TransferBlittableArray<T>(ref T[] array)
            where T : unmanaged;

        public void TransferDictionary<TKey, TValue>(ref Dictionary<TKey, TValue> dict, TransferFunction<TValue> valueTransfer)
            where TKey : unmanaged;
    }

    internal static class BakeInputVisitorExtensions
    {
        public static void Transfer<T>(this IBakeInputVisitor self, ref T result)
            where T : IBakeInputVisitable => result.Transfer(self);

        public static void TransferArray<T>(this IBakeInputVisitor self, ref T[] array) where T : IBakeInputVisitable
            => self.TransferArray(ref array, (IBakeInputVisitor visitor, ref T result) => visitor.Transfer(ref result));

        public static void TransferBlittableDictionary<TKey, TValue>(this IBakeInputVisitor self, ref Dictionary<TKey, TValue> dict)
            where TKey : unmanaged
            where TValue : unmanaged
        {
            self.TransferDictionary(ref dict, (IBakeInputVisitor visitor, ref TValue result) => visitor.TransferBlittable(ref result));
        }
    }

    internal class BakeInputReader : IBakeInputVisitor
    {
        private int _position;
        private byte[] _bytes;

        public BakeInputReader(byte[] bytes)
        {
            _position = 0;
            _bytes = bytes;
        }

        public void TransferBoolean(ref bool result)
        {
            result = _bytes[_position] != 0;
            _position += sizeof(byte);
        }

        public void TransferString(ref string result)
        {
            byte[] raw = null;
            this.TransferBlittableArray(ref raw);
            result = System.Text.Encoding.ASCII.GetString(raw);
        }

        public void TransferArray<T>(ref T[] array, TransferFunction<T> transfer)
        {
            UInt64 length = 0;
            TransferBlittable(ref length);
            array = new T[length];
            for (int i = 0; i < (int)length; i++)
            {
                transfer(this, ref array[i]);
            }
        }

        public unsafe void TransferBlittable<T>(ref T result)
            where T : unmanaged
        {
            var size = sizeof(T);
            fixed (byte* ptr = &_bytes[_position])
            {
                UnsafeUtility.CopyPtrToStructure(ptr, out result);
                _position += size;
            }
        }

        public unsafe void TransferBlittableArray<T>(ref T[] array)
            where T : unmanaged
        {
            UInt64 length = 0;
            TransferBlittable(ref length);

            array = new T[length];

            if (0 == length) // This avoids going out-of-bounds below when we are at the end of data.
                return;

            // Pin the managed arrays while we copy data over
            int byteLength = (int)length * sizeof(T);
            fixed (byte* ptr = &_bytes[_position])
            {
                fixed (T* arrayPtr = array)
                {
                    UnsafeUtility.MemCpy(arrayPtr, ptr, byteLength);
                    _position += byteLength;
                }
            }
        }

        public void TransferDictionary<TKey, TValue>(ref Dictionary<TKey, TValue> dict, TransferFunction<TValue> valueTransfer)
            where TKey : unmanaged
        {
            UInt64 length = 0;
            TransferBlittable(ref length);
            dict = new Dictionary<TKey, TValue>((int)length);
            for (int i = 0; i < (int)length; i++)
            {
                TKey key = default;
                TransferBlittable(ref key);
                TValue value = default;
                valueTransfer(this, ref value);
                dict.Add(key, value);
            }
        }
    }

    internal class BakeInputWriter : IBakeInputVisitor
    {
        private List<byte> _outBytes;

        public BakeInputWriter(List<byte> outBytes)
        {
            _outBytes = outBytes;
        }

        public void TransferBoolean(ref bool result) => _outBytes.Add(result ? (byte)1 : (byte)0);

        public void TransferString(ref string result)
        {
            byte[] raw = System.Text.Encoding.ASCII.GetBytes(result);
            this.TransferBlittableArray(ref raw);
        }

        public void TransferArray<T>(ref T[] array, TransferFunction<T> transfer)
        {
            UInt64 length = (UInt64)array.Length;
            TransferBlittable(ref length);
            for (int i = 0; i < (int)length; i++)
            {
                transfer(this, ref array[i]);
            }
        }

        public unsafe void TransferBlittable<T>(ref T result)
            where T : unmanaged
        {
            var size = sizeof(T);
            byte[] bytes = new byte[size];
            fixed (byte* ptr = &bytes[0])
            {
                UnsafeUtility.CopyStructureToPtr(ref result, ptr);
            }
            _outBytes.AddRange(bytes);
        }

        public void TransferBlittableArray<T>(ref T[] array)
            where T : unmanaged => TransferArray(ref array, (IBakeInputVisitor visitor, ref T result) => visitor.TransferBlittable(ref result));

        public void TransferDictionary<TKey, TValue>(ref Dictionary<TKey, TValue> dict, TransferFunction<TValue> valueTransfer)
            where TKey : unmanaged
        {
            UInt64 length = (UInt64)dict.Count;
            TransferBlittable(ref length);

            foreach (var (key, value) in dict)
            {
                TKey keyCopy = key;
                TransferBlittable(ref keyCopy);
                TValue valueCopy = value;
                valueTransfer(this, ref valueCopy);
            }
        }
    }

    internal enum Backend
    {
        CPU = 0,
        GPU,
        UnityComputeGPU,
    }

    internal enum TransmissionType
    {
        Opacity = 0,
        Transparency,
        None
    }

    internal enum TransmissionChannels
    {
        Red = 0,
        Alpha,
        AlphaCutout,
        RGB,
        None
    }

    internal enum LightmapBakeMode
    {
        NonDirectional = 0,
        CombinedDirectional
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SampleCount : IBakeInputVisitable
    {
        public UInt32 directSampleCount;
        public UInt32 indirectSampleCount;
        public UInt32 environmentSampleCount;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.TransferBlittable(ref directSampleCount);
            visitor.TransferBlittable(ref indirectSampleCount);
            visitor.TransferBlittable(ref environmentSampleCount);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LightingSettings : IBakeInputVisitable
    {
        public SampleCount lightmapSampleCounts;
        public SampleCount probeSampleCounts;
        public UInt32 minBounces;
        public UInt32 maxBounces;
        public LightmapBakeMode lightmapBakeMode;
        public MixedLightingMode mixedLightingMode;
        public bool aoEnabled;
        public float aoDistance;
        public bool useHardwareRayTracing;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.Transfer(ref lightmapSampleCounts);
            visitor.Transfer(ref probeSampleCounts);
            visitor.TransferBlittable(ref minBounces);
            visitor.TransferBlittable(ref maxBounces);
            visitor.TransferBlittable(ref lightmapBakeMode);
            visitor.TransferBlittable(ref mixedLightingMode);
            visitor.TransferBoolean(ref aoEnabled);
            visitor.TransferBlittable(ref aoDistance);
            visitor.TransferBlittable(ref useHardwareRayTracing);
        }
    }

    internal enum MeshShaderChannel
    {
        None = -1,
        Vertex = 0,
        Normal = 1,
        TexCoord0 = 2,
        TexCoord1 = 3,
        Count = 4
    }

    [Flags]
    internal enum MeshShaderChannelMask
    {
        Invalid = -1,
        Empty = 0,
        Vertex = 1 << MeshShaderChannel.Vertex,
        Normal = 1 << MeshShaderChannel.Normal,
        TexCoord0 = 1 << MeshShaderChannel.TexCoord0,
        TexCoord1 = 1 << MeshShaderChannel.TexCoord1,
        MaskAll = (1 << MeshShaderChannel.Count) - 1
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VertexData : IBakeInputVisitable
    {
        public UInt32 vertexCount;
        public MeshShaderChannelMask meshShaderChannelMask;
        public UInt32[] dimensions; // number of float comprising the channel item
        public UInt32[] offsets; // offset to channel item in bytes
        public UInt32[] stride; // stride between channel items in bytes
        public byte[] data;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.TransferBlittable(ref vertexCount);
            visitor.TransferBlittable(ref meshShaderChannelMask);
            visitor.TransferBlittableArray(ref dimensions);
            visitor.TransferBlittableArray(ref offsets);
            visitor.TransferBlittableArray(ref stride);
            visitor.TransferBlittableArray(ref data);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MeshData : IBakeInputVisitable
    {
        public VertexData vertexData;
        public UInt32[] indexBuffer;
        public UInt32[] subMeshIndexOffset;
        public UInt32[] subMeshIndexCount;
        public Bounds[] subMeshAABB;
        public float4 uvScaleOffset;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.Transfer(ref vertexData);
            visitor.TransferBlittableArray(ref indexBuffer);
            visitor.TransferBlittableArray(ref subMeshIndexOffset);
            visitor.TransferBlittableArray(ref subMeshIndexCount);
            visitor.TransferBlittableArray(ref subMeshAABB);
            visitor.TransferBlittable(ref uvScaleOffset);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MaterialData : IBakeInputVisitable
    {
        public bool doubleSidedGI;
        public TransmissionType transmissionType;
        public TransmissionChannels transmissionChannels;
        public float alphaCutoff;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.TransferBoolean(ref doubleSidedGI);
            visitor.TransferBlittable(ref transmissionType);
            visitor.TransferBlittable(ref transmissionChannels);
            visitor.TransferBlittable(ref alphaCutoff);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HeightmapData : IBakeInputVisitable
    {
        public Int16[] data;
        public UInt16 resolution;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.TransferBlittableArray(ref data);
            visitor.TransferBlittable(ref resolution);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TerrainHoleData : IBakeInputVisitable
    {
        public byte[] data;
        public UInt16 resolution;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.TransferBlittableArray(ref data);
            visitor.TransferBlittable(ref resolution);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TerrainData : IBakeInputVisitable
    {
        public UInt32 heightMapIndex; // index into BakeInput::m_HeightmapData
        public Int32 terrainHoleIndex; // index into BakeInput::m_TerrainHoleData -1 means no hole data
        public float outputResolution;
        public float3 heightmapScale;
        public float4 uvBounds;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.TransferBlittable(ref heightMapIndex);
            visitor.TransferBlittable(ref terrainHoleIndex);
            visitor.TransferBlittable(ref outputResolution);
            visitor.TransferBlittable(ref heightmapScale);
            visitor.TransferBlittable(ref uvBounds);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct InstanceData : IBakeInputVisitable
    {
        public Int32 meshIndex; // index into BakeInput::m_MeshData, -1 for Terrain
        public Int32 terrainIndex; // index into BakeInput::m_TerrainData, -1 for MeshRenderer
        public float4x4 transform;
        public bool oddNegativeScale;
        public bool castShadows;
        public bool receiveShadows;
        public Int32 lodGroup;
        public byte lodMask;
        public Int32 contributingLodLevel;
        public Int32[] subMeshMaterialIndices; // -1 is no material for a given subMesh entry

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.TransferBlittable(ref meshIndex);
            visitor.TransferBlittable(ref terrainIndex);
            visitor.TransferBlittable(ref transform);
            visitor.TransferBoolean(ref oddNegativeScale);
            visitor.TransferBoolean(ref castShadows);
            visitor.TransferBoolean(ref receiveShadows);
            visitor.TransferBlittable(ref lodGroup);
            visitor.TransferBlittable(ref lodMask);
            visitor.TransferBlittable(ref contributingLodLevel);
            visitor.TransferBlittableArray(ref subMeshMaterialIndices);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TextureData : IBakeInputVisitable
    {
        public UInt32 width;
        public UInt32 height;
        public float4[] data;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.TransferBlittable(ref width);
            visitor.TransferBlittable(ref height);
            visitor.TransferBlittableArray(ref data);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TextureTransformData : IBakeInputVisitable
    {
        public float2 scale;
        public float2 offset;

        public void Transfer(IBakeInputVisitor visitor)
        {
            float4 data = default;
            visitor.TransferBlittable(ref data);
            scale = data.xy;
            offset = data.zw;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TextureProperties : IBakeInputVisitable
    {
        public TextureWrapMode wrapModeU;
        public TextureWrapMode wrapModeV;
        public FilterMode filterMode;
        public TextureTransformData transmissionTextureST;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.TransferBlittable(ref wrapModeU);
            visitor.TransferBlittable(ref wrapModeV);
            visitor.TransferBlittable(ref filterMode);
            visitor.Transfer(ref transmissionTextureST);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EnvironmentData : IBakeInputVisitable
    {
        public UInt32 cubeResolution;
        public float4[] cubeData;
        public UInt32 importanceSampleCount;
        public float importanceIntegratedMetric;
        public float4[] importanceDirections;
        public float4[] importanceWeightedIntensities;
        public float4[] importanceIntensities;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.TransferBlittable(ref cubeResolution);
            visitor.TransferBlittableArray(ref cubeData);
            visitor.TransferBlittable(ref importanceSampleCount);
            visitor.TransferBlittable(ref importanceIntegratedMetric);
            visitor.TransferBlittableArray(ref importanceDirections);
            visitor.TransferBlittableArray(ref importanceWeightedIntensities);
            visitor.TransferBlittableArray(ref importanceIntensities);
        }
    }

    internal enum LightType : byte
    {
        Directional = 0,
        Point,
        Spot,
        Rectangle,
        Disc,
        SpotPyramidShape,
        SpotBoxShape
    }

    internal enum FalloffType : byte
    {
        InverseSquared = 0,
        InverseSquaredNoRangeAttenuation,
        Linear,
        Legacy,
        None
    }

    internal enum AngularFalloffType : byte
    {
        LUT = 0,
        AnalyticAndInnerAngle
    }

    internal enum LightMode : byte
    {
        Realtime = 0,
        Mixed,
        Baked
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LightData : IBakeInputVisitable
    {
        // shared
        public float3 color;
        public float3 indirectColor;
        public Quaternion orientation;
        public float3 position;
        public float range;

        // cookie
        public UInt32 cookieTextureIndex;
        public float cookieScale;

        // spot light only or cookieSize for directional lights
        public float coneAngle;
        public float innerConeAngle;

        // area light parameters (interpretation depends on the type)
        public float shape0;
        public float shape1;

        public LightType type;
        public LightMode mode;
        public FalloffType falloff;
        public AngularFalloffType angularFalloff;
        public bool castsShadows;
        public UInt32 shadowMaskChannel;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.TransferBlittable(ref color);
            visitor.TransferBlittable(ref indirectColor);
            visitor.TransferBlittable(ref orientation);
            visitor.TransferBlittable(ref position);
            visitor.TransferBlittable(ref range);
            visitor.TransferBlittable(ref cookieTextureIndex);
            visitor.TransferBlittable(ref cookieScale);
            visitor.TransferBlittable(ref coneAngle);
            visitor.TransferBlittable(ref innerConeAngle);
            visitor.TransferBlittable(ref shape0);
            visitor.TransferBlittable(ref shape1);
            visitor.TransferBlittable(ref type);
            visitor.TransferBlittable(ref mode);
            visitor.TransferBlittable(ref falloff);
            visitor.TransferBlittable(ref angularFalloff);
            visitor.TransferBoolean(ref castsShadows);
            visitor.TransferBlittable(ref shadowMaskChannel);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CookieData : IBakeInputVisitable
    {
        public UInt32 width;
        public UInt32 height;
        public UInt32 pixelStride;
        public UInt32 slices; // 1 for single, 6 for cubes
        public bool repeat; // texture addressing mode
        public byte[] textureData;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.TransferBlittable(ref width);
            visitor.TransferBlittable(ref height);
            visitor.TransferBlittable(ref pixelStride);
            visitor.TransferBlittable(ref slices);
            visitor.TransferBoolean(ref repeat);
            visitor.TransferBlittableArray(ref textureData);
        }
    }

    [Flags]
    internal enum ProbeRequestOutputType : uint
    {
        RadianceDirect = 1 << 0,
        RadianceIndirect = 1 << 1,
        Validity = 1 << 2,
        MixedLightOcclusion = 1 << 3,
        LightProbeOcclusion = 1 << 4,
        EnvironmentOcclusion = 1 << 5,
        Depth = 1 << 6,
        All = 0xFFFFFFFF
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ProbeRequest : IBakeInputVisitable
    {
        public ProbeRequestOutputType outputTypeMask;
        public UInt64 positionOffset;
        public UInt64 count;
        public float pushoff;
        public string outputFolderPath;

        public UInt64 integrationRadiusOffset;
        public UInt32 environmentOcclusionSampleCount;
        public bool ignoreDirectEnvironment;
        public bool ignoreIndirectEnvironment;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.TransferBlittable(ref outputTypeMask);
            visitor.TransferBlittable(ref positionOffset);
            visitor.TransferBlittable(ref count);
            visitor.TransferBlittable(ref pushoff);
            visitor.TransferString(ref outputFolderPath);
            visitor.TransferBlittable(ref integrationRadiusOffset);
            visitor.TransferBlittable(ref environmentOcclusionSampleCount);
            visitor.TransferBoolean(ref ignoreDirectEnvironment);
            visitor.TransferBoolean(ref ignoreIndirectEnvironment);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ProbeRequestData : IBakeInputVisitable
    {
        public float3[] positions;
        public int[] occlusionLightIndices; // 4 entries per probe, index into BakeInput.lightData
        public float[] integrationRadii;
        public ProbeRequest[] requests;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.TransferBlittableArray(ref positions);
            visitor.TransferBlittableArray(ref occlusionLightIndices);
            visitor.TransferBlittableArray(ref integrationRadii);
            visitor.TransferArray(ref requests);
        }
    }

    [Flags]
    internal enum LightmapRequestOutputType : uint
    {
        IrradianceIndirect = 1 << 0,
        IrradianceDirect = 1 << 1,
        IrradianceEnvironment = 1 << 2,
        Occupancy = 1 << 3,
        Validity = 1 << 4,
        DirectionalityIndirect = 1 << 5,
        DirectionalityDirect = 1 << 6,
        AmbientOcclusion = 1 << 7,
        Shadowmask = 1 << 8,
        Normal = 1 << 9,
        ChartIndex = 1 << 10,
        OverlapPixelIndex = 1 << 11,
        All = 0xFFFFFFFF
    }

    internal enum TilingMode : byte
    {   // Assuming a 4k lightmap (16M texels), the tiling will yield the following chunk sizes:
        None = 0,                 // 4k * 4k =    16M texels
        Quarter = 1,              // 2k * 2k =     4M texels
        Sixteenth = 2,            // 1k * 1k =     1M texels
        Sixtyfourth = 3,          // 512 * 512 = 262k texels
        TwoHundredFiftySixth = 4, // 256 * 256 =  65k texels
        Max = TwoHundredFiftySixth,
        Error = 5                 // Error. We don't want to go lower (GPU occupancy will start to be a problem for smaller atlas sizes).
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LightmapRequest : IBakeInputVisitable
    {
        public LightmapRequestOutputType outputTypeMask;
        public UInt32 lightmapOffset;
        public UInt32 lightmapCount;
        public TilingMode tilingMode;
        public string outputFolderPath;
        public float pushoff;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.TransferBlittable(ref outputTypeMask);
            visitor.TransferBlittable(ref lightmapOffset);
            visitor.TransferBlittable(ref lightmapCount);
            visitor.TransferBlittable(ref tilingMode);
            visitor.TransferString(ref outputFolderPath);
            visitor.TransferBlittable(ref pushoff);
        }

        public static UInt64 TilingModeToLightmapExpandedBufferSize(TilingMode tilingMode)
        {
            UInt64 kMinBufferSize = 64;
            UInt64 bufferSize = 0;
            // TODO: We need to change the naming of the entries in the enum see: https://jira.unity3d.com/browse/GFXFEAT-728
            switch (tilingMode)
            {
                case TilingMode.None: bufferSize = 1048576; break;                     // UI: Highest Performance
                case TilingMode.Quarter: bufferSize = 524288; break;                   // UI: High Performance
                case TilingMode.Sixteenth: bufferSize = 262144; break;                 // UI: Automatic     (but it is not automatic)
                case TilingMode.Sixtyfourth: bufferSize = 131072; break;               // UI: Low Memory Usage
                case TilingMode.TwoHundredFiftySixth: bufferSize = 65536; break;       // UI: Lowest Memory Usage
                default: Debug.Assert(false, "Unknown tiling mode."); break;
            }
            return math.max(bufferSize, kMinBufferSize);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ProgressiveBakeParametersStruct
    {
        public enum SupersamplingMultiplier
        {
            kSupersamplingDisabled = 1,
            kSupersamplingx2 = 2,
            kSupersamplingx4 = 4
        }

        public float backfaceTolerance;
        public SupersamplingMultiplier supersamplingMultiplier;
        public float pushOff;
        public int bakedLightmapTag;
        public int maxLightmapCount;
    }

    // This struct has the same binary layout as Hash128, but represents how we use 'fake' hashes
    // to store indices in LightBaker.
    [StructLayout(LayoutKind.Sequential)]
    internal struct IndexHash128 : IEquatable<IndexHash128>
    {
        internal ulong _u64First;
        internal ulong _u64Second;

        public ulong Index => _u64First;

        public override int GetHashCode() => HashCode.Combine(_u64First, _u64Second);
        public bool Equals(IndexHash128 other) => _u64First == other._u64First && _u64Second == other._u64Second;
        public override bool Equals(object obj) => obj is IndexHash128 other && Equals(other);
        public static bool operator ==(IndexHash128 a, IndexHash128 b) => a.Equals(b);
        public static bool operator !=(IndexHash128 a, IndexHash128 b) => !a.Equals(b);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PVRAtlasData : IBakeInputVisitable
    {
        public IndexHash128 m_AtlasHash;      // The hash of this atlas.
        public IndexHash128 m_SceneGUID;      // The scene identifier, used for multi-scene bakes.
        public Int32 m_AtlasId;          // The atlasId.
        public ProgressiveBakeParametersStruct m_BakeParameters;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.TransferBlittable(ref m_AtlasHash);
            visitor.TransferBlittable(ref m_SceneGUID);
            visitor.TransferBlittable(ref m_AtlasId);
            visitor.TransferBlittable(ref m_BakeParameters);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GBufferInstanceData : IBakeInputVisitable
    {
        public IndexHash128 objectIDHash;
        public IndexHash128 geometryHashPVR;
        public float4 st;
        public int transformIndex;
        public Rect atlasViewport;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.TransferBlittable(ref objectIDHash);
            visitor.TransferBlittable(ref geometryHashPVR);
            visitor.TransferBlittable(ref st);
            visitor.TransferBlittable(ref transformIndex);
            visitor.TransferBlittable(ref atlasViewport);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GBufferInstances : IBakeInputVisitable
    {
        public GBufferInstanceData[] gbufferInstanceDataArray;
        public int atlasId;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.TransferArray(ref gbufferInstanceDataArray);
            visitor.TransferBlittable(ref atlasId);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AtlassedInstanceData : IBakeInputVisitable
    {
        public int m_AtlasId;          // The 0-based atlas index of the atlas (used by the renderers to get the lightmap).
        public int m_InstanceIndex;    // Instance index in atlas.
        public float4 m_LightmapST;   // The atlas UV scale and translate.
        public Rect m_Viewport;
        public float m_Width;
        public float m_Height;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.TransferBlittable(ref m_AtlasId);
            visitor.TransferBlittable(ref m_InstanceIndex);
            visitor.TransferBlittable(ref m_LightmapST);
            visitor.TransferBlittable(ref m_Viewport);
            visitor.TransferBlittable(ref m_Width);
            visitor.TransferBlittable(ref m_Height);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PVRAtlassingData : IBakeInputVisitable
    {
        public PVRAtlasData[] m_AtlasIdToAtlasHash;
        public PVRAtlasData[] m_AtlasIdToAtlasHashLightmapped;
        public (int width, int height)[] m_AtlasSizes;
        public Dictionary<IndexHash128, int> m_AtlasHashToAtlasId;
        public Dictionary<IndexHash128, IndexHash128> m_AtlasHashToGBufferHash;
        public IndexHash128[] m_GBufferHashes;
        public Dictionary<IndexHash128, IndexHash128[]> m_AtlasHashToObjectIDHashes;
        public Dictionary<IndexHash128, float> m_AtlasHashToAtlasWeight;
        public Dictionary<IndexHash128, GBufferInstances> m_GBufferHashToGBufferInstances;
        public Dictionary<IndexHash128, AtlassedInstanceData> m_InstanceAtlassingData;
        public int[] m_AtlasOffsets;
        public double m_EstimatedTexelCount;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.TransferArray(ref m_AtlasIdToAtlasHash);
            visitor.TransferArray(ref m_AtlasIdToAtlasHashLightmapped);
            visitor.TransferBlittableArray(ref m_AtlasSizes);
            visitor.TransferBlittableDictionary(ref m_AtlasHashToAtlasId);
            visitor.TransferBlittableDictionary(ref m_AtlasHashToGBufferHash);
            visitor.TransferBlittableArray(ref m_GBufferHashes);
            visitor.TransferDictionary(ref m_AtlasHashToObjectIDHashes, (IBakeInputVisitor dictionaryVisitor, ref IndexHash128[] result) => dictionaryVisitor.TransferBlittableArray(ref result));
            visitor.TransferBlittableDictionary(ref m_AtlasHashToAtlasWeight);
            visitor.TransferDictionary(ref m_GBufferHashToGBufferInstances, (IBakeInputVisitor dictionaryVisitor, ref GBufferInstances result) => dictionaryVisitor.Transfer(ref result));
            visitor.TransferBlittableDictionary(ref m_InstanceAtlassingData);
            visitor.TransferBlittableArray(ref m_AtlasOffsets);
            visitor.TransferBlittable(ref m_EstimatedTexelCount);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LightmapRequestData : IBakeInputVisitable
    {
        public PVRAtlassingData atlassing;
        public LightmapRequest[] requests;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.Transfer(ref atlassing);
            visitor.TransferArray(ref requests);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BakeInput : IBakeInputVisitable
    {
        // Global settings
        public LightingSettings lightingSettings;
        // Mesh data
        public MeshData[] meshData;
        public TerrainData[] terrainData;
        public TerrainHoleData[] terrainHoleData;
        public HeightmapData[] heightMapData;
        // Material data
        public MaterialData[] materialData;
        // Instance data
        public InstanceData[] instanceData;
        // Texture data
        public UInt32[] instanceToTextureDataIndex; // Index into albedoData and emissiveData for each instance
        public Int32[] materialToTransmissionDataIndex; // Index into transmissionData and transmissionDataProperties for each material
        public TextureData[] albedoData;
        public TextureData[] emissiveData;
        public TextureData[] transmissionData; // Same size as transmissionDataProperties
        public TextureProperties[] transmissionDataProperties; // Same size as transmissionData
        // Cookie data
        public CookieData[] cookieData;
        public LightData[] lightData;
        // Environment data
        public EnvironmentData environmentData;

        public void Transfer(IBakeInputVisitor visitor)
        {
            visitor.Transfer(ref lightingSettings);
            visitor.TransferArray(ref meshData);
            visitor.TransferArray(ref terrainData);
            visitor.TransferArray(ref terrainHoleData);
            visitor.TransferArray(ref heightMapData);
            visitor.TransferArray(ref materialData);
            visitor.TransferArray(ref instanceData);
            visitor.TransferBlittableArray(ref instanceToTextureDataIndex);
            visitor.TransferBlittableArray(ref materialToTransmissionDataIndex);
            visitor.TransferArray(ref albedoData);
            visitor.TransferArray(ref emissiveData);
            visitor.TransferArray(ref transmissionData);
            visitor.TransferArray(ref transmissionDataProperties);
            visitor.TransferArray(ref cookieData);
            visitor.TransferArray(ref lightData);
            visitor.Transfer(ref environmentData);
        }
    }

    internal static class BakeInputSerialization
    {
        // Should match BakeInputSerialization::kCurrentFileVersion in BakeInputSerialization.h.
        // If these are out of sync, the implementation in this file probably needs to be updated.
        const UInt64 CurrentFileVersion = 202509021;

        public static bool Deserialize(string path, out BakeInput bakeInput)
        {
            BakeInputReader reader = new(File.ReadAllBytes(path));
            return Deserialize(reader, out bakeInput);
        }

        public static bool Deserialize(string path, out LightmapRequestData lightmapRequestData)
        {
            BakeInputReader reader = new(File.ReadAllBytes(path));
            return Deserialize(reader, out lightmapRequestData);
        }

        public static bool Deserialize(string path, out ProbeRequestData probeRequestData)
        {
            BakeInputReader reader = new(File.ReadAllBytes(path));
            return Deserialize(reader, out probeRequestData);
        }

        public static bool Deserialize(byte[] memory, out BakeInput bakeInput)
        {
            BakeInputReader reader = new(memory);
            return Deserialize(reader, out bakeInput);
        }

        private static bool Deserialize(BakeInputReader visitor, out BakeInput bakeInput)
        {
            bakeInput = default;

            UInt64 fileVersion = 0;
            visitor.TransferBlittable(ref fileVersion);
            Debug.Assert(fileVersion == CurrentFileVersion, "Version number did not match the current implementation of BakeInput deserialization.");
            if (fileVersion != CurrentFileVersion)
                return false;

            visitor.Transfer(ref bakeInput);

            return true;
        }

        private static bool Deserialize(BakeInputReader visitor, out LightmapRequestData lightmapRequestData)
        {
            lightmapRequestData = default;

            UInt64 fileVersion = 0;
            visitor.TransferBlittable(ref fileVersion);
            Debug.Assert(fileVersion == CurrentFileVersion, "Version number did not match the current implementation of LightmapRequestData deserialization.");
            if (fileVersion != CurrentFileVersion)
                return false;

            visitor.Transfer(ref lightmapRequestData);

            return true;
        }

        private static bool Deserialize(BakeInputReader visitor, out ProbeRequestData lightProbeRequestData)
        {
            lightProbeRequestData = default;

            UInt64 fileVersion = 0;
            visitor.TransferBlittable(ref fileVersion);
            Debug.Assert(fileVersion == CurrentFileVersion, "Version number did not match the current implementation of LightProbeRequestData deserialization.");
            if (fileVersion != CurrentFileVersion)
                return false;

            visitor.Transfer(ref lightProbeRequestData);

            return true;
        }

        public static byte[] Serialize(ref BakeInput bakeInput)
        {
            var bytes = new List<byte>();
            BakeInputWriter writer = new(bytes);

            UInt64 fileVersion = CurrentFileVersion;
            writer.TransferBlittable(ref fileVersion);

            writer.Transfer(ref bakeInput);

            return bytes.ToArray();
        }

        public static byte[] Serialize(ref LightmapRequestData lightmapRequestData)
        {
            var bytes = new List<byte>();
            BakeInputWriter writer = new(bytes);

            UInt64 fileVersion = CurrentFileVersion;
            writer.TransferBlittable(ref fileVersion);

            writer.Transfer(ref lightmapRequestData);

            return bytes.ToArray();
        }

        public static byte[] Serialize(ref ProbeRequestData probeRequestData)
        {
            var bytes = new List<byte>();
            BakeInputWriter writer = new(bytes);

            UInt64 fileVersion = CurrentFileVersion;
            writer.TransferBlittable(ref fileVersion);

            writer.Transfer(ref probeRequestData);

            return bytes.ToArray();
        }

        public static void Serialize(string path, ref BakeInput bakeInput)
        {
            File.WriteAllBytes(path, Serialize(ref bakeInput));
        }

        public static void Serialize(string path, ref LightmapRequestData lightmapRequestData)
        {
            File.WriteAllBytes(path, Serialize(ref lightmapRequestData));
        }

        public static void Serialize(string path, ref ProbeRequestData probeRequestData)
        {
            File.WriteAllBytes(path, Serialize(ref probeRequestData));
        }
    }
}
