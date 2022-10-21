using System;
using System.Runtime.CompilerServices;
using static UnityEngine.Rendering.HighDefinition.VolumeGlobalUniqueIDUtils;

[assembly: InternalsVisibleTo("Unity.Entities.Hybrid.HybridComponents")]
[assembly: InternalsVisibleTo("Unity.Rendering.Hybrid")]

namespace UnityEngine.Rendering.HighDefinition
{
    internal enum MaskSpacingMode
    {
        Density = 0,
        Resolution
    };

    [Serializable]
    internal struct MaskVolumePayload
    {
        public byte[] dataSHL0;

        public static readonly MaskVolumePayload zero = new MaskVolumePayload
        {
            dataSHL0 = null
        };

        public static int GetDataSHL0Stride()
        {
            return 4;
        }

        public static bool IsEmpty(ref MaskVolumePayload payload)
        {
            return payload.dataSHL0 == null || payload.dataSHL0.Length == 0;
        }

        public static int GetLength(ref MaskVolumePayload payload)
        {
            // No need to explicitly store mask length - dataValidity is one value per mask, so we can just query the length here.
            return payload.dataSHL0.Length / GetDataSHL0Stride();
        }

        public static void Allocate(ref MaskVolumePayload payload, int length)
        {
            payload.dataSHL0 = new byte[length * GetDataSHL0Stride()];
        }

        public static void Ensure(ref MaskVolumePayload payload, int length)
        {
            if (payload.dataSHL0 == null
                || payload.dataSHL0.Length != (length * GetDataSHL0Stride()))
            {
                Dispose(ref payload);
                Allocate(ref payload, length);
            }
        }

        public static void Dispose(ref MaskVolumePayload payload)
        {
            payload.dataSHL0 = null;
        }

        public static void Copy(ref MaskVolumePayload payloadSrc, ref MaskVolumePayload payloadDst)
        {
            Debug.Assert(GetLength(ref payloadSrc) == GetLength(ref payloadDst));

            Copy(ref payloadSrc, ref payloadDst, GetLength(ref payloadSrc));
        }

        public static void Copy(ref MaskVolumePayload payloadSrc, ref MaskVolumePayload payloadDst, int length)
        {
            Array.Copy(payloadSrc.dataSHL0, payloadDst.dataSHL0, length * GetDataSHL0Stride());
        }
        
        public static float FromUNormByte(byte value) => value / 255f;
        public static byte ToUNormByte(float value) => (byte)(Mathf.Clamp01(value) * 255f);

        public static void Resample(ref MaskVolumePayload oldPayload,
            int index0, float weight0,
            int index1, float weight1,
            int index2, float weight2,
            int index3, float weight3,
            int index4, float weight4,
            int index5, float weight5,
            int index6, float weight6,
            int index7, float weight7,
            ref MaskVolumePayload newPayload,
            int targetIndex)
        {
            var shl0Stride = GetDataSHL0Stride();
            index0 *= shl0Stride;
            index1 *= shl0Stride;
            index2 *= shl0Stride;
            index3 *= shl0Stride;
            index4 *= shl0Stride;
            index5 *= shl0Stride;
            index6 *= shl0Stride;
            index7 *= shl0Stride;
            targetIndex *= shl0Stride;
            for (int i = 0; i < shl0Stride; i++)
            {
                var value = oldPayload.dataSHL0[index0 + i] * weight0;
                value += oldPayload.dataSHL0[index1 + i] * weight1;
                value += oldPayload.dataSHL0[index2 + i] * weight2;
                value += oldPayload.dataSHL0[index3 + i] * weight3;
                value += oldPayload.dataSHL0[index4 + i] * weight4;
                value += oldPayload.dataSHL0[index5 + i] * weight5;
                value += oldPayload.dataSHL0[index6 + i] * weight6;
                value += oldPayload.dataSHL0[index7 + i] * weight7;
                newPayload.dataSHL0[targetIndex + i] = (byte)value;
            }
        }
    }

#if UNITY_EDITOR
    // Rather than hashing all the inputs that define a Mask Volume's bake state into a 128-bit int (16-bytes),
    // we simply store the raw state values (56-bytes)
    // While this is 3.5x more memory, it's still fairly low, and avoids the runtime cost of string appending garbage creation.
    // It also means we can never ever have hash collision issues (due to precision loss in string construction, or from hashing),
    // which means we always detect changes correctly.
    [Serializable]
    internal struct MaskVolumeSettingsKey
    {
        public VolumeGlobalUniqueID id;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 size;
        public int resolutionX;
        public int resolutionY;
        public int resolutionZ;

