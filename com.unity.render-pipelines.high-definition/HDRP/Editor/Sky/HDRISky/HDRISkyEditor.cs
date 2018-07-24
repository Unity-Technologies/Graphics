using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(HDRISky))]
    public class HDRISkyEditor
        : SkySettingsEditor
    {
        SerializedDataParameter m_hdriSky;
        SerializedDataParameter m_DesiredLuxValue;
        SerializedDataParameter m_IntensityMode;
        SerializedDataParameter m_UpperHemisphereLuxValue;
        
        RTHandleSystem.RTHandle m_IntensityTexture;
        Material m_IntegrateHDRISkyMaterial; // Compute the HDRI sky intensity in lux for the skybox
        Texture2D readBackTexture;

        public override void OnEnable()
        {
            base.OnEnable();

            // HDRI sky does not have control over sun display.
            m_CommonUIElementsMask = 0xFFFFFFFF & ~(uint)(SkySettingsUIElement.IncludeSunInBaking);

            var o = new PropertyFetcher<HDRISky>(serializedObject);
            m_hdriSky = Unpack(o.Find(x => x.hdriSky));
            m_DesiredLuxValue = Unpack(o.Find(x => x.desiredLuxValue));
            m_IntensityMode = Unpack(o.Find(x => x.skyIntensityMode));
            m_UpperHemisphereLuxValue = Unpack(o.Find(x => x.upperHemisphereLuxValue));
            
            m_IntensityTexture = RTHandles.Alloc(1, 1, colorFormat: RenderTextureFormat.ARGBFloat, sRGB: false);
            var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            m_IntegrateHDRISkyMaterial = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.integrateHdriSky);
            readBackTexture = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, false);
        }

        public override void OnDisable()
        {
            if (m_IntensityTexture != null)
                RTHandles.Release(m_IntensityTexture);
            
            readBackTexture = null;
        }

        public void GetUpperHemisphereLuxValue()
        {
            Cubemap hdri = m_hdriSky.value.objectReferenceValue as Cubemap;
            // GPU texture readback
            HDRISky sky = target as HDRISky;
            float omegaP = (Mathf.PI * 4) / (6.0f * hdri.width * hdri.width);
            m_IntegrateHDRISkyMaterial.SetTexture(HDShaderIDs._Cubemap, hdri);
            m_IntegrateHDRISkyMaterial.SetFloat(HDShaderIDs._InvOmegaP, 1.0f / omegaP);

            Graphics.Blit(Texture2D.whiteTexture, m_IntensityTexture.rt, m_IntegrateHDRISkyMaterial);

            RenderTexture.active = m_IntensityTexture.rt;
            readBackTexture.ReadPixels(new Rect(0.0f, 0.0f, 1, 1), 0, 0);
            RenderTexture.active = null;

            Color hdriIntensity = readBackTexture.GetPixel(0, 0);
            m_UpperHemisphereLuxValue.value.floatValue = hdriIntensity.r;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            PropertyField(m_hdriSky);
            if (EditorGUI.EndChangeCheck())
            {
                GetUpperHemisphereLuxValue();
            }

            EditorGUILayout.Space();
            
            PropertyField(m_IntensityMode);

            if (m_IntensityMode.value.enumValueIndex == (int)SkyIntensityMode.Lux)
            {
                EditorGUI.indentLevel++;
                PropertyField(m_DesiredLuxValue);
                // Hide exposure and multiplier
                m_CommonUIElementsMask &= ~(uint)(SkySettingsUIElement.Exposure | SkySettingsUIElement.Multiplier);
                m_CommonUIElementsMask |= (uint)SkySettingsUIElement.IndentExposureAndMultiplier;

                // Show the multiplier as read-only
                EditorGUI.BeginDisabledGroup(true);
                PropertyField(m_UpperHemisphereLuxValue);
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
            }
            else
            {
                m_CommonUIElementsMask |= (uint)(SkySettingsUIElement.Exposure | SkySettingsUIElement.Multiplier);
            }

            base.CommonSkySettingsGUI();
        }
    }
}