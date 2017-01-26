using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class CommonSettings 
        : MonoBehaviour
    {
        [SerializeField] private string     m_SkyRendererTypeName = ""; // Serialize a string because serialize a Type.

        public Type skyRendererType
        {
            set { m_SkyRendererTypeName = value != null ? value.FullName : ""; OnSkyRendererChanged(); }
            get { return m_SkyRendererTypeName == "" ? null : Assembly.GetAssembly(typeof(CommonSettings)).GetType(m_SkyRendererTypeName); }
        }

        // Shadows
        [SerializeField] float m_ShadowMaxDistance   = ShadowSettings.Default.maxShadowDistance;
        [SerializeField] int   m_ShadowCascadeCount  = ShadowSettings.Default.directionalLightCascadeCount;
        [SerializeField] float m_ShadowCascadeSplit0 = ShadowSettings.Default.directionalLightCascades.x;
        [SerializeField] float m_ShadowCascadeSplit1 = ShadowSettings.Default.directionalLightCascades.y;
        [SerializeField] float m_ShadowCascadeSplit2 = ShadowSettings.Default.directionalLightCascades.z;

        public float shadowMaxDistance   { set { m_ShadowMaxDistance   = value; OnValidate(); } get { return m_ShadowMaxDistance; } }
        public int   shadowCascadeCount  { set { m_ShadowCascadeCount  = value; OnValidate(); } get { return m_ShadowCascadeCount; } }
        public float shadowCascadeSplit0 { set { m_ShadowCascadeSplit0 = value; OnValidate(); } get { return m_ShadowCascadeSplit0; } }
        public float shadowCascadeSplit1 { set { m_ShadowCascadeSplit1 = value; OnValidate(); } get { return m_ShadowCascadeSplit1; } }
        public float shadowCascadeSplit2 { set { m_ShadowCascadeSplit2 = value; OnValidate(); } get { return m_ShadowCascadeSplit2; } }

        // Subsurface scattering
        [SerializeField] Color m_SssProfileFilter1Variance  = SubsurfaceScatteringProfile.Default.filter1Variance;
        [SerializeField] Color m_SssProfileFilter2Variance  = SubsurfaceScatteringProfile.Default.filter2Variance;
        [SerializeField] float m_SssProfileFilterLerpWeight = SubsurfaceScatteringProfile.Default.filterLerpWeight;
        [SerializeField] float m_SssBilateralScale          = SubsurfaceScatteringParameters.Default.bilateralScale;
        
        public Color sssProfileFilter1Variance  { set { m_SssProfileFilter1Variance  = value; OnValidate(); } get { return m_SssProfileFilter1Variance; } }
        public Color sssProfileFilter2Variance  { set { m_SssProfileFilter2Variance  = value; OnValidate(); } get { return m_SssProfileFilter2Variance; } }
        public float sssProfileFilterLerpWeight { set { m_SssProfileFilterLerpWeight = value; OnValidate(); } get { return m_SssProfileFilterLerpWeight; } }
        public float sssBilateralScale          { set { m_SssBilateralScale          = value; OnValidate(); } get { return m_SssBilateralScale; } }

        void OnEnable()
        {
            HDRenderPipeline renderPipeline = Utilities.GetHDRenderPipeline();
            if (renderPipeline == null)
            {
                return;
            }

            if (renderPipeline.commonSettings == null)
                renderPipeline.commonSettings = this;
            else if (renderPipeline.commonSettings != this)
                Debug.LogWarning("Only one CommonSettings can be setup at a time.");

            OnSkyRendererChanged();
        }

        void OnDisable()
        {
            HDRenderPipeline renderPipeline = Utilities.GetHDRenderPipeline();
            if (renderPipeline == null)
            {
                return;
            }

            if (renderPipeline.commonSettings == this)
                renderPipeline.commonSettings = null;
        }

        void OnValidate()
        {
            m_ShadowMaxDistance   = Mathf.Max(0.0f, m_ShadowMaxDistance);
            m_ShadowCascadeCount  = Math.Min(4, Math.Max(1, m_ShadowCascadeCount));
            m_ShadowCascadeSplit0 = Mathf.Clamp01(m_ShadowCascadeSplit0);
            m_ShadowCascadeSplit1 = Mathf.Clamp01(m_ShadowCascadeSplit1);
            m_ShadowCascadeSplit2 = Mathf.Clamp01(m_ShadowCascadeSplit2);

            m_SssProfileFilter1Variance.r = Mathf.Max(0.1f, m_SssProfileFilter1Variance.r);
            m_SssProfileFilter1Variance.g = Mathf.Max(0.1f, m_SssProfileFilter1Variance.g);
            m_SssProfileFilter1Variance.b = Mathf.Max(0.1f, m_SssProfileFilter1Variance.b);
            m_SssProfileFilter1Variance.a = 0.0f;
            m_SssProfileFilter2Variance.r = Mathf.Max(0.1f, m_SssProfileFilter2Variance.r);
            m_SssProfileFilter2Variance.g = Mathf.Max(0.1f, m_SssProfileFilter2Variance.g);
            m_SssProfileFilter2Variance.b = Mathf.Max(0.1f, m_SssProfileFilter2Variance.b);
            m_SssProfileFilter2Variance.a = 0.0f;
            m_SssProfileFilterLerpWeight  = Mathf.Clamp01(m_SssProfileFilterLerpWeight);
            m_SssBilateralScale           = Mathf.Clamp01(m_SssBilateralScale);

            OnSkyRendererChanged();
        }

        void OnSkyRendererChanged()
        {
            HDRenderPipeline renderPipeline = Utilities.GetHDRenderPipeline();
            if (renderPipeline == null)
            {
                return;
            }

            renderPipeline.InstantiateSkyRenderer(skyRendererType);

            List<SkyParameters> result = new List<SkyParameters>();
            gameObject.GetComponents<SkyParameters>(result);

            Type skyParamType = renderPipeline.skyManager.GetSkyParameterType();

            // Disable all incompatible sky parameters and enable the compatible one
            bool found = false;
            foreach (SkyParameters param in result)
            {
                if (param.GetType() == skyParamType)
                {
                    // This is a workaround the fact that we can't control the order in which components are initialized.
                    // So it can happen that a given SkyParameter is OnEnabled before the CommonSettings and so fail the setup because the SkyRenderer is not yet initialized.
                    // So we disable it to for OnEnable to be called again.
                    param.enabled = false;

                    param.enabled = true;
                    found = true;
                }
                else
                {
                    param.enabled = false;
                }
            }

            // If it does not exist, create the parameters
            if (!found && skyParamType != null)
            {
                gameObject.AddComponent(skyParamType);
            }
        }
    }
}
