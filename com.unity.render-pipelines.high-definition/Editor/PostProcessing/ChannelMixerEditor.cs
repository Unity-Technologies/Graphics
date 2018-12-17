using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
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
                    if (GUILayout.Toggle(currentChannel == 0, CoreEditorUtils.GetContent("Red|Red output channel."), EditorStyles.miniButtonLeft)) currentChannel = 0;
                    if (GUILayout.Toggle(currentChannel == 1, CoreEditorUtils.GetContent("Green|Green output channel."), EditorStyles.miniButtonMid)) currentChannel = 1;
                    if (GUILayout.Toggle(currentChannel == 2, CoreEditorUtils.GetContent("Blue|Blue output channel."), EditorStyles.miniButtonRight)) currentChannel = 2;
                }
            }
            if (EditorGUI.EndChangeCheck())
                GUI.FocusControl(null);

            m_SelectedChannel.intValue = currentChannel;

            if (currentChannel == 0)
            {
                PropertyField(m_RedOutRedIn, CoreEditorUtils.GetContent("Red"));
                PropertyField(m_RedOutGreenIn, CoreEditorUtils.GetContent("Green"));
                PropertyField(m_RedOutBlueIn, CoreEditorUtils.GetContent("Blue"));
            }
            else if (currentChannel == 1)
            {
                PropertyField(m_GreenOutRedIn, CoreEditorUtils.GetContent("Red"));
                PropertyField(m_GreenOutGreenIn, CoreEditorUtils.GetContent("Green"));
                PropertyField(m_GreenOutBlueIn, CoreEditorUtils.GetContent("Blue"));
            }
            else
            {
                PropertyField(m_BlueOutRedIn, CoreEditorUtils.GetContent("Red"));
                PropertyField(m_BlueOutGreenIn, CoreEditorUtils.GetContent("Green"));
                PropertyField(m_BlueOutBlueIn, CoreEditorUtils.GetContent("Blue"));
            }
        }
    }
}
