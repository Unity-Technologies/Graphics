using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class HDRenderPipeWindow : EditorWindow
    {
        [MenuItem("HDRenderPipeline/Configure Overrides")]
        static void ConfigureOverrides()
        {
            GetWindow<HDRenderPipeWindow>().Show();
        }

        void OnGUI()
        {
            CommonSettingsSingleton.overrideSettings = (CommonSettings)EditorGUILayout.ObjectField(new GUIContent("Common Settings"), CommonSettingsSingleton.overrideSettings, typeof(CommonSettings), false);
            SkyParametersSingleton.overrideSettings = (SkyParameters)EditorGUILayout.ObjectField(new GUIContent("Sky Settings"), SkyParametersSingleton.overrideSettings, typeof(SkyParameters), false);
            SubsurfaceScatteringSettings.overrideSettings = (SubsurfaceScatteringParameters)EditorGUILayout.ObjectField(new GUIContent("Subsurface Scattering Settings"), SubsurfaceScatteringSettings.overrideSettings, typeof(SubsurfaceScatteringParameters), false);

            if (GUILayout.Button("Create new common settings"))
            {
                var instance = CreateInstance<CommonSettings>();
                AssetDatabase.CreateAsset(instance, "Assets/NewCommonSettings.asset");
            }

            if (GUILayout.Button("Create new HDRI sky params"))
            {
                var instance = CreateInstance<HDRISkyParameters>();
                AssetDatabase.CreateAsset(instance, "Assets/NewHDRISkyParameters.asset");
            }

            if (GUILayout.Button("Create new Procedural sky params"))
            {
                var instance = CreateInstance<ProceduralSkyParameters>();
                AssetDatabase.CreateAsset(instance, "Assets/NewProceduralSkyParameters.asset");
            }

            if (GUILayout.Button("Create new SSS params"))
            {
                var instance = CreateInstance<SubsurfaceScatteringParameters>();
                AssetDatabase.CreateAsset(instance, "Assets/NewSssParameters.asset");
            }
        }
    }
}
