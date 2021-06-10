# Custom Pass Volume workflow

The workflow for Custom Pass Volumes is similar to the HDRP [Volumes](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@10.0/manual/Volumes.html) framework. However, there are the following differences between the two:

- You can’t blend Custom Pass Volumes, but you can fade between them using **Fade Radius** in the **Custom Pass Volume component**.
- Unity stores Custom Pass data on a GameObject rather than an asset in your project.
- If you have more than one Custom Pass Volume in your scene with the same injection point, Unity executes the Volume in order of [Priority](Custom-Pass-Creating.md#Custom-Pass-Volume).

To create a Custom Pass Volume that affects every camera in the scene, set the Custom Pass Volume component’s **Mode** to **Global**. To create a Custom Pass Volume that only affects cameras within the boundaries of the Volume, set the Custom Pass Volume component’s **Mode** to **Local**.

You can use the **Priority** field to determine the execution order of multiple Custom Pass Volumes in your scene. If two Custom Pass Volumes have the same priority, Unity executes them in one of the following ways:

- When both Custom Pass Volumes are **Global**, the execution order is undefined.
- When both Custom Pass Volumes are **Local**, Unity executes the Volume with the smallest collider first (ignoring the fade radius). If both Volumes have the same collider size, the order is undefined.
- When one Custom Pass Volume is **Local** and the other is **Global**, Unity executes the **Local** Volume first, then the **Global** Volume.

If the **Mode** of your Custom Pass Volume is set to **Local**, you can use the **Fade Radius** property in the [Custom Pass Volume Component](Custom-Pass-Creating.md#Custom-Pass-Volume) to smooth the transition between your normal rendering and the Custom Pass. The fade radius value is measured in meters and does not scale when you transform the object it is assigned to.

The image below visualises the outer bounds of the fade radius as a wireframe box. The Custom Pass Volume in this image has a [Box Collider](https://docs.unity3d.com/Manual/class-BoxCollider.html) component which visualises the Volume.

![A Custom Pass Volume visualised using a Box Collider, with its fade radius visualised as a wireframe box.](images/CustomPassVolumeBox_Collider.png)

## Using C# to change the fade radius

To change the fade radius in code, you can use the built in `_FadeValue` variable in the shader, and `CustomPass.fadeValue` in your C# script. These variables operate on a 0 - 1 scale that represents how far the Camera is from the collider bounding Volume. To learn more, see [Scripting your own Custom Pass in C#](Custom-Pass-Scripting.md).
