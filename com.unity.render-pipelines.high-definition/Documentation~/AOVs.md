# Arbitrary Output Variables

Arbitrary Output Variables (AOVs) are additional images that an HDRP camera can generate. Typically they are used to output additional information per pixel, which can be used later for compositing or additional image processing (such as denoising).

Here is an example of three AOVs, containing from left to right the Albedo, Normal and Object ID of each pixel:

![](Images/aov_example.png)

In HDRP you can access and configure AOVs in the following ways:
- Using the [HDRP Compositor Tool](Compositor-Main).
- Using the Unity Recorder and the AOV Recorder Package.
- Using the scripting API to setup a custom AOV request in any HDRP Camera of your scene.

The first two options offer a limited selection of AOVs in their User Interface, while the third option allows much more flexibility on the nature of data that will be outputted.

## Material Property AOVs
Here is a list of material properties that can be outputted using the AOV API.

| Material Property | Description               |
|-------------------|---------------------------|
| Normal            | Output the surface albedo |
| Albedo            | Output the surface normal |
| Smoothness        | Output the surface smoothness |
| Ambient Occlusion | Output the ambient occlusion (N/A for AxF) |
| Specular          | Output the surface specularity |
| Alpha             | Output the surface alpha (pixel coverage) |

## Lighting Selection with AOVs
AOVs can also be used to output the contribution from a selected list of lights, or they can be used to output only specific components of the lighting.

| Lighting Property | Description               |
|-------------------|---------------------------|
| DiffuseOnly        | Render only diffuse lighting (direct and indirect) |
| SpecularOnly       | Render only specular lighting (direct and indirect) |
| DirectDiffuseOnly  | Render only direct diffuse lighting |
| DirectSpecularOnly  | Render only direct specular lighting |
| IndirectDiffuseOnly  | Render only indirect diffuse lighting |
| ReflectionOnly  | Render only reflections |
| RefractionOnly  | Render only refractions |
| EmissiveOnly  | Render only emissive lighting |

## Custom Pass AOVs
Finally, AOVs can also be used to output the results of [custom passes](Custom-Pass). In particular, you can output the cumulative results of all custom passes that are active on every custom pass injection point. This can be useful to output arbitrary information that is computed in custom passes, such as the Object ID of the scene objects.

## Scripting API
Here is a code snippet demonstrating how to setup a simple AOV request for the surface albedo on an HDRP camera.
```
// member variables:
RenderTexture m_AovRT;  // Stores the final AOV output
RTHandle m_TmpRT;       // The RTHandle used to render the AOV
```


```
// runtime code:

var hdAdditionalCameraData = camera.GetComponent<HDAdditionalCameraData>();
if (hdAdditionalCameraData != null )
{
    // first allocate a render texture to store the resulting
    if (m_AovRT == null)
        m_AovRT = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);

    // initialize a new AOV request
    var aovRequest = AOVRequest.NewDefault();

    AOVBuffers[] aovBuffers = null;
    CustomPassAOVBuffers[] customPassAovBuffers = null;
    
    // Request an AOV with the surface albedo
    aovRequest.SetFullscreenOutput(MaterialSharedProperty.Albedo);
    aovBuffers = new[] { AOVBuffers.Color };

    // Allocate the RTHandle that will store the intermediate results
    var buferAlloc = m_TmpRT ?? (m_TmpRT = RTHandles.Alloc(camera.pixelWidth, camera.pixelHeight));

    // Add the reuesto to a new AOVRequestBuilder
    var aovRequestBuilder = new AOVRequestBuilder();
    aovRequestBuilder.Add(aovRequest,
        bufferId => buferAlloc,
        null,
        aovBuffers,
        customPassAovBuffers,
        bufferId => buferAlloc,
        (cmd, textures, customPassTextures, properties) =>
        {
            // callback to blit the AOV from the intermediate RTHandle to the final render texture (m_AovRT). 
            if (textures.Count > 0)
            {
                cmd.Blit(textures[0], m_AovRT);
            }
            else if (customPassTextures.Count > 0)
            {
                cmd.Blit(customPassTextures[0], m_AovRT);
            }
        });

    // Now build the AOV request
    var aovRequestDataCollection = aovRequestBuilder.Build(); 

    // And finally set the request to the camera
    hdAdditionalCameraData.SetAOVRequests(aovRequestDataCollection);
}
```