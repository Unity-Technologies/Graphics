# Shadows in the Universal Render Pipeline

This section contains information on how shadows are implemented in URP.

For more general information on how shadows work in Unity, see the [Shadows](https://docs.unity3d.com/2020.2/Documentation/Manual/Shadows.html) page.

## Shadow maps in URP

In URP, the number of shadow maps per Light depends on the type of the Light:

* A Directional Light renders one shadow map per cascade. Set the cascade count for a Directional Light in the **Shadows** section in the [Universal Render Pipeline Asset](universalrp-asset.md#shadows).
* A Spot Light renders one shadow map.
* A Point Light renders six shadow maps (the number of faces in a cubemap).

URP determines resolutions for specific shadow maps depending on the number of the shadow maps that are required in the Scene, and the following settings in the [URP Asset](universalrp-asset.md#lighting):

* **Lighting** > **Main Light** > **Shadow Resolution**.

* **Lighting** > **Additional Lights** > **Shadow Atlas Resolution**.

## Shadow atlases

URP uses two shadow map atlases to render all real-time shadows in a frame:

* One shadow map atlas for all Spot Light and Point Light shadows.

* One shadow map atlas for Directional Light shadows.

Set the size of these atlases in your the following settings in the [URP Asset](universalrp-asset.md#lighting):

* **Lighting** > **Main Light** > **Shadow Resolution**.

* **Lighting** > **Additional Lights** > **Shadow Atlas Resolution**.

The atlas size determines the maximum resolution of shadows in your Scene.

For example, an atlas of size 1024 x 1024 can fit:

* Four shadow maps of 512 x 512 pixels.

* Sixteen shadow maps of 256 x 256 pixels.

### Matching shadow atlas resolution to Built-In Render Pipeline settings

In projects that use the **Built-In Render Pipeline**, you control shadow map resolution by selecting a shadow resolution level (Low, Medium, High, Very High) in the project's Quality Settings. For each shadow map, Unity determines which resolution to use, based on the algorithm described on the page [Shadow Mapping](https://docs.unity3d.com/Manual/shadow-mapping.html). You can use the [Frame Debugger](https://docs.unity3d.com/Manual/FrameDebugger.html) to see the resolution that Unity uses for a specific shadow map.

In **Universal Render Pipeline**, you specify the resolution of the shadow map atlases. This lets you control the amount of video memory your application allocates for shadows.

To ensure that the resolution that URP uses for a specific Point or Spot Light shadow is not less than a specific value, consider the number of shadow maps required in the scene, and select a big enough shadow atlas resolution.

Consider the following example: a Scene has four Spot Lights and one Point light, and each shadow map resolution must be at least 256x256.
* In this case, Unity needs to render ten shadow maps (one for each Spot Light, and six for the Point Light), each with resolution 256x256.
* A shadow atlas of size 512x512 is not enough, because it can contain only four maps of size 256x256.
* A shadow atlas of size 1024x1024 is big enough, it can contain up to 16 maps of size 256x256.

## Shadow bias

Shadow maps are textures projected from the point of view of the Light. URP uses a bias in the projection so that the shadow casting geometry does not self-shadow itself.

In URP, set the shadow bias values for each individual Light component using the following properties (**Light** > **Shadow Type** > **Realtime Shadows**):

- **Depth Bias**
- **Normal Bias**
- **Near Plane**

To see the Depth and Normal bias properties, set the Bias property to Custom.
