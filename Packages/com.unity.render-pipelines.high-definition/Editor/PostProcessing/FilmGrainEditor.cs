using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(FilmGrain))]
    sealed class FilmGrainEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Type;
        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_Response;
        SerializedDataParameter m_Texture;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<FilmGrain>(serializedObject);

            m_Type = Unpack(o.Find(x => x.type));
            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_Response = Unpack(o.Find(x => x.response));
            m_Texture = Unpack(o.Find(x => x.texture));
        }

        public override void OnInspectorGUI()
        {
            HDEditorUtils.EnsureFrameSetting(FrameSettingsField.FilmGrain);

            PropertyField(m_Type);

            if (m_Type.value.intValue == (int)FilmGrainLookup.Custom)
            {
                using (new IndentLevelScope())
                    PropertyField(m_Texture);

                var texture = (target as FilmGrain).texture.value;

                if (texture != null)
                {
                    var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;

                    // Fails when using an internal texture as you can't change import settings on
                    // builtin resources, thus the check for null
                    if (importer != null)
                    {
                        bool valid = importer.mipmapEnabled == false
                            && importer.alphaSource == TextureImporterAlphaSource.FromGrayScale
                            && importer.filterMode == FilterMode.Point
                            && importer.textureCompression == TextureImporterCompression.Uncompressed
                            && importer.textureType == TextureImporterType.SingleChannel;

                        if (!valid)
                            CoreEditorUtils.DrawFixMeBox("Invalid texture import settings.", () => SetTextureImportSettings(importer));
                    }
                }
            }

            PropertyField(m_Intensity);
            PropertyField(m_Response);
        }

        void SetTextureImportSettings(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.SingleChannel;
            importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            AssetDatabase.Refresh();
        }
    }
}
