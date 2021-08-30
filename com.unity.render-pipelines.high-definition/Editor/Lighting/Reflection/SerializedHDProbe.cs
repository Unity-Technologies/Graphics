using System;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    internal abstract class SerializedHDProbe
    {
        internal SerializedObject serializedObject;

        internal SerializedProperty bakedTexture;
        internal SerializedProperty customTexture;
        internal SerializedProbeSettings probeSettings;
        internal SerializedProbeSettingsOverride probeSettingsOverride;
        internal SerializedProperty proxyVolume;

        internal HDProbe target { get { return serializedObject.targetObject as HDProbe; } }

        internal SerializedHDProbe(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            bakedTexture = serializedObject.Find((HDProbe p) => p.bakedTexture);
            customTexture = serializedObject.Find((HDProbe p) => p.customTexture);
            proxyVolume = serializedObject.Find((HDProbe p) => p.proxyVolume);
            probeSettings = new SerializedProbeSettings(serializedObject.FindProperty("m_ProbeSettings"));
            probeSettingsOverride = new SerializedProbeSettingsOverride(serializedObject.FindProperty("m_ProbeSettingsOverride"));
        }

        internal virtual void Update()
        {
            serializedObject.Update();
            //InfluenceVolume does not have Update. Add it here if it have in the future.
            //CaptureSettings does not have Update. Add it here if it have in the future.
            //FrameSettings does not have Update. Add it here if it have in the future.
        }

        internal virtual void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
