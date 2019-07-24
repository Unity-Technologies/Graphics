using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeComponentEditor(typeof(ChannelMixer))]
    sealed class ChannelMixerEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_RedOutRedIn;
        SerializedDataParameter m_RedOutGreenIn;
        SerializedDataParameter m_RedOutBlueIn;
        SerializedDataParameter m_GreenOutRedIn;
        SerializedDataParameter m_GreenOutGreenIn;
        SerializedDataParameter m_GreenOutBlueIn;
        SerializedDataParameter m_BlueOutRedIn;
        SerializedDataParameter m_BlueOutGreenIn;
        SerializedDataParameter m_BlueOutBlueIn;
        SerializedProperty m_SelectedChannel;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ChannelMixer>(serializedObject);
            
            m_RedOutRedIn      = Unpack(o.Find(x => x.redOutRedIn));
            m_RedOutGreenIn    = Unpack(o.Find(x => x.redOutGreenIn));
            m_RedOutBlueIn     = Unpack(o.Find(x => x.redOutBlueIn));
            m_GreenOutRedIn    = Unpack(o.Find(x => x.greenOutRedIn));
            m_GreenOutGreenIn  = Unpack(o.Find(x => x.greenOutGreenIn));
            m_GreenOutBlueIn   = Unpack(o.Find(x => x.greenOutBlueIn));
            m_BlueOutRedIn     = Unpack(o.Find(x => x.blueOutRedIn));
            m_BlueOutGreenIn   = Unpack(o.Find(x => x.blueOutGreenIn));
            m_BlueOutBlueIn    = Unpack(o.Find(x => x.blueOutBlueIn));
            m_SelectedChannel  = o.Find("m_SelectedChannel");
        }

        public override void OnInspectorGUI()
        {
            int currentChannel = m_SelectedChannel.intValue;

            EditorGUI.BeginChangeCheck();
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Toggle(currentChannel == 0, EditorGUIUtility.TrTextContent("Red", "Red output channel."), EditorStyles.miniButtonLeft)) currentChannel = 0;
                    if (GUILayout.Toggle(currentChannel == 1, EditorGUIUtility.TrTextContent("Green", "Green output channel."), EditorStyles.miniButtonMid)) currentChannel = 1;
                    if (GUILayout.Toggle(currentChannel == 2, EditorGUIUtility.TrTextContent("Blue", "Blue output channel."), EditorStyles.miniButtonRight)) currentChannel = 2;
                }
            }
            if (EditorGUI.EndChangeCheck())
                GUI.FocusControl(null);

            m_SelectedChannel.intValue = currentChannel;

            if (currentChannel == 0)
            {
                PropertyField(m_RedOutRedIn, EditorGUIUtility.TrTextContent("Red"));
                PropertyField(m_RedOutGreenIn, EditorGUIUtility.TrTextContent("Green"));
                PropertyField(m_RedOutBlueIn, EditorGUIUtility.TrTextContent("Blue"));
            }
            else if (currentChannel == 1)
            {
                PropertyField(m_GreenOutRedIn, EditorGUIUtility.TrTextContent("Red"));
                PropertyField(m_GreenOutGreenIn, EditorGUIUtility.TrTextContent("Green"));
                PropertyField(m_GreenOutBlueIn, EditorGUIUtility.TrTextContent("Blue"));
            }
            else
            {
                PropertyField(m_BlueOutRedIn, EditorGUIUtility.TrTextContent("Red"));
                PropertyField(m_BlueOutGreenIn, EditorGUIUtility.TrTextContent("Green"));
                PropertyField(m_BlueOutBlueIn, EditorGUIUtility.TrTextContent("Blue"));
            }
        }
    }
}
