# Dynamic resolution

Dynamic resolution reduces the workload on the GPU, which helps maintain a stable target frame rate. The High Definition Render Pipeline (HDRP) uses dynamic resolution to lower the resolution of the render targets that the main rendering passes use. To do this, HDRP uses hardware dynamic resolution, if the platform supports it, otherwise it uses a software version. The main difference is that, for hardware dynamic resolution, the hardware treats the render targets up until the back buffer as being of the scaled size. This means that is is faster to clear the render targets.

To use dynamic resolution in your Project, you must enable dynamic resolution in your [HDRP Asset)(HDRP-Asset.html). In the Inspector for your HDRP Asset, navigate to **Rendering** **> Dynamic Resolution** and enable the **Enable** checkbox. For information on how to customize the rest of the HDRP Asset’s global dynamic resolution properties, see the dynamic resolution section of the [HDRP Asset documentation](HDRP-Asset.html#DynamicResolution).
Moreover, each camera that needs to perform dynamic resolution needs to have the checkbox **General > Allow Dynamic Resolution** set on the camera component.

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