        public static readonly MaskVolumeSettingsKey zero = new MaskVolumeSettingsKey()
        {
            id = VolumeGlobalUniqueID.zero,
            position = Vector3.zero,
            rotation = Quaternion.identity,
            size = Vector3.zero,
            resolutionX = 0,
            resolutionY = 0,
            resolutionZ = 0
        };
    }
#endif
    
    [Serializable]
    internal struct MaskVolumeArtistParameters
    {
        public bool drawGizmos;
        public float drawWeightThreshold;
        public Color debugColor;
        public Vector3 size;
        [SerializeField]
        private Vector3 m_PositiveFade;
        [SerializeField]
        private Vector3 m_NegativeFade;
        [SerializeField]
        private float m_UniformFade;
        public bool advancedFade;
        public float distanceFadeStart;
        public float distanceFadeEnd;

        public Vector3 scale;
        public Vector3 bias;

        public MaskSpacingMode maskSpacingMode;

        public float densityX;
        public float densityY;
        public float densityZ;

        public int resolutionX;
        public int resolutionY;
        public int resolutionZ;

        public float weight;
        public float normalBiasWS;

        public LightLayerEnum lightLayers;

        public Vector3 positiveFade
        {
            get
            {
                return advancedFade ? m_PositiveFade : m_UniformFade * Vector3.one;
            }
            set
            {
                if (advancedFade)
                {
                    m_PositiveFade = value;
                }
                else
                {
                    m_UniformFade = value.x;
                }
            }
        }

        public Vector3 negativeFade
        {
            get
            {
                return advancedFade ? m_NegativeFade : m_UniformFade * Vector3.one;
            }
            set
            {
                if (advancedFade)
                {
                    m_NegativeFade = value;
                }
                else
                {
                    m_UniformFade = value.x;
                }
            }
        }

        public MaskVolumeArtistParameters(Color debugColor)
        {
            this.debugColor = debugColor;
            this.drawGizmos = false;
            this.drawWeightThreshold = 0.0f;
            this.size = Vector3.one;
            this.m_PositiveFade = Vector3.zero;
            this.m_NegativeFade = Vector3.zero;
            this.m_UniformFade = 0;
            this.advancedFade = false;
            this.distanceFadeStart = 10000.0f;
            this.distanceFadeEnd = 10000.0f;
            this.scale = Vector3.zero;
            this.bias = Vector3.zero;
            this.maskSpacingMode = MaskSpacingMode.Density;
            this.resolutionX = 4;
            this.resolutionY = 4;
            this.resolutionZ = 4;
            this.densityX = (float)this.resolutionX / this.size.x;
            this.densityY = (float)this.resolutionY / this.size.y;
            this.densityZ = (float)this.resolutionZ / this.size.z;
            this.weight = 1;
            this.normalBiasWS = 0.0f;
            this.lightLayers = LightLayerEnum.LightLayerDefault;
        }

