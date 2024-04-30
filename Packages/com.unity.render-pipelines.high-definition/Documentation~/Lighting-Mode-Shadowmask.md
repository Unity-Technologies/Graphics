# Use shadowmasks

Shadowmasks use the Shadowmask mixed lighting mode combine baked and real-time shadows at runtime and render shadows in the far distance. 

HDRP supports the following [Mixed Lighting Modes](https://docs.unity3d.com/Manual/LightMode-Mixed.html):

- [Baked indirect](https://docs.unity3d.com/Manual/LightMode-Mixed-BakedIndirect.html)
- [Shadowmask](https://docs.unity3d.com/Manual/LightMode-Mixed-ShadowmaskMode.html).

# Enable shadowmasks

To use shadowmasks in HDRP, you must enable shadowmask support in your Unity Project’s HDRP Asset and then make your Cameras use shadowmasks in their [Frame Settings](Frame-Settings.md) :

1. Under **Render Pipeline Supported Features**, enable the **Shadowmask** checkbox. This enables support for shadowmasks in your Unity Project.
2. Next, you must make your Cameras use shadowmasks. In your HDRP Asset, navigate to **Default Frame Settings > Lighting** and enable the **Shadowmask** checkbox to make Cameras use shadowmasks by default.

# Set up shadowmasks

To use shadowmasks in HDRP, you must set up your Project to support them. To do this:

1. Enable **Shadowmask** for every Quality Level in your Unity Project:
   1. Go to **Edit** > **Project Setting** > **Quality** > **HDRP**.
   2. Select a Quality Level (for example, **HDRPMediumQuality**).
   3. Go to **Lighting** > **Shadows** and enable **Shadowmask**.
   4. Repeat these steps for every Quality Level.
2. Enable the **Shadowmask** property in your Unity Project’s [HDRP Asset](HDRP-Asset.md):
   1. In the Project window, select an HDRP Asset to view in the Inspector.
   2. Go to **Lighting** > **Shadow**.
   3. Enable **Shadowmask**.
3. Set up your Scene to use **Shadowmask** and **Baked Global Illumination**:
   1. Open the Lighting window (menu: **Window** > **Rendering** > **Lighting**).
   2. In the **Mixed Lighting** section, enable **Baked Global Illumination** and set the **Lighting Mode** to **Shadowmask**.
4. Make your Cameras use shadowmasks when they render the Scene. To set this as the default behaviour for Cameras:
   1. Go to **Edit** > **Project Settings** > **Graphics** > **Pipeline Specific Settings** > **HDRP**.
   2. Go to **Frame Settings (Default Values)** > **Camera** > **Lighting** and enable **Shadowmask**.
5. Optionally, you can make your [Reflection Probes](Reflection-Probes-Intro.md) use shadowmask for baked or real-time reflections. To do this:
   1. Go to **Frame Settings (Default Values)** > **Realtime Reflection** or **Baked or Custom Reflection** > **Lighting** and enable **Shadowmask**.

On a [Light](Light-Component.md), when you select the **Mixed** mode. The lightmapper precomputes Shadowmasks for static GameObject that the Light affects.


## Change the shadowmask lighting mode

The High Definition Render Pipelines (HDRP) supports the Shadowmask Lighting Mode which makes the lightmapper precompute shadows for static GameObjects, but still process real-time lighting for non-static GameObjects. HDRP also supports the [Baked Indirect](https://docs.unity3d.com/Manual/LightMode-Mixed-BakedIndirect.html) mixed Lighting Mode. For more information on mixed lighting and shadowmasks, see [Mixed lighting modes](https://docs.unity3d.com/Manual/LightMode-Mixed.html) and [Shadowmask](https://docs.unity3d.com/Manual/LightMode-Mixed-ShadowmaskMode.html).

## Shadowmask mode

To allow for flexible lighting setups, HDRP lets you choose the behaviour of the shadowmasks for each individual Light. To change the behavior of the shadowmask, use the Light’s **Shadowmask Mode** property. To do this:

1. Select a Light in your Scene to view it in the Inspector window.
2. Go to **General** > **Mode** > **Mixed**.
2. Go to **Shadows** > **Shadow Map** > **Shadowmask Mode** and select one of the options.

For information on the behavior of each Shadowmask Mode, see the following table:

| Shadowmask Mode     | Description                                                  |
| ------------------- | ------------------------------------------------------------ |
| **Distance Shadowmask** | Makes the Light cast real-time shadows for all GameObjects when the distance between the Camera and the Light is less than the Fade Distance. If you can not see this property, enable [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html) for the Shadows section. When the distance between the Light and the Camera is greater than the Fade Distance, HDRP stops calculating real-time shadows for the Light. Instead, it uses shadowmasks for static GameObjects, and non-static GameObjects don't cast shadows. Directional Lights don't use Fade Distance, instead they use the current [Max Shadow Distance](Override-Shadows.md). |
| **Shadowmask**          | Makes the Light cast real-time shadows for non-static GameObjects only. It then combines these shadows with shadowmasks for static GameObjects when the distance between the Camera and the Light is less than the Fade Distance. When the distance between the Light and the Camera is greater than the Fade Distance, HDRP stops calculating real-time shadows for the Light. It uses shadowmasks for static GameObjects and non-static GameObjects don't cast shadows. |

<a name="DirectionalLightEquivalentProperty"></a>

Directional Lights don't use **Fade Distance**. Instead they use the **Max Distance** property located in the [HD Shadow Settings](Override-Shadows.md) of your Scene’s Volumes.

## Performance

**Distance Shadowmask** is more GPU intensive, but looks more realistic because real-time lighting that's closer to the Light is more accurate than shadowmask Textures with a low resolution meant to represent areas further away.

**Shadowmask** is more memory intensive because the Camera uses shadowmask Textures for static GameObjects close to the Camera, which requires a larger resolution shadowmask Texture.
