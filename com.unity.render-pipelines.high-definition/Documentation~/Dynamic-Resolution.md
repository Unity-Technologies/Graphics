# Dynamic resolution

Dynamic resolution reduces the workload on the GPU, which helps maintain a stable target frame rate. The High Definition Render Pipeline (HDRP) uses dynamic resolution to lower the resolution of the render targets that the main rendering passes use. To do this, HDRP uses hardware dynamic resolution, if the platform supports it, otherwise it uses a software version. The main difference is that, for hardware dynamic resolution, the hardware treats the render targets up until the back buffer as being of the scaled size. This means that is is faster to clear the render targets.

When you enable dynamic resolution, HDRP allocates render targets to accommodate the maximum resolution possible. Then, HDRP rescales the viewport accordingly, so it can render at varying resolutions. At the end of each frame, HDRP upscales the result of the scaled rendering to match the back buffer resolution. Regardless of which method HDRP uses to process dynamic resolution, either hardware or software, it still uses a software method to upscale the result. The method HDRP uses is defined in the **Upscale Filter**.

![](Images/DynamicResolution1.png)

## Using dynamic resolution

To use dynamic resolution in your Project, you must enable dynamic resolution in your [HDRP Asset](HDRP-Asset.md) and then enable it for each [Camera](HDRP-Camera.md) you want to use it with. To do this:

