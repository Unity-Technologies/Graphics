# Dynamic resolution

Dynamic resolution reduces the workload on the GPU, which helps maintain a stable target frame rate. The High Definition Render Pipeline (HDRP) uses software dynamic resolution to lower the resolution of the render targets that the main rendering passes use.

To use dynamic resolution in your Project, you must enable dynamic resolution in your [HDRP Asset)(HDRP-Asset.html). In the Inspector for your HDRP Asset, navigate to **Rendering** **> Dynamic Resolution** and enable the **Enable** checkbox. For information on how to customize the rest of the HDRP Asset’s global dynamic resolution properties, see the dynamic resolution section of the [HDRP Asset documentation](HDRP-Asset.html#DynamicResolution).

When you enable dynamic resolution, HDRP allocates render targets to accommodate the maximum resolution possible. Then, HDRP rescales the viewport accordingly, so it can render at varying resolutions. At the end of each frame, HDRP upscales the result of the scaled rendering to match the back buffer resolution. To do this, HDRP uses the method defined in the **Upscale Filter**. 

![](Images/DynamicResolution1.png)

## Using dynamic resolution

Dynamic resolution is not automatic, so you need to manually call the `HDDynamicResolutionHandler.SetDynamicResScaler(PerformDynamicRes scaler, DynamicResScalePolicyType scalerType)` function. This function is in the `UnityEngine.Experimental.Rendering.HDPipeline` namespace.

The example below shows how to call this function. In a real production environment, you would call this function depending on the performance of your application.




```c#

using System.Collections;

using System.Collections.Generic;

using UnityEngine;

using UnityEngine.Experimental.Rendering.HDPipeline;

public class DynamicRes : MonoBehaviour

{

​    public float secondsToNextChange = 1.0f;

​    public float fractionDeltaStep = 0.1f;

​    private float currentScale = 1.0f;

​    private float directionOfChange = -1.0f;

​    private float elapsedTimeSinceChange = 0.0f;

​    // Simple example of a policy that scales the resolution every secondsToNextChange seconds. 

​    // Since this call uses DynamicResScalePolicyType.ReturnsMinMaxLerpFactor, HDRP uses currentScale in the following context:

​    // finalScreenPercentage = Mathf.Lerp(minScreenPercentage, maxScreenPercentage, currentScale);

​    public float SetDynamicResolutionScale()

​    {

​        elapsedTimeSinceChange += Time.deltaTime;

​        // Waits for secondsToNextChange seconds then requests a change of resolution.

​        if (elapsedTimeSinceChange >= secondsToNextChange)

​        {

​            currentScale += directionOfChange * fractionDeltaStep;

​            // When currenScale reaches the minimum or maximum resolution, this switches the direction of resolution change.

​            if (currentScale <= 0.0f || currentScale >= 1.0f)

​            {

​                directionOfChange *= -1.0f;

​            }

​            

​            elapsedTimeSinceChange = 0.0f;

​        }

​        return currentScale;

​    }

​    void Start()

​    {

​        // Binds the dynamic resolution policy defined above.

​        HDDynamicResolutionHandler.SetDynamicResScaler(SetDynamicResolutionScale, DynamicResScalePolicyType.ReturnsMinMaxLerpFactor);

​    }

}
​```
```