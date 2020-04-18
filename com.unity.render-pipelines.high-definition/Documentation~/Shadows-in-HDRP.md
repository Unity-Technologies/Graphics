# Shadows in the High Definition Render Pipeline

The High Definition Render Pipeline’s [Lights](Light-Component.html) can cast shadows from one GameObject onto another. They emphasize the position and scale of GameObjects, which adds a degree of depth and realism to a Scene that could otherwise look flat.

![](Images/HDRPFeatures-Shadows.png)

## Shadow map resolution

The resolution of a Light’s shadow map determines the size of its shadow maps. The larger the shadow map, the more precise the shadows can be, and the better the High Definition Render Pipeline (HDRP) can capture small details in the shadow casting geometry. Rendering shadow maps at higher resolutions make them look sharper.

Set the resolution of a specific Light’s shadow map in the **Shadows** section of the Light component.

The number of shadow maps HDRP renders per Light depends on the **Type** of the Light:

- A Spot Light renders one shadow map.
- A Point Light renders six shadow maps (the number of faces in a cubemap).
- A Directional Light renders one shadow map per cascade. Set the cascade count of Directional Lights from the [HD Shadow Settings](Override-Shadows.html) of your Scene’s [Volumes](Volumes.html). The default value is four cascades.

## Shadow atlases

