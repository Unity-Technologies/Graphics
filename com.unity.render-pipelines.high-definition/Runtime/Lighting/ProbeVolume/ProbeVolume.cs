using System;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEditor.Experimental;
using Unity.Collections;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    internal enum ProbeSpacingMode
    {
        Density = 0,
        Resolution
    };

    [GenerateHLSL]
    internal enum VolumeBlendMode
    {
        Normal = 0,
        Additive,
        Subtractive
    }

    [Serializable]
    internal struct ProbeVolumeArtistParameters
    {
        public bool drawProbes;
        public Color debugColor;
        public int payloadIndex;
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
        public Vector4 octahedralDepthScaleBias;

        public ProbeSpacingMode probeSpacingMode;

        public float densityX;
        public float densityY;
        public float densityZ;

        public int resolutionX;
        public int resolutionY;
        public int resolutionZ;

        public VolumeBlendMode volumeBlendMode;
        public float weight;

        public float backfaceTolerance;
        public int dilationIterations;

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

        public ProbeVolumeArtistParameters(Color debugColor)
        {
            this.debugColor = debugColor;
            this.drawProbes = false;
            this.payloadIndex = -1;
            this.size = Vector3.one;
            this.m_PositiveFade = Vector3.zero;
            this.m_NegativeFade = Vector3.zero;
            this.m_UniformFade = 0;
            this.advancedFade = false;
            this.distanceFadeStart = 10000.0f;
            this.distanceFadeEnd = 10000.0f;
            this.scale = Vector3.zero;
            this.bias = Vector3.zero;
            this.octahedralDepthScaleBias = Vector4.zero;
            this.probeSpacingMode = ProbeSpacingMode.Density;
            this.resolutionX = 4;
            this.resolutionY = 4;
            this.resolutionZ = 4;
            this.densityX = (float)this.resolutionX / this.size.x;
            this.densityY = (float)this.resolutionY / this.size.y;
            this.densityZ = (float)this.resolutionZ / this.size.z;
            this.volumeBlendMode = VolumeBlendMode.Normal;
            this.weight = 1;
            this.dilationIterations = 2;
            this.backfaceTolerance = 0.25f;
            this.lightLayers = LightLayerEnum.LightLayerDefault;
        }

        internal void Constrain()
        {
            this.distanceFadeStart = Mathf.Max(0, this.distanceFadeStart);
            this.distanceFadeEnd = Mathf.Max(this.distanceFadeStart, this.distanceFadeEnd);

            switch (this.probeSpacingMode)
            {
                case ProbeSpacingMode.Density:
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

                case ProbeSpacingMode.Resolution:
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
                        Debug.Assert(false, "Error: ProbeVolume: Encountered unsupported Probe Spacing Mode: " + this.probeSpacingMode);
                        break;
                    }
            }
        }
    } // class ProbeVolumeArtistParameters

    [ExecuteAlways]
    [AddComponentMenu("Light/Experimental/Probe Volume")]
    public class ProbeVolume : MonoBehaviour
    {
        [SerializeField] internal ProbeVolumeArtistParameters parameters = new ProbeVolumeArtistParameters(Color.white);

        public Vector3 GetExtents()
        {
            return parameters.size;
        }
    } 
} // UnityEngine.Experimental.Rendering.HDPipeline