1. In the Inspector for your HDRP Asset, go to **Rendering** **> Dynamic Resolution** and enable the **Enable** checkbox. For information on how to customize the rest of the HDRP Asset’s global dynamic resolution properties, see the dynamic resolution section of the [HDRP Asset documentation](HDRP-Asset.md#DynamicResolution).
2. For every [Camera](HDRP-Camera.md) you want to perform dynamic resolution, go to the **General** section and enable **Allow Dynamic Resolution**.

Dynamic resolution is not automatic, so you need to manually call the `DynamicResolutionHandler.SetDynamicResScaler(PerformDynamicRes scaler, DynamicResScalePolicyType scalerType)` function.

The policy type can be one of the following:

- `DynamicResScalePolicyType.ReturnsPercentage`:  The DynamicResolutionHandler expects the `scaler` to return a screen percentage value. The value set will be clamped between the minimum and maximum percentage set in the HDRP Asset.
- `DynamicResScalePolicyType.ReturnsMinMaxLerpFactor`:  The DynamicResolutionHandler expects the `scaler` to return a factor `t` that is the [0 ... 1] range and that will be used as a lerp factor between the minimum and maximum screen percentages set in the HDRP asset.

The example below shows how to call this function. In a real production environment, you would call this function depending on the performance of your application.




```c#

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DynamicRes : MonoBehaviour
{
    public float secondsToNextChange = 1.0f;
    public float fractionDeltaStep = 0.1f;
    private float currentScale = 1.0f;
    private float directionOfChange = -1.0f;
    private float elapsedTimeSinceChange = 0.0f;

    // Simple example of a policy that scales the resolution every secondsToNextChange seconds.
    // Since this call uses DynamicResScalePolicyType.ReturnsMinMaxLerpFactor, HDRP uses currentScale in the following context:
    // finalScreenPercentage = Mathf.Lerp(minScreenPercentage, maxScreenPercentage, currentScale);
    public float SetDynamicResolutionScale()
    {
        elapsedTimeSinceChange += Time.deltaTime;

        // Waits for secondsToNextChange seconds then requests a change of resolution.
        if (elapsedTimeSinceChange >= secondsToNextChange)
        {
            currentScale += directionOfChange * fractionDeltaStep;

            // When currenScale reaches the minimum or maximum resolution, this switches the direction of resolution change.
            if (currentScale <= 0.0f || currentScale >= 1.0f)
            {
                directionOfChange *= -1.0f;
            }

            elapsedTimeSinceChange = 0.0f;
        }
        return currentScale;
    }

    void Start()

    {
        // Binds the dynamic resolution policy defined above.
        DynamicResolutionHandler.SetDynamicResScaler(SetDynamicResolutionScale, DynamicResScalePolicyType.ReturnsMinMaxLerpFactor);
    }
}
```

## Choosing an upscale filter

The upscale filter used can be set on the [HDRP Asset ](HDRP-Asset.md#DynamicResolution). It is possible to override what is set in the asset via script on a per-camera basis with: `DynamicResolutionHandler.SetUpscaleFilter(Camera camera, DynamicResUpscaleFilter filter)`, once the filter is overridden this way, the value in the HDRP asset is ignored for the given camera.

HDRP offers many filters:

| **Upscale Filter Name**         | Description                                                  |
| ------------------------------- | ------------------------------------------------------------ |
| Bilinear                        | A fairly low quality and simple filter, but by far the cheapest option in terms of performance. It can result in blurry images post-upscaling and can let through more aliasing than other filters, its usage is suggested only for situations in which any overhead of the other filters cannot be afforded. |
| Catmull-Rom                     | A bicubic upscale filter performed with 4 taps. It produces better quality than Bilinear, but can still result is blurry images post-upscaling. It is the second cheapest filter in terms of performance impact after the Bilinear one. |
| Lanczos                         | A Lanczos filter that produces a significantly sharper final image than with Bilinear and Catmull-Rom, however at a measurable cost increase. Note that due to the nature of the filter it can cause artifacts when used together with extremely low screen percentages as the sharpening that the filter causes can lead to ringing (i.e. unnaturally dark edges). |
| Contrast Adaptive Sharpen       | An ultra-sharp upsample. It produces very sharp post-upscale image via an aggressive sharpening. This option is not meant to be used when screen percentage is less than 50%.  This uses **FidelityFX (CAS) AMD™**. For information about FidelityFX and Contrast Adaptive Sharpening, see [AMD FidelityFX](https://www.amd.com/en/technologies/radeon-software-fidelityfx). |
| FidelityFX Super Resolution 1.0 | A spatial super resolution technology that leverages cutting-edge algorithms to produce very good upscaling quality and fast performance. For more information, see [AMD FidelityFX](https://www.amd.com/en/technologies/radeon-software-fidelityfx). |
| TAA Upscale                     | A temporal upscaling, uses temporal integration to perform temporal super resolution. This upscale method produce very clear and sharp images post-upscale, moreover it is tunable the same way normal TAA is.  The algorithm is performance effective as it performed alongside the normal anti-aliasing. <br />The algorithm takes place before post processing alongside TAA and as such forces TAA as the anti-aliasing algorithm of choice. This is the main drawback of the method as it is not compatible with other anti-aliasing algorithms and suffers from the usual TAA drawbacks. <br />More information on [Notes on TAA Upscale](Dynamic-Resolution.md#Notes on TAA Upscale) section. |

HDRP also supports NVIDIA Deep Learning Super Sampling (DLSS)  for GPU that support it, more informations about DLSS on the [dedicated documentation page](deep-learning-super-sampling-in-hdrp.md).



The final choice of the upscaling filter is ultimately dependent on the project,



## Notes on TAA Upscale

Temporal Anti-Aliasing Upscaling replaces Temporal Anti-Aliasing in the pipeline. This has a few important implications:

- Temporal Anti-Aliasing is forced as the anti-aliasing method of choice, no other post-process anti-aliasing option is supported.
- The Post-process pipeline runs at final resolution and not the downscaled resolution at which the pipeline runs up until TAA Upscale. This lead to more precise post-processing, but also comes at an increased cost of the post-processing pipeline

Moreover, as Temporal Anti-Aliasing Upscaling is based TAA High quality preset, currently no other preset is selectable.

Any option that is used to tune TAA is going to tune TAA Upscaling, however it is important to  remember that the source of current frame is lower resolution than the final image/history buffer; this is especially relevant when setting up speed rejection. Speed rejection heavily reduces the influence of the history in favour of current frame, this mean that at lower screen percentages speed rejection can lead to a fairly low resolution looking image.
