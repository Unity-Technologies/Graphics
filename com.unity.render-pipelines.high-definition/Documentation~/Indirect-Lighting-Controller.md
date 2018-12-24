# Indirect Lighting Controller

The **Indirect Lighting Controller** is a Volume component that allows you to globally control the intensity of baked or precomputed indirect lighting.

The Indirect Lighting Controller has two properties:

| Property                        | Description                                                  |
| ------------------------------- | ------------------------------------------------------------ |
| **Indirect Diffuse Intensity**  | A multiplier for baked and realtime Global Illumination lightmaps and Light Probes. HDRP multiplies the lightmap and Light Probe data by this value. |
| **Indirect Specular Intensity** | A multiplier for baked, realtime, and custom Reflection Probes. HDRP multiplies the Reflection Probe data by this value. |

This component override is useful in situations where you need to animate your lighting globally.

### Example scenario

An example of a useful situation of where an Indirect Lighting Controller would be useful is when your Camera is in a dark area and you want to light the area suddenly. To create this effect with this override:

1. Create a **Scene Settings** GameObject (menu: **GameObject > Rendering > Scene Settings**) and add an **Indirect Lighting Controller** (click on the Scene Settings GameObject and then, in the Volume component in the Inspector, click **Add component overrides** and select **Indirect Lighting Controller**). 
2. Add a Collider to the Scene Settings GameObject and enable the **Is Trigger** checkbox. Set the **Size** of the Collider to be the size of the area you want to change the indirect lighting for.
3. In the **Indirect Lighting Controller**, set the **Indirect Diffuse Intensity** and **Indirect Specular Intensity** to 0. This dims all indirect lighting to black.
4. When you light the area, animate (using Timeline or an Animation) the Volume's **Weight** property to transition from 1 to 0. This will progressively interpolate between the values set inside the Volume and the values from other Volumes that affect your Camera.
5. As a result, the Indirect lighting globally fades in. Alter the length of the animation to speed up or slow down the fade.