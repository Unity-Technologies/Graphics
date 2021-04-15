using UnityEngine;
using System.Linq;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

namespace UnityEditor.Rendering.Universal
{
    class SerializedUniversalGlobalSettings
    {
        public SerializedObject serializedObject;
        private List<UniversalGlobalSettings> serializedSettings = new List<UniversalGlobalSettings>();

        public SerializedProperty lightLayerName0;
        public SerializedProperty lightLayerName1;
        public SerializedProperty lightLayerName2;
        public SerializedProperty lightLayerName3;
        public SerializedProperty lightLayerName4;
        public SerializedProperty lightLayerName5;
        public SerializedProperty lightLayerName6;
        public SerializedProperty lightLayerName7;

        public SerializedUniversalGlobalSettings(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            // do the cast only once
            foreach (var currentSetting in serializedObject.targetObjects)
            {
                if (currentSetting is UniversalGlobalSettings urpSettings)
                    serializedSettings.Add(urpSettings);
                else
                    throw new System.Exception($"Target object has an invalid object, objects must be of type {typeof(UniversalGlobalSettings)}");
            }


            lightLayerName0 = serializedObject.Find((UniversalGlobalSettings s) => s.lightLayerName0);
            lightLayerName1 = serializedObject.Find((UniversalGlobalSettings s) => s.lightLayerName1);
            lightLayerName2 = serializedObject.Find((UniversalGlobalSettings s) => s.lightLayerName2);
            lightLayerName3 = serializedObject.Find((UniversalGlobalSettings s) => s.lightLayerName3);
            lightLayerName4 = serializedObject.Find((UniversalGlobalSettings s) => s.lightLayerName4);
            lightLayerName5 = serializedObject.Find((UniversalGlobalSettings s) => s.lightLayerName5);
            lightLayerName6 = serializedObject.Find((UniversalGlobalSettings s) => s.lightLayerName6);
            lightLayerName7 = serializedObject.Find((UniversalGlobalSettings s) => s.lightLayerName7);
        }
    }
}