        internal void Constrain()
        {
            this.distanceFadeStart = Mathf.Max(0, this.distanceFadeStart);
            this.distanceFadeEnd = Mathf.Max(this.distanceFadeStart, this.distanceFadeEnd);

            switch (this.maskSpacingMode)
            {
                case MaskSpacingMode.Density:
                {
                    // Compute resolution from density and size.
                    this.densityX = Mathf.Max(1e-5f, this.densityX);
                    this.densityY = Mathf.Max(1e-5f, this.densityY);
                    this.densityZ = Mathf.Max(1e-5f, this.densityZ);

                    this.resolutionX = Mathf.Max(1, Mathf.RoundToInt(this.densityX * this.size.x));
                    this.resolutionY = Mathf.Max(1, Mathf.RoundToInt(this.densityY * this.size.y));
                    this.resolutionZ = Mathf.Max(1, Mathf.RoundToInt(this.densityZ * this.size.z));
                    break;
                }

                case MaskSpacingMode.Resolution:
                {
                    // Compute density from resolution and size.
                    this.resolutionX = Mathf.Max(1, this.resolutionX);
                    this.resolutionY = Mathf.Max(1, this.resolutionY);
                    this.resolutionZ = Mathf.Max(1, this.resolutionZ);

                    this.densityX = (float)this.resolutionX / Mathf.Max(1e-5f, this.size.x);
                    this.densityY = (float)this.resolutionY / Mathf.Max(1e-5f, this.size.y);
                    this.densityZ = (float)this.resolutionZ / Mathf.Max(1e-5f, this.size.z);
                    break;
                }

                default:
                {
                    Debug.Assert(false, "Error: MaskVolume: Encountered unsupported Mask Spacing Mode: " + this.maskSpacingMode);
                    break;
                }
            }
        }
    } // class MaskVolumeArtistParameters

