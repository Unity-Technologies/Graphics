using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor.Rendering.HighDefinition;
#endif
using UnityEngine.Rendering.LookDev;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline : IDataProvider
    {
        struct LookDevDataForHDRP
        {
            public HDAdditionalCameraData additionalCameraData;
            public HDAdditionalLightData additionalLightData;
            public VisualEnvironment visualEnvironment;
            public HDRISky sky;
            public Volume volume;
        }

        void IDataProvider.FirstInitScene(StageRuntimeInterface SRI)
        {
            Camera camera = SRI.camera;
            camera.allowHDR = true;

            var additionalCameraData = camera.gameObject.AddComponent<HDAdditionalCameraData>();
            additionalCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            additionalCameraData.clearDepth = true;
            additionalCameraData.backgroundColorHDR = camera.backgroundColor;
            additionalCameraData.volumeAnchorOverride = camera.transform;
            additionalCameraData.volumeLayerMask = 1 << 31; //31 is the culling layer used in LookDev

            Light light = SRI.sunLight;
            HDAdditionalLightData additionalLightData = light.gameObject.AddComponent<HDAdditionalLightData>();
#if UNITY_EDITOR
            HDAdditionalLightData.InitDefaultHDAdditionalLightData(additionalLightData);
#endif
            additionalLightData.intensity = 0f;
            additionalLightData.SetShadowResolution(2048);

            GameObject volumeGO = SRI.AddGameObject(persistent: true);
            volumeGO.name = "SkyManagementVolume";
            Volume volume = volumeGO.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = float.MaxValue;
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;

            HDShadowSettings shadows = profile.Add<HDShadowSettings>();
            shadows.maxShadowDistance.Override(25f);
            shadows.cascadeShadowSplitCount.Override(2);

            VisualEnvironment visualEnvironment = profile.Add<VisualEnvironment>();
            visualEnvironment.fogType.Override(FogType.None);

            HDRISky sky = profile.Add<HDRISky>();

            SRI.SRPData = new LookDevDataForHDRP()
            {
                additionalCameraData = additionalCameraData,
                additionalLightData = additionalLightData,
                visualEnvironment = visualEnvironment,
                sky = sky,
                volume = volume
            };

            //[TODO: remove]
            //temp for debug: show component in scene hierarchy
            //UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camera.gameObject, GameObject.Find("Main Camera").scene);
            //camera.gameObject.hideFlags = HideFlags.None;
            //UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(volumeGO, GameObject.Find("Main Camera").scene);
            //volumeGO.hideFlags = HideFlags.None;
            //UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(light.gameObject, GameObject.Find("Main Camera").scene);
            //light.gameObject.hideFlags = HideFlags.None;
        }

        void IDataProvider.UpdateSky(Camera camera, Sky sky, StageRuntimeInterface SRI)
        {
            LookDevDataForHDRP data = (LookDevDataForHDRP)SRI.SRPData;
            if (sky.cubemap == null)
            {
                data.visualEnvironment.skyType.Override((int)0); //Skytype.None do not really exist
                data.additionalCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            }
            else
            {
                data.visualEnvironment.skyType.Override((int)SkyType.HDRI);
                data.sky.hdriSky.Override(sky.cubemap);
                data.sky.rotation.Override(sky.longitudeOffset);
                data.additionalCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Sky;
            }
        }

        IEnumerable<string> IDataProvider.supportedDebugModes
            => new[]
            {
                "Albedo",
                "Normal",
                "Smoothness",
                "AmbientOcclusion",
                "Metal",
                "Specular",
                "Alpha"
            };

        void IDataProvider.UpdateDebugMode(int debugIndex)
            => debugDisplaySettings.SetDebugViewCommonMaterialProperty((Attributes.MaterialSharedProperty)(debugIndex + 1));

        void IDataProvider.GetShadowMask(ref RenderTexture output, StageRuntimeInterface SRI)
        {
            LookDevDataForHDRP data = (LookDevDataForHDRP)SRI.SRPData;
            Color oldBackgroundColor = data.additionalCameraData.backgroundColorHDR;
            var oldClearMode = data.additionalCameraData.clearColorMode;
            data.additionalCameraData.backgroundColorHDR = Color.white;
            data.additionalCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            data.additionalLightData.intensity = 1f;
            debugDisplaySettings.SetShadowDebugMode(ShadowMapDebugMode.SingleShadow);
            SRI.camera.targetTexture = output;
            SRI.camera.Render();
            debugDisplaySettings.SetShadowDebugMode(ShadowMapDebugMode.None);
            data.additionalLightData.intensity = 0f;
            data.additionalCameraData.backgroundColorHDR = oldBackgroundColor;
            data.additionalCameraData.clearColorMode = oldClearMode;
        }
    }
}
