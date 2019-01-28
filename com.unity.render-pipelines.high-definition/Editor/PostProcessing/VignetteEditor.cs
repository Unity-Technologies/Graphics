using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [VolumeComponentEditor(typeof(Vignette))]
    sealed class VignetteEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Mode;
        SerializedDataParameter m_Color;

        SerializedDataParameter m_Center;
        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_Smoothness;
        SerializedDataParameter m_Roundness;
        SerializedDataParameter m_Rounded;

        SerializedDataParameter m_Mask;
        SerializedDataParameter m_Opacity;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Vignette>(serializedObject);

            m_Mode = Unpack(o.Find(x => x.mode));
            m_Mode = Unpack(o.Find(x => x.mode));
            m_Color = Unpack(o.Find(x => x.color));

            m_Center = Unpack(o.Find(x => x.center));
            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_Smoothness = Unpack(o.Find(x => x.smoothness));
            m_Roundness = Unpack(o.Find(x => x.roundness));
            m_Rounded = Unpack(o.Find(x => x.rounded));

            m_Mask = Unpack(o.Find(x => x.mask));
            m_Opacity = Unpack(o.Find(x => x.opacity));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Mode);
            PropertyField(m_Color);

            if (m_Mode.value.intValue == (int)VignetteMode.Procedural)
            {
                PropertyField(m_Center);
                PropertyField(m_Intensity);
                PropertyField(m_Smoothness);
                PropertyField(m_Roundness);
                PropertyField(m_Rounded);
            }
            else
            {
                PropertyField(m_Mask);

                var mask = (target as Vignette).mask.value;

                // Checks import settings on the mask
                if (mask != null)
                {
                    var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(mask)) as TextureImporter;

                    // Fails when using an internal texture as you can't change import settings on
                    // builtin resources, thus the check for null
                    if (importer != null)
                    {
                        bool valid = importer.anisoLevel == 0
                            && importer.mipmapEnabled == false
                            && importer.alphaSource == TextureImporterAlphaSource.FromGrayScale
                            && importer.wrapMode == TextureWrapMode.Clamp;

                        if (!valid)
                            CoreEditorUtils.DrawFixMeBox("Invalid mask import settings.", () => SetMaskImportSettings(importer));
                    }
                }

                PropertyField(m_Opacity);
            }
        }

        void SetMaskImportSettings(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.SingleChannel;
            importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
            importer.anisoLevel = 0;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            AssetDatabase.Refresh();
        }
    }
}
