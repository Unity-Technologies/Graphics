# Custom pass variables

Arbitrary Output Variables (AOVs) are additional images that an [HDRP Camera](hdrp-camera-component-reference.md) can generate. They can output additional information per pixel, which you can use later for compositing or additional image processing (such as denoising).

Here is an example of three AOVs, containing from left to right the Albedo, Normal, and Object ID of each pixel:

![](Images/aov_example.png)

In HDRP, you can access and configure AOVs in the following ways:
- Using the [HDRP Compositor tool](graphics-compositor.md).
- Using the [Unity Recorder](https://docs.unity3d.com/Packages/com.unity.recorder@latest/index.html) and the [AOV Recorder](https://docs.unity3d.com/Packages/com.unity.aovrecorder@latest/index.html) packages.
- Using the scripting API to set up a custom AOV request in any HDRP Camera in your Scene.

The first two options offer a limited selection of AOVs in their User Interface, while the third option allows for much more flexibility on what data an HDRP Camera can output.

## Material property AOVs
Here is the list of Material properties that you can access with the AOV API.

| Material property | Description               |
|-------------------|---------------------------|
| **Albedo**        | Outputs the surface albedo. |
| **Normal**        | Outputs the surface normal. |
| **Smoothness**    | Outputs the surface smoothness. |
| **Ambient Occlusion** | Outputs the ambient occlusion (N/A for AxF). **Note**: the ambient occlusion this outputs does not include ray-traced/screen-space ambient occlusion from the [Ambient Occlusion override](Override-Ambient-Occlusion.md). It only includes ambient occlusion from materials in the Scene. |
| **Specular**      | Outputs the surface specularity. |
| **Alpha**         | Outputs the surface alpha (pixel coverage). |

## Lighting selection with AOVs
You can use AOVs to output the contribution from a selected list of [Lights](Light-Component.md), or you can use them to output only specific components of the lighting.

| Lighting property | Description               |
|-------------------|---------------------------|
| **DiffuseOnly**    | Renders only diffuse lighting (direct and indirect). |
| **SpecularOnly**   | Renders only specular lighting (direct and indirect). |
| **DirectDiffuseOnly** | Renders only direct diffuse lighting. |
| **DirectSpecularOnly** | Renders only direct specular lighting. |
| **IndirectDiffuseOnly** | Renders only indirect diffuse lighting. |
| **ReflectionOnly** | Renders only reflections. |
| **RefractionOnly** | Renders only refractions. |
| **EmissiveOnly** | Renders only emissive lighting. |

## Custom Pass AOVs
You can use AOVs to output the results of [custom passes](Custom-Pass.md). In particular, you can output the cumulative results of all custom passes that are active on every custom pass injection point. This can be useful to output arbitrary information that custom passes compute, such as the Object ID of the Scene GameObjects.

## Rendering precision
By default AOVs are rendering at the precision and format selected in the HDRP asset. If the  AOVRequest is configured with *SetOverrideRenderFormat* option set to true, then rendering will use the same precision as the user allocated AOV output buffer.

## Scripting API example
The following example script outputs albedo AOVs from an HDRP Camera and saves the resulting frames to disk as a sequence of .png images. To use the example script, attach it to an HDRP Camera and enter Play Mode.
```
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.HighDefinition.Attributes;

public class AovRecorder : MonoBehaviour
{
    RTHandle m_TmpRT;       // The RTHandle used to render the AOV
    Texture2D m_ReadBackTexture;

    int m_Frames = 0;

    // Start is called before the first frame update
    void Start()
    {
        var camera = gameObject.GetComponent<Camera>();
        if (camera != null)
        {
            var hdAdditionalCameraData = gameObject.GetComponent<HDAdditionalCameraData>();
            if (hdAdditionalCameraData != null)
            {
                // initialize a new AOV request
                var aovRequest = AOVRequest.NewDefault();

                AOVBuffers[] aovBuffers = null;
                CustomPassAOVBuffers[] customPassAovBuffers = null;

                // Request an AOV with the surface albedo
                aovRequest.SetFullscreenOutput(MaterialSharedProperty.Albedo);
                aovBuffers = new[] { AOVBuffers.Color };

                // Allocate the RTHandle that will store the intermediate results
                m_TmpRT = RTHandles.Alloc(camera.pixelWidth, camera.pixelHeight);

                // Add the request to a new AOVRequestBuilder
                var aovRequestBuilder = new AOVRequestBuilder();
                aovRequestBuilder.Add(aovRequest,
                    bufferId => m_TmpRT,
                    null,
                    aovBuffers,
                    customPassAovBuffers,
                    bufferId => m_TmpRT,
                    (cmd, textures, customPassTextures, properties) =>
                    {
                        // callback to read back the AOV data and write them to disk
                        if (textures.Count > 0)
                        {
                            m_ReadBackTexture = m_ReadBackTexture ?? new Texture2D(camera.pixelWidth, camera.pixelHeight, TextureFormat.RGBAFloat, false);
                            RenderTexture.active = textures[0].rt;
                            m_ReadBackTexture.ReadPixels(new Rect(0, 0, camera.pixelWidth, camera.pixelHeight), 0, 0, false);
                            m_ReadBackTexture.Apply();
                            RenderTexture.active = null;
                            byte[] bytes = m_ReadBackTexture.EncodeToPNG();
                            System.IO.File.WriteAllBytes($"output_{m_Frames++}.png", bytes);
                        }

                    });

                // Now build the AOV request
                var aovRequestDataCollection = aovRequestBuilder.Build();

                // And finally set the request to the camera
                hdAdditionalCameraData.SetAOVRequests(aovRequestDataCollection);
            }
        }
    }
}

```
