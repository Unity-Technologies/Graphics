using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Experimental.LookDev;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class HDRenderPipeline : IDataProvider
    {
        struct LookDevDataForHDRP
        {
            public HDAdditionalCameraData additionalCameraData;
            public VisualEnvironment visualEnvironment;
            public HDRISky sky;
            public Volume volume;
        }

        void IDataProvider.FirstInitScene(StageRuntimeInterface SRI)
        {
            Camera camera = SRI.camera;
            camera.allowHDR = true;

            var additionalData = camera.gameObject.AddComponent<HDAdditionalCameraData>();
            additionalData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            additionalData.clearDepth = true;
            additionalData.backgroundColorHDR = camera.backgroundColor;
            additionalData.volumeAnchorOverride = camera.transform;
            additionalData.volumeLayerMask = 1 << 31; //31 is the culling layer used in LookDev

            GameObject volumeGO = SRI.AddGameObject(persistent: true);
            volumeGO.name = "SkyManagementVolume";
            Volume volume = volumeGO.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = float.MaxValue;
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = profile;
            VisualEnvironment visualEnvironment = profile.Add<VisualEnvironment>();
            HDRISky sky = profile.Add<HDRISky>();

            SRI.SRPData = new LookDevDataForHDRP()
            {
                additionalCameraData = additionalData,
                visualEnvironment = visualEnvironment,
                sky = sky,
                volume = volume
            };


            //temp for debug: show component in scene hierarchy
            //UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camera.gameObject, GameObject.Find("Main Camera").scene);
            //camera.gameObject.hideFlags = HideFlags.None;
            //UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(volumeGO, GameObject.Find("Main Camera").scene);
            //volumeGO.hideFlags = HideFlags.None;
        }

        void IDataProvider.UpdateSky(Camera camera, Sky sky, StageRuntimeInterface SRI)
        {
            //[TODO: add rotation and intensity]
            LookDevDataForHDRP data = (LookDevDataForHDRP)SRI.SRPData;
            if (sky.cubemap == null)
            {
                data.visualEnvironment.skyType.Override((int)0); //Skytype.None do not really exist
                data.additionalCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            }
            else
            {
                data.visualEnvironment.skyType.Override((int)SkyType.HDRISky);
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
    }
}
