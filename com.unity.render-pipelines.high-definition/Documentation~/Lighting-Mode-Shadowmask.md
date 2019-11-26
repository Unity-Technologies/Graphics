# Shadowmask lighting mode

The High Definition Render Pipelines (HDRP) supports the Shadowmask Lighting Mode which makes the lightmapper precompute shadows for static GameObjects, but still process real-time lighting for non-static GameObjects. HDRP also supports the [Baked Indirect](https://docs.unity3d.com/Manual/LightMode-Mixed-BakedIndirect.html) mixed Lighting Mode. For more information on mixed lighting and shadowmasks, see [Mixed lighting modes](https://docs.unity3d.com/Manual/LightMode-Mixed.html) and [Shadowmask](https://docs.unity3d.com/Manual/LightMode-Mixed-ShadowmaskMode.html).

## Using shadowmasks

To use shadowmasks in HDRP, you must set up your Project to support them. To do this:

1. Enable Distance Shadowmask for every Quality Level in your Unity Project:

2. 1. Open the Project Settings window (menu: Edit > Project Setting) and select the Quality tab.
   2. Select a Level (for example, Medium) then, in the Shadows section, set the Shadowmask Mode to Distance Shadowmask.
   3. Do this for every Quality Level.

3. Enable the Shadowmask property in your Unity Project’s [HDRP Asset](HDRP-Asset.html):

4. 1. In the Project window, select an HDRP Asset to view in the Inspector.
   2. Go to the Lighting section and then to Shadow.
   3. Enable Shadowmask.

5. Set up your Scene to use shadowmask and baked global illumination:

6. 1. Open the Lighting window (menu: Window > Rendering > Lighting Settings).
   2. In the Mixed Lighting section, enable Baked Global Illumination and set the Lighting Mode to Shadowmask.

7. Make your Cameras use shadowmasks when they render the Scene. To set this as the default behaviour for Cameras:

8. 1. Open the Project Settings window (menu: Edit > Project Settings) and select the HDRP Default Settings tab.
   2. In the Frame Settings section, set Default Frame Settings For to Camera.
   3. In the Lighting section, enable Shadowmask.

9. Optionally, you can make your [Reflection Probes](Reflection-Probes-Intro.html) use shadowmask for baked or real-time reflections. To do this, follow the same instructions as in step 3, but set Default Frame Settings For to Baked Or Custom Reflection or Realtime Reflection.

Now, on a [Light](Light-Component.html), when you select the Mixed mode. The lightmapper precomputes Shadowmasks for static GameObject that the Light affects.

## Shadowmask mode

To allow for flexible lighting setups, HDRP lets you choose the behaviour of the shadowmasks for each individual Light. To change the behavior of the shadowmask, use the Light’s Shadowmask Mode property. To do this, set the Light’s Mode to Mixed then go to Shadows > Shadow Map and set the Shadowmask Mode to your desired behavior. For information on the behavior of each Shadowmask Mode, see the following table.

| Shadowmask Mode     | Description                                                  |
| ------------------- | ------------------------------------------------------------ |
| Distance Shadowmask | Makes the Light cast real-time shadows for all GameObjects when the distance between the Camera and the Light is less than the Fade Distance. If you can not see this property, expose [more options](More-Options.html) for the Shadows section. When the distance between the Light and the Camera is greater than the Fade Distance, HDRP stops calculating real-time shadows for the Light. Instead, it uses shadowmasks for static GameObjects, and non-static GameObjects do not cast shadows. Directional Lights don't use Fade Distance, instead they use the current [Max Shadow Distance](Override-Shadows.html). |
| Shadowmask          | Makes the Light cast real-time shadows for non-static GameObjects only. It then combines these shadows with shadowmasks for static GameObjects when the distance between the Camera and the Light is less than the Fade Distance. When the distance between the Light and the Camera is greater than the Fade Distance, HDRP stops calculating real-time shadows for the Light. It uses shadowmasks for static GameObjects and non-static GameObjects do not cast shadows. |

## Details

Distance Shadowmask is more GPU intensive, but looks more realistic because real-time lighting that is closer to the Light is more accurate than shadowmask Textures with a low resolution meant to represent areas further away.

Shadowmask is more memory intensive because the Camera uses shadowmask Textures for static GameObjects close to the Camera, which requires a larger resolution shadowmask Texture.