    [ExecuteAlways]
    [AddComponentMenu("Rendering/Mask Volume")]
    internal class MaskVolume : MonoBehaviour
#if UNITY_EDITOR
        , IVolumeGlobalUniqueIDOwnerEditorOnly
#endif
    {
        internal struct MaskVolumeAtlasKey : IEquatable<MaskVolumeAtlasKey>
        {
            public VolumeGlobalUniqueID id;
            public int width;
            public int height;
            public int depth;

            public static readonly MaskVolumeAtlasKey zero = new MaskVolumeAtlasKey
            {
                id = VolumeGlobalUniqueID.zero,
                width = 0,
                height = 0,
                depth = 0
            };

            // Override Equals to manually control when atlas keys are considered equivalent.
            public bool Equals(MaskVolumeAtlasKey keyOther)
            {
                return (this.id == keyOther.id)
                    && (this.width == keyOther.width)
                    && (this.height == keyOther.height)
                    && (this.depth == keyOther.depth);
            }

            public override bool Equals(object other)
            {
                return other is MaskVolumeAtlasKey key && Equals(key);
            }

            public override int GetHashCode()
            {
                var hash = id.GetHashCode();
                hash = hash * 23 + width.GetHashCode();
                hash = hash * 23 + height.GetHashCode();
                hash = hash * 23 + depth.GetHashCode();

                return hash;
            }
        }

        internal bool dataUpdated = false;
        [SerializeField] private VolumeGlobalUniqueID globalUniqueID = VolumeGlobalUniqueID.zero;
        [SerializeField] internal MaskVolumeAsset maskVolumeAsset = null;
        [SerializeField] internal MaskVolumeArtistParameters parameters = new MaskVolumeArtistParameters(Color.white);

        VolumeGlobalUniqueID GetID()
        {
            // Handle case where a globalUniqueId has not been assigned yet.
            // This occurs due to legacy data - probe volumes that were serialized before we introduced globalUniqueIds.
            // The IDs can only be generated in the editor, so no way to perform runtime migrations (i.e: during streaming).
            return (globalUniqueID == VolumeGlobalUniqueID.zero) ? new VolumeGlobalUniqueID(0, 0, 0, (ulong)unchecked((uint)GetInstanceID()), 0) : globalUniqueID;
        }
        internal MaskVolumeAtlasKey ComputeMaskVolumeAtlasKey()
        {
            if (maskVolumeAsset == null)
                return MaskVolumeAtlasKey.zero;

            // Use the payloadID, rather than the probe volume ID to uniquely identify data in the atlas.
            // This ensures that if 2 mask volumes exist that point to the same data, that data will only be uploaded once.
            return ComputeMaskVolumeAtlasKey(GetPayloadID(), maskVolumeAsset.resolutionX, maskVolumeAsset.resolutionY, maskVolumeAsset.resolutionZ);
        }

        internal static MaskVolumeAtlasKey ComputeMaskVolumeAtlasKey(VolumeGlobalUniqueID id, int width, int height, int depth)
        {
            return new MaskVolumeAtlasKey
            {
                id = id,
                width = width,
                height = height,
                depth = depth
            };
        }

        private VolumeGlobalUniqueID GetPayloadID()
        {
            return (maskVolumeAsset == null) ? VolumeGlobalUniqueID.zero : maskVolumeAsset.GetID();
        }

        internal Vector3Int GetResolution()
        {
            return new Vector3Int(maskVolumeAsset.resolutionX, maskVolumeAsset.resolutionY, maskVolumeAsset.resolutionZ);
        }
        
        internal MaskVolumePayload GetPayload()
        {
            dataUpdated = false;

            if (!maskVolumeAsset) { return MaskVolumePayload.zero; }

            return maskVolumeAsset.payload;
        }

        bool CheckMigrationRequirement()
        {
            return maskVolumeAsset != null && maskVolumeAsset.Version != (int)MaskVolumeAsset.AssetVersion.Current;
        }

        void Migrate()
        {
            // Must not be called at deserialization time if require other component
            while (CheckMigrationRequirement())
            {
                ApplyMigration();
            }
        }

        void ApplyMigration()
        {
            switch ((MaskVolumeAsset.AssetVersion)maskVolumeAsset.Version)
            {
                case MaskVolumeAsset.AssetVersion.First:
                    // No migration required.
                    break;
            }
        }

        protected void OnEnable()
        {
            Migrate();

#if UNITY_EDITOR
            InitializeGlobalUniqueIDEditorOnly(this);
#endif
            MaskVolumeManager.manager.RegisterVolume(this);

            // Signal update
            if (maskVolumeAsset)
                dataUpdated = true;
        }

        protected void OnDisable()
        {
            MaskVolumeManager.manager.DeRegisterVolume(this);
        }

        internal bool IsDataAssigned()
        {
            return maskVolumeAsset && maskVolumeAsset.IsDataAssigned();
        }

        internal bool IsAssetMatchingResolution()
        {
            if (maskVolumeAsset)
            {
                return parameters.resolutionX == maskVolumeAsset.resolutionX &&
                       parameters.resolutionY == maskVolumeAsset.resolutionY &&
                       parameters.resolutionZ == maskVolumeAsset.resolutionZ;
            }
            return false;
        }

#if UNITY_EDITOR

        public void SetMaskVolumeAsset(MaskVolumeAsset asset)
        {
            maskVolumeAsset = asset;
            dataUpdated = true;
            
            UnityEditor.EditorUtility.SetDirty(this);
            
            if (maskVolumeAsset != null)
            {
                UnityEditor.EditorUtility.SetDirty(maskVolumeAsset);    
            }
        }

        protected void OnValidate()
        {
            parameters.Constrain();
        }
        
        internal void CreateAsset()
        {
            maskVolumeAsset = MaskVolumeAsset.CreateAsset(GetID());

            maskVolumeAsset.resolutionX = parameters.resolutionX;
            maskVolumeAsset.resolutionY = parameters.resolutionY;
            maskVolumeAsset.resolutionZ = parameters.resolutionZ;

            int numMasks = parameters.resolutionX * parameters.resolutionY * parameters.resolutionZ;
            MaskVolumePayload.Allocate(ref maskVolumeAsset.payload, numMasks);

            UnityEditor.EditorUtility.SetDirty(maskVolumeAsset);

            dataUpdated = true;
        }

        internal void ResampleAsset()
        {
            MaskVolumePayload oldPayload = maskVolumeAsset.payload;
            int oldResolutionX = maskVolumeAsset.resolutionX;
            int oldResolutionY = maskVolumeAsset.resolutionY;
            int oldResolutionZ = maskVolumeAsset.resolutionZ;
            
            int numMasks = parameters.resolutionX * parameters.resolutionY * parameters.resolutionZ;
            MaskVolumePayload newPayload = default;
            MaskVolumePayload.Allocate(ref newPayload, numMasks);

            if (oldResolutionX > 0 && oldResolutionY > 0 && oldResolutionZ > 0 && !MaskVolumePayload.IsEmpty(ref oldPayload))
            {
                for (int z = 0; z < parameters.resolutionZ; z++)
                {
                    CalculateResamplingWeights(oldResolutionZ, parameters.resolutionZ, z, out int oldZLow, out int oldZHigh, out float oldZLowWeight, out float oldZHighWeight);

                    for (int y = 0; y < parameters.resolutionY; y++)
                    {
                        CalculateResamplingWeights(oldResolutionY, parameters.resolutionY, y, out int oldYLow, out int oldYHigh, out float oldYLowWeight, out float oldYHighWeight);

                        for (int x = 0; x < parameters.resolutionX; x++)
                        {
                            CalculateResamplingWeights(oldResolutionX, parameters.resolutionX, x, out int oldXLow, out int oldXHigh, out float oldXLowWeight, out float oldXHighWeight);

                            MaskVolumePayload.Resample(ref oldPayload,
                                PayloadIndex(oldResolutionX, oldResolutionY, oldXLow, oldYLow, oldZLow), oldXLowWeight * oldYLowWeight * oldZLowWeight,
                                PayloadIndex(oldResolutionX, oldResolutionY, oldXHigh, oldYLow, oldZLow), oldXHighWeight * oldYLowWeight * oldZLowWeight,
                                PayloadIndex(oldResolutionX, oldResolutionY, oldXLow, oldYHigh, oldZLow), oldXLowWeight * oldYHighWeight * oldZLowWeight,
                                PayloadIndex(oldResolutionX, oldResolutionY, oldXHigh, oldYHigh, oldZLow), oldXHighWeight * oldYHighWeight * oldZLowWeight,
                                PayloadIndex(oldResolutionX, oldResolutionY, oldXLow, oldYLow, oldZHigh), oldXLowWeight * oldYLowWeight * oldZHighWeight,
                                PayloadIndex(oldResolutionX, oldResolutionY, oldXHigh, oldYLow, oldZHigh), oldXHighWeight * oldYLowWeight * oldZHighWeight,
                                PayloadIndex(oldResolutionX, oldResolutionY, oldXLow, oldYHigh, oldZHigh), oldXLowWeight * oldYHighWeight * oldZHighWeight,
                                PayloadIndex(oldResolutionX, oldResolutionY, oldXHigh, oldYHigh, oldZHigh), oldXHighWeight * oldYHighWeight * oldZHighWeight,
                                ref newPayload,
                                PayloadIndex(parameters.resolutionX, parameters.resolutionY, x, y, z));
                        }
                    }
                }
            }

            maskVolumeAsset.resolutionX = parameters.resolutionX;
            maskVolumeAsset.resolutionY = parameters.resolutionY;
            maskVolumeAsset.resolutionZ = parameters.resolutionZ;
           
            maskVolumeAsset.payload = newPayload;
            MaskVolumePayload.Dispose(ref oldPayload);
            
            UnityEditor.EditorUtility.SetDirty(maskVolumeAsset);

            dataUpdated = true;
        }

        static void CalculateResamplingWeights(int oldResolution, int newResolution, int targetTexel, out int oldLowTexel, out int oldHighTexel, out float oldLowWeight, out float oldHighWeight)
        {
            float sampleCoord = (targetTexel + 0.5f) / newResolution * oldResolution - 0.5f;

            if (oldResolution > 1)
            {
                oldLowTexel = Mathf.Clamp(Mathf.FloorToInt(sampleCoord), 0, oldResolution - 2);
                oldHighTexel = oldLowTexel + 1;
            }
            else
            {
                oldLowTexel = 0;
                oldHighTexel = 0;
            }

            oldHighWeight = sampleCoord - oldLowTexel;
            oldLowWeight = 1f - oldHighWeight;
            
            if (oldHighTexel == oldResolution)
                Debug.LogError(oldHighTexel);
        }

        static int PayloadIndex(int resolutionX, int resolutionY, int x, int y, int z)
        {
            return (z * resolutionY + y) * resolutionX + x;
        }

        private static bool ShouldDrawGizmos(MaskVolume maskVolume, out Camera camera)
        {
            camera = null;

            UnityEditor.SceneView sceneView = UnityEditor.SceneView.currentDrawingSceneView;

            if (sceneView == null)
                sceneView = UnityEditor.SceneView.lastActiveSceneView;

            if (sceneView == null)
                return false;

            if (sceneView != null && !sceneView.drawGizmos)
                return false;

            if (!maskVolume.enabled)
                return false;

            // Do not spend time rendering debug visualizations in playmode.
            if (UnityEditor.EditorApplication.isPlaying)
                return false;

            camera = sceneView.camera;
            return true;
        }

        internal void DrawSelectedMasks()
        {
            if (!ShouldDrawGizmos(this, out Camera camera))
            {
                return;
            }

            if (parameters.drawGizmos && HDRenderPipeline.currentPipeline != null)
            {
                OnValidate();
                HDRenderPipeline.currentPipeline.DrawMaskVolumeDebugSamplePreview(this, camera);
            }
        }

        VolumeGlobalUniqueID IVolumeGlobalUniqueIDOwnerEditorOnly.GetGlobalUniqueID() { return globalUniqueID; }
        void IVolumeGlobalUniqueIDOwnerEditorOnly.SetGlobalUniqueID(VolumeGlobalUniqueID id) { globalUniqueID = id; }
        void IVolumeGlobalUniqueIDOwnerEditorOnly.InitializeDuplicate()
        {
            // When a mask volume is duplicated, we unlink the asset.
            // This is not strictly necessary, it is valid at runtime to have multiple mask volumes who point to the same asset.
            // However, it is not valid to have multiple mask volumes pointing to the same asset at edit time.
            // To be extra safe, simply unlink the asset so a new one will be created next time we bake.
            maskVolumeAsset = null;
        }

        internal static Bounds ComputeBoundsWS(MaskVolume maskVolume)
        {
            return VolumeUtils.ComputeBoundsWS(maskVolume.transform, maskVolume.parameters.size);
        }

        internal static Matrix4x4 ComputeProbeIndex3DToPositionWSMatrix(MaskVolume maskVolume)
        {
            return VolumeUtils.ComputeProbeIndex3DToPositionWSMatrix(
                maskVolume.transform,
                maskVolume.parameters.size,
                maskVolume.parameters.resolutionX,
                maskVolume.parameters.resolutionY,
                maskVolume.parameters.resolutionZ
            );
        }

        internal static Vector3 ComputeCellSizeWS(MaskVolume maskVolume)
        {
            return VolumeUtils.ComputeCellSizeWS(
                maskVolume.parameters.size,
                maskVolume.parameters.resolutionX,
                maskVolume.parameters.resolutionY,
                maskVolume.parameters.resolutionZ
            );
        }

        internal static int ComputeProbeCount(MaskVolume maskVolume)
        {
            return VolumeUtils.ComputeProbeCount(
                maskVolume.parameters.resolutionX,
                maskVolume.parameters.resolutionY,
                maskVolume.parameters.resolutionZ
            );
        }
#endif
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
