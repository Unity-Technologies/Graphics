using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HairData : HDTargetData
    {
        public enum MaterialType
        {
            Approximate,
            Physical,
            PhysicalCinematic
        }

        public enum DirectionalFractionMode
        {
            ScatteringData,
            ShadowMap
        }

        public enum ColorParameterization
        {
            BaseColor,
            Melanin,
            Absorption
        }

        public enum GeometryType
        {
            Cards,
            Strands
        }

        public enum CinematicSampleCount
        {
            Low,
            Medium,
            High,
            Ultra
        }

        [SerializeField]
        MaterialType m_MaterialType;
        public MaterialType materialType
        {
            get => m_MaterialType;
            set => m_MaterialType = value;
        }

        [SerializeField]
        DirectionalFractionMode m_DirectionalFractionMode;

        public DirectionalFractionMode directionalFractionMode
        {
            get => m_DirectionalFractionMode;
            set => m_DirectionalFractionMode = value;
        }

        [SerializeField]
        ColorParameterization m_ColorParameterization = ColorParameterization.BaseColor;

        public ColorParameterization colorParameterization
        {
            get => m_ColorParameterization;
            set => m_ColorParameterization = value;
        }

        [SerializeField]
        GeometryType m_GeometryType;

        public GeometryType geometryType
        {
            get => m_GeometryType;
            set => m_GeometryType = value;
        }

        [SerializeField]
        CinematicSampleCount m_EnvironmentSamples = CinematicSampleCount.Medium;

        public CinematicSampleCount environmentSamples
        {
            get => m_EnvironmentSamples;
            set => m_EnvironmentSamples = value;
        }

        [SerializeField]
        CinematicSampleCount m_AreaLightSamples = CinematicSampleCount.Medium;

        public CinematicSampleCount areaLightSamples
        {
            get => m_AreaLightSamples;
            set => m_AreaLightSamples = value;
        }
    }
}
