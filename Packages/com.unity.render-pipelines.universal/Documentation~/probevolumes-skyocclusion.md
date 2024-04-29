# Update light from the sky at runtime with sky occlusion

You can enable sky occlusion when you use Adaptive Probe Volumes. Sky occlusion means that when a GameObject samples a color from the sky, Unity dims the color if the light can't reach the GameObject.

Sky occlusion in Unity uses the sky color from the [ambient probe](https://docs.unity3d.com/2023.3/Documentation/ScriptReference/RenderSettings-ambientProbe.html), which updates at runtime. This means you can dynamically light GameObjects as the sky color changes. For example, you can change the sky color from light to dark, to simulate the effect of a day-night cycle.

If you enable sky occlusion, Adaptive Probe Volumes might take longer to bake, and Unity might use more memory at runtime.

## How sky occlusion works

When you enable sky occlusion, Unity bakes an additional static sky occlusion value into each probe in an Adaptive Probe Volume. The sky occlusion value is the amount of indirect light the probe receives from the sky, including light that bounced off static GamesObjects.

At runtime, when a static or dynamic GameObject samples an Adaptive Probe Volume probe, Unity approximates the light from the sky using two values:

- A sky color from the ambient probe, which updates when the sky color changes.
- The sky occlusion value, which is static.

## Enable sky occlusion

First, enable the GPU lightmapper. Unity doesn't support sky occlusion if you use **Progressive CPU** instead.

1. Go to **Window** &gt; **Rendering** &gt; **Lighting**.
2. Go to the **Scene** panel.
3. Set **Lightmapper** to **Progressive GPU**.

To enable sky occlusion, follow these steps: 

1. Go to the **Adaptive Probe Volumes** panel.
2. Enable **Sky Occlusion**.

To update the lighting data, you must also [bake the Adaptive Probe Volume](probevolumes-use.md#add-and-bake-an-adaptive-probe-volume) after you enable or disable sky occlusion.

## Update light at runtime

To update light from the sky at runtime, you must use colors for the ambient light in your scene, not a skybox. To use colors, do the following:

1. Go to **Window** &gt; **Rendering** &gt; **Lighting**.
2. Go to the **Environment** panel.
3. Set **Source** to either **Gradient** or **Color**.

To update the light from the sky at runtime, do either of the following:

- Use the properties in the [`RenderSettings`](https://docs.unity3d.com/ScriptReference/RenderSettings.html) API to change the sky colors, for example [ambientSkyColor](https://docs.unity3d.com/ScriptReference/RenderSettings-ambientSkyColor.html).
- Use the [**Animation** window](https://docs.unity3d.com/Manual/AnimationEditorGuide.html).

## Enable more accurate sky direction data

When an object samples the ambient probe, by default Unity uses the surface normal of the object as the direction to the sky. This direction might not match the direction the light comes from, for example if the object is inside and the sky light bounces off other objects to reach it.

Unity can instead calculate, store, and use an accurate direction from each Adaptive Probe Volume probe, and take bounce lighting into account. This makes sky occlusion more accurate, especially in areas like caves where probes don't have a direct line of sight to the sky, or when the sky has contrasting colors and the light comes from a specific direction such as through a window.

To enable this feature, in the **Adaptive Probe Volumes** of the Lighting window, enable **Sky Direction**.

If you enable **Sky Direction**, the following applies:

- Baking takes longer and Unity uses more memory at runtime.
- There might be visible seams, because Unity doesn't interpolate sky direction data between probes.

To override the directions Unity uses, use a [Probe Adjustment Volume component](probevolumes-adjustment-volume-component-reference.md).

## Additional resources

- [Adaptive Probe Volumes panel properties](probevolumes-lighting-panel-reference.md#sky-occlusion-settings) for more information about sky occlusion settings
- [Rendering Debugger](features/rendering-debugger.md#probe-volume-panel) for information about displaying baked sky occlusion data
