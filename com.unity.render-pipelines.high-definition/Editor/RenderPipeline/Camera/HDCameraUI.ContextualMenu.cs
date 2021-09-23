using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDCameraUI
    {
        [MenuItem("CONTEXT/Camera/Reset", false, 0)]
        static void ResetCamera(MenuCommand menuCommand)
        {
            // Grab the current HDRP asset, we should not be executing this code if HDRP is null
            var hdrp = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
            if (hdrp == null)
                return;

            GameObject go = ((Camera)menuCommand.context).gameObject;
            Assert.IsNotNull(go);

            Camera camera = go.GetComponent<Camera>();
            Assert.IsNotNull(camera);

            // Try to grab the HDAdditionalCameraData component, it is possible that the component is null of the camera was created without an asset assigned and the inspector
            // was kept on while assigning the asset and then triggering the reset.
            HDAdditionalCameraData cameraAdditionalData;
            if ((!go.TryGetComponent<HDAdditionalCameraData>(out cameraAdditionalData)))
            {
                cameraAdditionalData = go.AddComponent<HDAdditionalCameraData>();
            }
            Assert.IsNotNull(cameraAdditionalData);

            Undo.SetCurrentGroupName("Reset HD Camera");
            Undo.RecordObjects(new UnityEngine.Object[] { camera, cameraAdditionalData }, "Reset HD Camera");
            camera.Reset();
            // To avoid duplicating init code we copy default settings to Reset additional data
            // Note: we can't call this code inside the HDAdditionalCameraData, thus why we don't wrap it in a Reset() function
            HDUtils.s_DefaultHDAdditionalCameraData.CopyTo(cameraAdditionalData);
        }
    }
}
