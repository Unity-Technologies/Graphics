# Dynamic resolution

Dynamic resolution reduces the workload on the GPU, which helps maintain a stable target frame rate. The High Definition Render Pipeline (HDRP) uses dynamic resolution to lower the resolution of the render targets that the main rendering passes use. To do this, HDRP uses hardware dynamic resolution, if the platform supports it, otherwise it uses a software version. The main difference is that, for hardware dynamic resolution, the hardware treats the render targets up until the back buffer as being of the scaled size. This means that is is faster to clear the render targets.

Hardware Dynamic resolution is supported on:

- On all Console platforms supported by HDRP.
- On PC only with DX12 and Metal.

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

It is best practice to keep the screen percentage above 50%. However, HDRP supports values from 5% to 100%, anything below or above this range gets clamped accordingly.

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

You can set the upscale filter in the [HDRP Asset](HDRP-Asset.md#DynamicResolution). To do this, navigate to **Rendering > Dynamic Resolution**, select **Enable** and open the **Upscale Filter** drop down.

HDRP supports NVIDIA Deep Learning Super Sampling (DLSS)  for GPUs that support it. For more information, see [DLSS in HDRP](deep-learning-super-sampling-in-hdrp.md).

Each upscale filter gives a different effect. If your project uses Temporal Anti-aliasing, select the **Temporal Anti-Aliasing (TAA) Upscale** method first and then experiment with the other options to see what best fit your project. If your project is not suited for TAA, then select the **FidelityFX Super Resolution 1.0** method first.

The following images give an example of the difference between the Temporal Anti-Aliasing Upscale (A) and Catmull-Rom (B) methods.

![A:TAA Upscale. B: Catmull-Rom.](Images/DynamicRes_SidebySide_AB.png)

HDRP provides the following upscale filter methods:
| **Upscale Filter Name**              | Description                                                  |
| ------------------------------------ | ------------------------------------------------------------ |
| Catmull-Rom                          | This upscale filter uses four bilinear samples. This method uses the least resources, but it can cause blurry images after HDRP performs the upscaling step.<br/><br/> This upscale filter has no dependencies and runs at the end of the post-processing pipeline. |
| Contrast Adaptive Sharpen (CAS)      | This method produces a sharp image with an aggressive sharpening step. Do not use this option when the dynamic resolution screen percentage is less than 50%.  <br/>This upscale filter uses **FidelityFX (CAS) AMD™**. For information about FidelityFX and Contrast Adaptive Sharpening, see [AMD FidelityFX](https://www.amd.com/en/technologies/radeon-software-fidelityfx).<br/><br/> This upscale filter has no dependencies and runs at the end of the post-processing pipeline. |
| FidelityFX Super Resolution 1.0      | This upscale filter uses a spatial super-resolution method that balances quality and performance. For more information, see [AMD FidelityFX](https://www.amd.com/en/technologies/radeon-software-fidelityfx).<br/><br/> This upscale filter has no dependencies and runs at the end of the post-processing pipeline.<br />The filter also runs when at 100% resolution as it can have beneficial sharpening effects. |
| Temporal Anti-Aliasing (TAA) Upscale | This upscale filter uses temporal integration to produce a sharp image. This method uses the fewest resources on the GPU.<br />HDRP executes this upscale filter before post processing and at the same time as the TAA step. This means you can only use the TAA anti-aliasing method. This filter is not compatible with other anti-aliasing methods. <br /><br/>The filter also runs when at 100% resolution when Dynamic Resolution is enabled as it is responsible for anti-aliasing. <br />More information on [Notes on TAA Upscale](Dynamic-Resolution.md#Notes) section. |

You can also override the upscale  options in the HDRP asset for each Camera in your scene using code.  To do this, call `DynamicResolutionHandler.SetUpscaleFilter(Camera camera, DynamicResUpscaleFilter filter)`, to make HDRP ignore the value in the HDRP Asset for a given camera.

## Notes on Temporal Anti-Aliasing (TAA) Upscaling

When you enable Temporal Anti-Aliasing (TAA) Upscaling, it replaces Temporal Anti-Aliasing in the pipeline. This means:

- Temporal Anti-Aliasing is the only the anti-aliasing method and no other post-process anti-aliasing option will work.
- The post-process pipeline runs at final resolution. Before HDRP applies TAA Upscaling, the pipeline uses a downscaled resolution. This gives a more precise post-processing result, but also uses more GPU resources.

Temporal Anti-Aliasing Upscaling sets the [Camera's](HDRP-Camera.md) **TAA Quality Preset** to **High Quality**. When you select the Temporal Anti-Aliasing Upscaling method, you cannot use change this preset.

Any option that can control TAA also controls TAA Upscaling. however it is important to remember that the source of current frame is lower resolution than the final image/history buffer. For this reason:

-  It is important to be cautious when setting up speed rejection thresholds. Speed rejection heavily reduces the influence of the history in favor of current frame, this mean that at lower screen percentages speed rejection can lead to a fairly low resolution looking image.
- Given that the source data is low resolution, it is worth to explore the possibility of being more generous with the sharpening setting than what would be normally used at full resolution.
