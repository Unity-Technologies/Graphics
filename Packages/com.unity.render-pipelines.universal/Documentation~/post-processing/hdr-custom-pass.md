# Scriptable Render Passes with HDR Output

High Dynamic Range (HDR) Output changes the inputs to your custom pass when it applies tone mapping and color space conversions. These changes can cause your custom pass to produce incorrect results. This means that when you use HDR Output and a custom pass that happens in or after the AfterRenderingPostProcessing injection point, you need to account for the changes HDR Output makes. This also applies when you want to add overlays during or after post-processing, such as UI or output from other cameras, because you need to work with the color gamut that results from HDR Output. To make a custom pass work with the changes HDR Output makes, you must manually perform [tone mapping and convert color space in a script](#tone-map-convert-color-space).

When you execute a custom pass in or before the BeforeRenderingPostProcessing injection point, you don't need to do anything else to make it work with HDR Output. This is because Unity executes your custom pass before it renders the HDR Output.

**Note**: You can avoid this problem when you use a camera stack to render camera output before Unity performs tone mapping. Unity then applies HDR Output processing to the last camera in the stack. To learn how to set up a camera stack, refer to [Camera stacking](../camera-stacking.md).

## <a name="tone-map-convert-color-space"></a>Tone map and convert color space in a script

To make a custom pass work with the changes HDR Output makes to color space and dynamic range, use the `SetupHDROutput`  function to apply tone mapping and color space conversion to the material the custom pass alters:

1. Open the C# script which contains the Scriptable Render Pass you wish to use with HDR Output.
2. Add a method with the name `SetupHDROutput` to the Render Pass class.

    The following script gives an example of how to use the `SetupHDROutput` function:

    ```c#
    class CustomFullScreenRenderPass : ScriptableRenderPass
    {
        // Leave your existing Render Pass code here

        static void SetupHDROutput(ref CameraData cameraData, Material material)
        {
            // This is where most HDR related code is added
        }
    }
    ```

3. Add an `if` statement to check whether HDR Output is active and if the camera has post-processing enabled. If either condition is not met, disable the HDR Output shader keywords to reduce resource usage.

    ```c#
    static void SetupHDROutput(ref CameraData cameraData, Material material)
    {
        // If post processing is enabled, color grading has already applied tone mapping
        // As a result the input here will be in the display colorspace (Rec2020, P3, etc) and in nits
        if (cameraData.isHDROutputActive && cameraData.postProcessEnabled)
        {

        }
        else
        {
            // If HDR output is disabled, disable HDR output-related keywords
            // If post processing is disabled, the final pass will do the color conversion so there is
            // no need to account for HDR Output
            material.DisableKeyword(HDROutputUtils.ShaderKeywords.HDR_INPUT);
        }
    }
    ```

4. Create variables to retrieve and store the luminance information from the display as shown below.

    ```c#
    if (cameraData.isHDROutputActive && cameraData.postProcessEnabled)
    {
        // Get luminance information from the display, these define the dynamic range of the display.
        float minNits = cameraData.hdrDisplayInformation.minToneMapLuminance;
        float maxNits = cameraData.hdrDisplayInformation.maxToneMapLuminance;
        float paperWhite = cameraData.hdrDisplayInformation.paperWhiteNits;
    }
    else
    {
        // If HDR output is disabled, disable HDR output-related keywords
        // If post processing is disabled, the final pass will do the color conversion so there is
        // no need to account for HDR Output
        material.DisableKeyword(HDROutputUtils.ShaderKeywords.HDR_INPUT);
    }
    ```

5. Retrieve the tonemapping component from the Volume Manager.

    ```c#
    if (cameraData.isHDROutputActive && cameraData.postProcessEnabled)
    {
        var tonemapping = VolumeManager.instance.stack.GetComponent<Tonemapping>();

        // Get luminance information from the display, these define the dynamic range of the display.
        float minNits = cameraData.hdrDisplayInformation.minToneMapLuminance;
        float maxNits = cameraData.hdrDisplayInformation.maxToneMapLuminance;
        float paperWhite = cameraData.hdrDisplayInformation.paperWhiteNits;
    }
    ```

6. Add another `if` statement to check whether a tonemapping component is present. If a tonemapping component is found, this can override the luminance data from the display.

    ```c#
    if (cameraData.isHDROutputActive && cameraData.postProcessEnabled)
    {
        var tonemapping = VolumeManager.instance.stack.GetComponent<Tonemapping>();

        // Get luminance information from the display, these define the dynamic range of the display.
        float minNits = cameraData.hdrDisplayInformation.minToneMapLuminance;
        float maxNits = cameraData.hdrDisplayInformation.maxToneMapLuminance;
        float paperWhite = cameraData.hdrDisplayInformation.paperWhiteNits;

        if (tonemapping != null)
        {
            // Tone mapping post process can override the luminance retrieved from the display
            if (!tonemapping.detectPaperWhite.value)
            {
                paperWhite = tonemapping.paperWhite.value;
            }
            if (!tonemapping.detectBrightnessLimits.value)
            {
                minNits = tonemapping.minNits.value;
                maxNits = tonemapping.maxNits.value;
            }
        }
    }
    ```

7. Set the luminance properties of the material with the luminance data from the display and tonemapping.

    ```c#
    if (cameraData.isHDROutputActive && cameraData.postProcessEnabled)
    {
        var tonemapping = VolumeManager.instance.stack.GetComponent<Tonemapping>();

        // Get luminance information from the display, these define the dynamic range of the display.
        float minNits = cameraData.hdrDisplayInformation.minToneMapLuminance;
        float maxNits = cameraData.hdrDisplayInformation.maxToneMapLuminance;
        float paperWhite = cameraData.hdrDisplayInformation.paperWhiteNits;

        if (tonemapping != null)
        {
            // Tone mapping post process can override the luminance retrieved from the display
            if (!tonemapping.detectPaperWhite.value)
            {
                paperWhite = tonemapping.paperWhite.value;
            }
            if (!tonemapping.detectBrightnessLimits.value)
            {
                minNits = tonemapping.minNits.value;
                maxNits = tonemapping.maxNits.value;
            }
        }

        // Pass luminance data to the material, use these to interpret the range of values the
        // input will be in.
        material.SetFloat("_MinNits", minNits);
        material.SetFloat("_MaxNits", maxNits);
        material.SetFloat("_PaperWhite", paperWhite);
    }
    ```

8. Retrieve the color gamut of the current color space and pass it to the material.

    ```c#
    // Pass luminance data to the material, use these to interpret the range of values the
    // input will be in.
    material.SetFloat("_MinNits", minNits);
    material.SetFloat("_MaxNits", maxNits);
    material.SetFloat("_PaperWhite", paperWhite);

    // Pass the color gamut data to the material (colorspace and transfer function).
    HDROutputUtils.GetColorSpaceForGamut(cameraData.hdrDisplayColorGamut, out int colorspaceValue);
    material.SetInteger("_HDRColorspace", colorspaceValue);
    ```

9. Enable the HDR Output shader keywords.

    ```c#
    // Pass the color gamut data to the material (colorspace and transfer function).
    HDROutputUtils.GetColorSpaceForGamut(cameraData.hdrDisplayColorGamut, out int colorspaceValue);
    material.SetInteger("_HDRColorspace", colorspaceValue);

    // Enable HDR shader keywords
    material.EnableKeyword(HDROutputUtils.ShaderKeywords.HDR_INPUT);
    ```

10. Call the SetupHDROutput method in your Execute() function to ensure that HDR Output is accounted for whenever this Scriptable Render Pass is in use.

## Complete Script Example

The following is the complete code from the example:

```c#
class CustomFullScreenRenderPass : ScriptableRenderPass
{
    // Leave your existing Render Pass code here

    static void SetupHDROutput(ref CameraData cameraData, Material material)
    {
        // If post processing is enabled, color grading has already applied tone mapping
        // As a result the input here will be in the display colorspace (Rec2020, P3, etc) and in nits
        if (cameraData.isHDROutputActive && cameraData.postProcessEnabled)
        {
            var tonemapping = VolumeManager.instance.stack.GetComponent<Tonemapping>();

            // Get luminance information from the display, these define the dynamic range of the display.
            float minNits = cameraData.hdrDisplayInformation.minToneMapLuminance;
            float maxNits = cameraData.hdrDisplayInformation.maxToneMapLuminance;
            float paperWhite = cameraData.hdrDisplayInformation.paperWhiteNits;

            if (tonemapping != null)
            {
                // Tone mapping post process can override the luminance retrieved from the display
                if (!tonemapping.detectPaperWhite.value)
                {
                    paperWhite = tonemapping.paperWhite.value;
                }
                if (!tonemapping.detectBrightnessLimits.value)
                {
                    minNits = tonemapping.minNits.value;
                    maxNits = tonemapping.maxNits.value;
                }
            }

            // Pass luminance data to the material, use these to interpret the range of values the
            // input will be in.
            material.SetFloat("_MinNits", minNits);
            material.SetFloat("_MaxNits", maxNits);
            material.SetFloat("_PaperWhite", paperWhite);

            // Pass the color gamut data to the material (colorspace and transfer function).
            HDROutputUtils.GetColorSpaceForGamut(cameraData.hdrDisplayColorGamut, out int colorspaceValue);
            material.SetInteger("_HDRColorspace", colorspaceValue);

            // Enable HDR shader keywords
            material.EnableKeyword(HDROutputUtils.ShaderKeywords.HDR_INPUT);
        }
        else
        {
            // If HDR output is disabled, disable HDR output-related keywords
            // If post processing is disabled, the final pass will do the color conversion so there is
            // no need to account for HDR Output
            material.DisableKeyword(HDROutputUtils.ShaderKeywords.HDR_INPUT);
        }
    }
}
```
