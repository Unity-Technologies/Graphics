using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [VolumeComponentEditor(typeof(Tonemapping))]
    sealed class TonemappingEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Mode;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Tonemapping>(serializedObject);

            m_Mode = Unpack(o.Find(x => x.mode));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Mode);

            var mode = (TonemappingMode)m_Mode.value.intValue;

            bool suppoted = false;
            switch (mode)
            {
                case TonemappingMode.None:
                case TonemappingMode.Neutral:
                case TonemappingMode.ACES:
                    suppoted = true;
                    break;
            }

            if(!suppoted)
                EditorGUILayout.HelpBox("Only Modes None, Neutral, and ACES are supported. Fallback to Neutral will be used", MessageType.Warning);

            // Display a warning if the user is trying to use a tonemap while rendering in LDR
            var asset = UniversalRenderPipeline.asset;
            if (asset != null && !asset.supportsHDR)
            {
                EditorGUILayout.HelpBox("Tonemapping should only be used when working with High Dynamic Range (HDR). Please enable HDR through the active Render Pipeline Asset.", MessageType.Warning);
            }
        }
    }
}
