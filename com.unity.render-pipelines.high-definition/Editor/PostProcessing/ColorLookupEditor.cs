using UnityEditor.Rendering;
using UnityEngine;
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
            if (lut != null)
            {
                bool valid = false;

                if (lut is Texture3D)
                {
                    var o = (Texture3D)lut;
                    if (o.width == o.height && o.height == o.depth)
                        valid = true;
                }
                else if (lut is RenderTexture)
                {
                    var o = (RenderTexture)lut;
                    if (o.width == o.height && o.height == o.volumeDepth)
                        valid = true;
                }

                if (!valid)
                    EditorGUILayout.HelpBox("Custom LUTs have to be log-encoded 3D textures or 3D render textures.", MessageType.Warning);
            }

            PropertyField(m_Contribution);
        }
    }
}
