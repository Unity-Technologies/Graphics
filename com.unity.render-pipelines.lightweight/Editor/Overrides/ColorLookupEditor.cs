using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering.LWRP
{
    [VolumeComponentEditor(typeof(ColorLookup))]
    sealed class ColorLookupEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Texture;
        SerializedDataParameter m_Contribution;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ColorLookup>(serializedObject);

            m_Texture = Unpack(o.Find(x => x.texture));
            m_Contribution = Unpack(o.Find(x => x.contribution));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Texture, EditorGUIUtility.TrTextContent("Lookup Texture"));

            var lut = m_Texture.value.objectReferenceValue;
            if (lut != null && !((ColorLookup)target).ValidateLUT())
                EditorGUILayout.HelpBox("Invalid lookup texture. It must be a non-sRGB 2D texture or render texture with the same size as set in the LWRP settings.", MessageType.Warning);

            PropertyField(m_Contribution, EditorGUIUtility.TrTextContent("Contribution"));

            var asset = LightweightRenderPipeline.asset;
            if (asset != null)
            {
                if (asset.supportsHDR && asset.colorGradingMode == ColorGradingMode.HighDynamicRange)
                    EditorGUILayout.HelpBox("Color Grading Mode in the LWRP Settings is set to HDR. As a result, this LUT will be applied after the internal color grading and tonemapping have been applied.", MessageType.Info);
                else
                    EditorGUILayout.HelpBox("Color Grading Mode in the LWRP Settings is set to LDR. As a result, this LUT will be applied after tonemapping and before the internal color grading has been applied.", MessageType.Info);
            }
        }
    }
}
