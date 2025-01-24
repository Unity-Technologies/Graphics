# Restrict a render pass to a scene area in URP

To restrict a render pass to a specific area of a scene, add a [volume](../Volumes.md) to the scene, then add code to your render pass and your shader to check if the camera is inside the volume.

Follow these steps:

1. Update your shader code to enable or disable your custom rendering effect based on a Boolean value.

    For example, add the following code to your shader:

    ```hlsl
    Pass
    {
        ...

        // Add a variable to enable or disable your custom rendering effect
        float _Enabled;

        ...

        float4 Frag(Varyings input) : SV_Target0
        {
            ...

            // Return the color with the effect if the variable is 1, or the original color if the variable is 0
            if (_Enabled == 1){
                return colorWithEffect;
            } else {
                return originalColor;
            }
        }
    }
    ```

2. Create a script that implements the `VolumeComponent` class. This creates a volume override component that you can add to a volume.

    ```c#
    using UnityEngine;
    using UnityEngine.Rendering;

    public class MyVolumeOverride : VolumeComponent
    {
    }
    ```

3. In the **Hierarchy** window, select the **Add** (**+**) button, then select **GameObject** > **Volume** > **Box Volume**.

4. In the **Inspector** window for the new box volume, under **Volume**, select **New** to create a new volume profile.

5. Select **Add override**, then select your volume override component, for example **My Volume Override**.

6. Add a property to the volume override script. Unity adds the property in the **Inspector** window of the volume override.

    For example:

    ```c#
    public class MyVolumeOverride : VolumeComponent
    {
        // Add an 'Effect Enabled' checkbox to the Volume Override, with a default value of true.
        public BoolParameter effectEnabled = new BoolParameter(true);
    }
    ```

5. In your custom pass, use the `GetComponent` API to get the volume override component and check the value of the property.

    For example:

    ```c#
    class myCustomPass : ScriptableRenderPass
    {

        ...

        public void Setup(Material material)
        {
            // Get the volume override component
            MyVolumeOverride myOverride = VolumeManager.instance.stack.GetComponent<MyVolumeOverride>();

            // Get the value of the 'Effect Enabled' property
            bool effectStatus = myOverride.effectEnabled.overrideState ? myOverride.effectEnabled.value : false;
        }
    }
    ```

6. Pass the value of the property to the variable you added to the shader code.

    For example:

    ```c#
    class myCustomPass : ScriptableRenderPass
    {

        ...

        public void Setup(Material material)
        {
            MyVolumeOverride myOverride = VolumeManager.instance.stack.GetComponent<MyVolumeOverride>();
            bool effectStatus = myOverride.effectEnabled.overrideState ? myOverride.effectEnabled.value : false;

            // Pass the value to the shader
            material.SetFloat("_Enabled", effectStatus ? 1 : 0);
        }
    }
    ```

Your custom rendering effect is now enabled when the camera is inside the volume, and disabled when the camera is outside the volume.

## Additional resources

- [Write a Scriptable Render Pass](../renderer-features/write-a-scriptable-render-pass.md)
- [Volumes in URP](../volumes.md)
- [Writing custom shaders in URP](../writing-custom-shaders-urp.md)
