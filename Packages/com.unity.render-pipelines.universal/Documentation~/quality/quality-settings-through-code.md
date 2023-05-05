# Control URP Quality settings through code

Unity has several preset levels of [Quality settings](https://docs.unity3d.com/Manual/class-QualitySettings.html) and you might add more to your project. To accommodate different hardware specifications, you can switch between these levels and the associated URP Asset from C# scripts. The following examples show how to use API to change Quality setting levels and the active URP Asset, and how to change specific settings in the URP Asset at runtime.

**Note**: You should only change Quality settings and URP Asset settings at runtime at points where performance is not essential, such as during loading screens or on static menus. This is because these changes cause a temporary but significant performance impact.

## Change URP Asset at runtime

Each quality level uses a URP Asset to control many of the specific graphics settings. You can assigning different URP Assets to each quality level and switch between them at runtime.

### Configure Project Quality settings

To use Quality settings to switch between URP Assets, ensure that the quality levels of your project are configured to use different URP Assets. The URP 3D Sample scene has this configuration by default.

1. Create a URP Asset for each quality level. To do this, right-click in the Project window and select **Create** > **Rendering** > **URP Asset (with Universal Renderer)**.

    > **Note**: These instructions are also valid for URP Assets that use the 2D Renderer.

2. Configure and name the new URP Assets as necessary.
3. Open the Quality section in the Project Settings (**Edit** > **Project Settings** > **Quality**).
4. Assign each URP Asset to a quality level. To do this, select a quality level from the Levels list, then go to **Rendering** > **Render Pipeline Asset** and choose the URP Asset you created for this quality level. Do this for each quality level.

The quality levels of your project are now ready to be used to change between URP Assets at runtime.

### Change Quality Level

You can change the quality level Unity uses at runtime through the [QualitySettings API](https://docs.unity3d.com/ScriptReference/QualitySettings.html). With the quality levels setup as shown previously, this enables you to switch between URP Assets as well as Quality settings presets.

In the following simple example, the C# script uses the system's total Graphics Memory to determine the appropriate quality level without any input from the user when they open the built project.

1. Create a new C# script with the name QualityControls.
2. Open the QualityControls script and add the `SwitchQualityLevel` method to the `QualityControls` class.

    ```C#
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class QualityControls : MonoBehaviour
    {
        void Start()
        {
            
        }
        
        private void SwitchQualityLevel()
        {
            
        }
    }
    ```

3. Add a `switch` statement in the `SwitchQualityLevel` method to select the quality level with the `QualitySettings.SetQualityLevel()` method as shown below.

	> **Note**: Each Quality level has an index that matches the level's position in the list in the Quality section of the Project Settings window. The quality level at the top of the list has an index of 0. This index only counts quality levels which you specified as enabled for the target platform of any built version of your project.

    ```C#
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class QualityControls : MonoBehaviour
    {
        void Start()
        {
            
        }
        
        private void SwitchQualityLevel()
        {
            // Select Quality settings level (URP Asset) based on the size of the device's graphics memory
            switch (SystemInfo.graphicsMemorySize)
            {
                case <= 2048:
                    QualitySettings.SetQualityLevel(1);
                    break;
                case <= 4096:
                    QualitySettings.SetQualityLevel(2);
                    break;
                default:
                    QualitySettings.SetQualityLevel(0);
                    break;
            }
        }
    }
    ```

4. Add a call to the `SwitchQualityLevel` method in the `Start` method. This ensures that the quality level only changes when the scene first loads.

    ```C#
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class QualityControls : MonoBehaviour
    {
        void Start()
        {
            SwitchQualityLevel();
        }
        
        private void SwitchQualityLevel()
        {
            // Select Quality settings level (URP Asset) based on the size of the device's graphics memory
            switch (SystemInfo.graphicsMemorySize)
            {
                case <= 2048:
                    QualitySettings.SetQualityLevel(1);
                    break;
                case <= 4096:
                    QualitySettings.SetQualityLevel(2);
                    break;
                default:
                    QualitySettings.SetQualityLevel(0);
                    break;
            }
        }
    }
    ```

5. Open the first scene that your built project loads on startup.
6. Create an empty GameObject and call it QualityController. To do this, right-click in the Hierarchy Window and select **Create Empty**.
7. Open the QualityController object in the Inspector.
8. Add the QualityControls script to the QualityController as a component.

Now when this scene loads, Unity runs the `SwitchQualityLevel` method in the QualityControls script which detects the system's total graphics memory and sets the quality level. The quality level sets the URP Asset as the active Render Pipeline Asset.

You can create more complex systems and sequences of checks to determine which quality level to use, but the fundamental process remains the same. When the project starts, run a script which uses [`QualitySettings.SetQualityLevel`](https://docs.unity3d.com/ScriptReference/QualitySettings.SetQualityLevel.html) to select a quality level and through that select the URP Asset for the project to use at runtime.

## Change URP Asset settings

You can change some properties of the URP Asset at runtime with C# scripts. This can help fine tune performance on devices with hardware that doesn't perfectly match any of the quality levels in your project.

> **Note**: To change a property of the URP Asset with a C# script, the property must have a `set` method. For more information on these properties see [Accessible Properties](#accessible-properties).

The following example uses the QualityControls script and QualityController object from the [Change Quality Level through code](#change-quality-level-through-code) section, and extends the functionality to locate the active URP Asset and change some of its properties to fit the performance level of the hardware.

1. Open the QualityControls script.
2. At the top of the script add `using UnityEngine.Rendering` and `using UnityEngine.Rendering.Universal`.
3. Add a method with the name `ChangeAssetProperties` and the type `void` to the `QualityControls` class as shown below.

    ```C#
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    public class QualityController : MonoBehaviour
    {
        void Start()
        {
            // Select the appropriate Quality Level first
            SwitchQualityLevel();
        }

        private void SwitchQualityLevel()
        {
            // Code from previous example
        }

        private void ChangeAssetProperties()
        {
            // New code is added to this method
        }
    }
    ```

4. Retrieve the active Render Pipeline Asset with `GraphicsSettings.currentRenderPipeline` as shown below.

	> **Note**: You must use the `as` keyword to cast the Render Pipeline Asset as the `UniversalRenderPipelineAsset` type for the script to work correctly.

    ```C#
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    public class QualityController : MonoBehaviour
    {
        void Start()
        {
            // Select the appropriate Quality Level first
            SwitchQualityLevel();
        }

        private void SwitchQualityLevel()
        {
            // Code from previous example
        }

        private void ChangeAssetProperties()
        {
            // Locate the current URP Asset
            UniversalRenderPipelineAsset data = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

            // Do nothing if Unity can't locate the URP Asset
            if (!data) return;
        }
    }
    ```

5. Add a `switch` statement in the ChangeAssetProperties method to set the value of the URP Asset properties.

    ```C#
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    public class QualityController : MonoBehaviour
    {
        void Start()
        {
            // Select the appropriate Quality Level first
            SwitchQualityLevel();
        }

        private void SwitchQualityLevel()
        {
            // Code from previous example
        }

        private void ChangeAssetProperties()
        {
            // Locate the current URP Asset
            UniversalRenderPipelineAsset data = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

            // Do nothing if Unity can't locate the URP Asset
            if (!data) return;

            // Change URP Asset settings based on the size of the device's graphics memory
            switch (SystemInfo.graphicsMemorySize)
            {
                case <= 1024:
                    data.renderScale = 0.7f;
                    data.shadowDistance = 50.0f;
                    break;
                case <= 3072:
                    data.renderScale = 0.9f;
                    data.shadowDistance = 150.0f;
                    break;
                default:
                    data.renderScale = 0.7f;
                    data.shadowDistance = 25.0f;
                    break;
            }
        }
    }
    ```

6. Add a call to the `ChangeAssetProperties` method in the `Start` method. This ensures that the URP Asset only changes when the scene first loads.

    ```C#
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    public class QualityController : MonoBehaviour
    {
        void Start()
        {
            // Select the appropriate Quality Level first
            SwitchQualityLevel();

            // Fine tune performance with specific URP Asset properties
            ChangeAssetProperties();
        }

        private void SwitchQualityLevel()
        {
            // Code from previous example
        }

        private void ChangeAssetProperties()
        {
            // Locate the current URP Asset
            UniversalRenderPipelineAsset data = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

            // Do nothing if Unity can't locate the URP Asset
            if (!data) return;

            // Change URP Asset settings based on the size of the device's graphics memory
            switch (SystemInfo.graphicsMemorySize)
            {
                case <= 1024:
                    data.renderScale = 0.7f;
                    data.shadowDistance = 50.0f;
                    break;
                case <= 3072:
                    data.renderScale = 0.9f;
                    data.shadowDistance = 150.0f;
                    break;
                default:
                    data.renderScale = 0.7f;
                    data.shadowDistance = 25.0f;
                    break;
            }
        }
    }
    ```

Now when this scene loads, Unity detects the system's total graphics memory and sets the URP Asset properties accordingly.

You can use this method of changing particular URP Asset properties in conjunction with changing quality levels to fine tune the performance of your project for different systems without the need to create a quality level for every target hardware configuration.

### Accessible Properties

You can access and change any properties of the URP Asset which have a `set` method through a C# script at runtime.

The following properties of the URP Asset have a `set` method:

- cascadeBorder
- colorGradingLutSize
- colorGradingMode
- conservativeEnclosingSphere
- enableRenderGraph
- fsrOverrideSharpness
- fsrSharpness
- hdrColorBufferPrecision
- maxAdditionalLightsCount
- msaaSampleCount
- numIterationsEnclosingSphere
- renderScale
- shadowCascadeCount
- shadowDepthBias
- shadowDistance
- shadowNormalBias
- storeActionsOptimization
- supportsCameraDepthTexture
- supportsCameraOpaqueTexture
- supportsDynamicBatching
- supportsHDR
- upscalingFilter
- useAdaptivePerformance
- useSRPBatcher

For more information on these properties, see [Universal Render Pipeline Asset API](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@15.0/api/UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.html#properties).
