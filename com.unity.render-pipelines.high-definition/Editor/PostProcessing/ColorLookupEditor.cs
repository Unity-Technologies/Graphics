using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [VolumeComponentEditor(typeof(ColorLookup))]
    sealed class ColorLookupEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Texture;
        SerializedDataParameter m_Contribution;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ColorLookup>(serializedObject);

            m_Texture      = Unpack(o.Find(x => x.texture));
            m_Contribution = Unpack(o.Find(x => x.contribution));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Texture);

            var lut = m_Texture.value.objectReferenceValue;
            if (lut != null && !((ColorLookup)target).ValidateTexture())
                EditorGUILayout.HelpBox("Invalid lookup texture. It must be a 3D texture or render texture with the same size as set in the HDRP settings.", MessageType.Warning);

            PropertyField(m_Contribution);
        }
    }
}
