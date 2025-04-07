# Update light from the sky at runtime with sky occlusion

You can enable sky occlusion when you use Adaptive Probe Volumes. Sky occlusion means that when a GameObject samples a color from the sky, Unity dims the color if the light can't reach the GameObject.

Sky occlusion in Unity uses the sky color from the [ambient probe](https://docs.unity3d.com/2023.3/Documentation/ScriptReference/RenderSettings-ambientProbe.html), which updates at runtime. This means you can dynamically light GameObjects as the sky color changes. For example, you can change the sky color from light to dark, to simulate the effect of a day-night cycle.

If you enable sky occlusion, Adaptive Probe Volumes might take longer to bake, and Unity might use more memory at runtime.

## How sky occlusion works

When you enable sky occlusion, Unity bakes an additional static sky occlusion value into each probe in an Adaptive Probe Volume. The sky occlusion value is the amount of indirect light the probe receives from the sky, including light that bounced off static GamesObjects.

At runtime, when a static or dynamic GameObject samples an Adaptive Probe Volume probe, Unity approximates the light from the sky using two values:

- A sky color from the ambient probe, which updates when the sky color changes.
- The sky occlusion value, which is static.

## Limitations

When Unity calculates sky occlusion values, Unity treats the surfaces of GameObjects as opaque and a gray color. As a result, transparent or translucent GameObjects like windows or leaves bounce light away instead of transmitting it through. Also, dark-colored walls reflect the same amount of light as light-colored walls. To minimize lighting issues, disable **Contribute GI** in the [**Static Editor Flags** property](xref:um-static-objects) for transparent or translucent GameObjects.

To override the gray color Unity uses, go to **Sky Occlusion Settings** in the [Adaptive Probe Volumes panel](probevolumes-lighting-panel-reference.html#sky-occlusion-settings) and adjust **Albedo Override**.

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

To update the light from the sky at runtime, follow these steps to make sure the ambient probe updates when the sky updates. 

1. In the **Hierarchy** window, select the volume that affects the current camera.
2. In the **Inspector** window, double-click the Volume Profile Asset to open the asset.
3. In the **Visual Environment** &gt; **Sky** section, set **Ambient Mode** to **Dynamic**.

Refer to [Environment lighting](environment-lighting.md) for more information.

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
- [Rendering Debugger](rendering-debugger-window-reference.md#probe-volume-panel) for information about displaying baked sky occlusion data
