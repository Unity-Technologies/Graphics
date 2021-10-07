using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeComponentEditor(typeof(LiftGammaGain))]
    sealed class LiftGammaGainEditor : VolumeComponentEditor
    {
        static class Styles
        {
            public static readonly GUIContent liftLabel = EditorGUIUtility.TrTextContent("Lift", "Use this control to apply a hue to the dark tones (shadows) and adjust their level.");
            public static readonly GUIContent gammaLabel = EditorGUIUtility.TrTextContent("Gamma", "Use this control to apply a hue to the mid-range tones and adjust their level.");
            public static readonly GUIContent gainLabel = EditorGUIUtility.TrTextContent("Gain", "Use this control to apply a hue to the highlights and adjust their level.");
        }

        SerializedDataParameter m_Lift;
        SerializedDataParameter m_Gamma;
        SerializedDataParameter m_Gain;

        readonly TrackballUIDrawer m_TrackballUIDrawer = new TrackballUIDrawer();

        public override void OnEnable()
        {
            var o = new PropertyFetcher<LiftGammaGain>(serializedObject);

            m_Lift = Unpack(o.Find(x => x.lift));
            m_Gamma = Unpack(o.Find(x => x.gamma));
            m_Gain = Unpack(o.Find(x => x.gain));
        }

        public override void OnInspectorGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                m_TrackballUIDrawer.OnGUI(m_Lift.value, m_Lift.overrideState, Styles.liftLabel, GetLiftValue);
                GUILayout.Space(4f);
                m_TrackballUIDrawer.OnGUI(m_Gamma.value, m_Gamma.overrideState, Styles.gammaLabel, GetLiftValue);
                GUILayout.Space(4f);
                m_TrackballUIDrawer.OnGUI(m_Gain.value, m_Gain.overrideState, Styles.gainLabel, GetLiftValue);
            }
        }

        Vector3 GetLiftValue(Vector4 x) => new Vector3(x.x + x.w, x.y + x.w, x.z + x.w);
    }
}