HDRP renders all real-time shadows for a frame using a shadow map atlas for all [punctual light](Glossary.html#PunctualLight) shadows, and another shadow map atlas for Directional Light shadows.

Set the size of these atlases in your Unity Project’s [HDRP Asset](HDRP-Asset.html). The atlas size determines the maximum resolution of shadows in your Scene.

For example, the default size of an atlas is 4096 x 4096, which can fit:

- Sixteen shadow maps of 1024 x 1024 pixels.
- Two shadow maps of 2048 x 2048 plus four shadow maps of 1024 x 1024 plus eight shadow maps of 512 x 512 plus 32 shadow maps of 256 x 256.

## Controlling the maximum number of shadows on screen

In addition to the atlases, you can also set the maximum number of shadow maps HDRP can render in a single frame. To do this, open your Unity Project’s HDRP Asset, navigate to the **Shadows** section, and enter a **Max Shadows on Screen** value. If the number of shadow maps on screen is higher than this limit, HDRP does not render them.

## Shadow Bias

Shadow maps are essentially textures projected from the point of view of the Light. HDRP uses a bias in the projection so that the shadow casting geometry does not self-shadow itself.

In HDRP, each individual Light component controls its own shadow biasing using the following parameters:

- **Near Plane**
- **Slope-Scale Depth Bias**
- **Normal Bias**

Find these settings under the **Shadows** section. If some of the property fields are missing, click the [more options](More-Options.html) cog to expose them. For details on how each property controls the shadow biasing, see the [Light documentation](Light-Component.html).

![](Images/Shadows1.png)

Using high shadow bias values may result in light "leaking" through Meshes. This is where there is a visible gap between the shadow and its caster and leads to shadow shapes that do not accurately represent their casters.

<a name="ShadowFiltering"></a>

## Shadow filtering

After HDRP captures a shadow map, it processes filtering on the map in order to decrease the aliasing effect that occurs on low resolution shadow maps. Different filters affect the perceived sharpness of shadows.

To change which filter HDRP uses, the method depends on which filter quality you want to use and whether your HDRP Project uses [forward or deferred rendering](Forward-And-Deferred-Rendering.md).

* **Forward rendering**: Change the **Filtering Quality** property in your Unity Project’s [HDRP Asset](HDRP-Asset.html). This method works for every filter quality. There are currently three filter quality presets for directional and punctual lights. For information on the available filter qualities, see the [Filtering Qualities table](HDRP-Asset.html#FilteringQualities).
* **Deferred rendering**: For **Low** and **Medium** filter qualities, use the same method as forward rendering. If you want to use **High** quality (PCSS) filtering, you need to enable it in the [HDRP Config package](HDRP-Config-Package.html). For information on how to do this, see the [Example section](HDRP-Config-Package.html#Example) of the Config package documentation.

## Shadowmasks

HDRP supports two [Mixed Lighting Modes](https://docs.unity3d.com/Manual/LightMode-Mixed.html):

* [Baked indirect](https://docs.unity3d.com/Manual/LightMode-Mixed-BakedIndirect.html)
* [Shadowmask](https://docs.unity3d.com/Manual/LightMode-Mixed-ShadowmaskMode.html).

### Enabling shadowmasks

To use shadowmasks in HDRP, you must enable shadowmask support in your Unity Project’s HDRP Asset and then make your Cameras use shadowmasks in their [Frame Settings](Frame-Settings.html) :

1. Under **Render Pipeline Supported Features**, enable the **Shadowmask** checkbox. This enables support for shadowmasks in your Unity Project.
2. Next, you must make your Cameras use shadowmasks. In your HDRP Asset, navigate to **Default Frame Settings > Lighting** and enable the **Shadowmask** checkbox to make Cameras use shadowmasks by default.

### Specific settings in HDRP

For flexible lighting setups, HDRP allows you to choose how you want the shadowmasks to behave for each individual Light. You can change the behavior of the shadowmask by changing a Light’s **Shadowmask Mode**. Set the Light’s **Mode** to **Mixed** to expose **Shadowmask Mode** in the **Shadow Map** drop-down of the **Shadows** section. 

<a name="ShadowmaskModes"></a>

| **Shadowmask Mode**     | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| **Distance Shadowmask** | Makes the Light cast real-time shadows for all GameObjects when the distance between the Camera and the Light is less than a punctual light’s **Fade Distance**. See [below](#DirectionalLightEquivalentProperty) for the alternative distance property that Directional Lights use. When the distance between the Light and the Camera is greater than the **Fade Distance**, HDRP stops calculating real-time shadows for the Light. Instead, it uses shadowmasks for static GameObjects, and non-static GameObjects do not cast shadows. |
| **Shadowmask**          | Makes the Light cast real-time shadows for non-static GameObjects only. It then combines these shadows with shadowmasks for static GameObjects when the distance between the Camera and the Light is less than the **Fade Distance**. When the distance between the Light and the Camera is greater than the **Fade Distance**, HDRP stops calculating real-time shadows for the Light. Instead, it uses shadowmasks for static GameObjects and non-static GameObjects do not cast shadows. |

<a name="DirectionalLightEquivalentProperty"></a>

Directional Lights do not use **Fade Distance**. Instead they use the **Max Distance** property located in the [HD Shadow Settings](Override-Shadows.html) of your Scene’s Volumes.

**Distance Shadowmask** is more GPU intensive, but looks more realistic because real-time lighting that is closer to the Light is more accurate than shadowmask textures with a low resolution chosen to represent areas further away.

**Shadowmask** is more memory intensive because the Camera uses shadowmask textures for static GameObjects close to the Camera, requiring a larger resolution shadowmask texture.

<a name="ShadowUpdateMode"></a>

## Shadow Update Mode

You can use **Update Mode** to specify the calculation method HDRP uses to update a [Light](Light-Component.html)'s shadow maps. The following Update Modes are available:

| **Update Mode** | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| **Every Frame** | HDRP updates the shadow maps for the light every frame.      |
| **On Enable**   | HDRP updates the shadow maps for the light whenever you enable the GameObject. |
| **On Demand**   | HDRP updates the shadow maps for the light every time you request them. To do this, call the RequestShadowMapRendering() method in the Light's HDAdditionalLightData component. |

**Note:** no matter what Update Mode a Light uses, if Unity resizes the content of the shadow atlas (due to shadow maps not fitting on the atlas at their original resolution), Unity also updates the shadow map to perform the required rescaling.

## Contact Shadows

Contact Shadows are shadows that HDRP [ray marches](Glossary.html#RayMarching) in screen space, inside the depth buffer, at a close range. They provide small, detailed, shadows for details in geometry that shadow maps cannot usually capture.

For details on how to enable and customize Contact Shadows, see the [Contact Shadows override documentation](Override-Contact-Shadows.html).

Only one Light can cast Contact Shadows at a time. This means that, if you have more than one Light that casts Contact Shadows visible on the screen, only the dominant Light renders Contact Shadows. HDRP chooses the dominant Light using the screen space size of the Light’s bounding box. A Direction Light that casts Contact Shadows is always the dominant Light.