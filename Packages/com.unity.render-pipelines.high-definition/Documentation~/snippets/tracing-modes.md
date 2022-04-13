## Tracing modes

The properties visible in the Inspector change depending on the option you select from the **Tracing** drop-down:

- To use a screen-space, ray-marched solution, select **Ray Marching** and see [Screen-space](#screen-space) for the list of properties.
- To use ray tracing, select **Ray Tracing** and see [Ray-traced](#ray-traced) for the list of properties.
- To use a combination of ray tracing and ray marching, select **Mixed** and see [Ray-traced](#ray-traced) for the list of properties. For more information about mixed tracing mode, see [mixed tracing](#mixed-tracing)

### Mixed tracing

This option uses ray marching to intersect on-screen geometry and uses ray tracing to intersect off-screen geometry. This enables HDRP to include on-screen opaque particles, vertex animations, and decals when it processes the effect. This option only works in [Performance mode](../Ray-Tracing-Getting-Started.md#ray-tracing-mode) and with Lit Shader Mode setup to Deferred.

In mixed tracing mode, HDRP processes screen-space ray marching in the GBuffer. This means that it can only use GameObjects rendered using the [deferred](../Forward-And-Deferred-Rendering.md) rendering path. For example, HDRP renders transparent GameObjects in the forward rendering path which means they do not appear in the GBuffer and thus not in effects that use mixed tracing.

In mixed tracing mode, HDRP still uses ray tracing for any geometry inside the ray tracing acceleration structure, regardless of whether vertex animation or decals modify the geometry's surface. This means if HDRP fails to intersect the on-screen deformed geometry, it intersects the original mesh inside in the ray tracing acceleration structure. This may cause visual discrepancies between what you see and what you expect. For example, the following Scene contains a cliff that uses mesh deformation.

![](../Images/mixed-tracing-mixed.png)

*In this Scene, Mixed mode can include reflections for the opaque leaf particles, the white decal, and for GameObjects that are not visible in the cliff face's original, non-deformed, geometry.*

![](../Images/mixed-tracing-ray-traced.png)

*Ray tracing mode does not render reflections for the white decal or for the opaque leaf particles. Also, reflection rays intersect with the original, non-deformed, cliff face geometry which means they can not see the rock and bush on the right-hand side. To see the Scene from the perspective of the ray tracing mode, see the following image.*

![](../Images/mixed-tracing-ray-traced-no-deform.png)

*This is the Scene from the perspective of the ray tracing mode. See how the original, non-deformed, cliff face geometry hides the rock and bush that were on the right-hand side of the Scene.*

### Tracing Modes Limitation

#### Ray Marching

* Transparent Emissive Material are taken into account only when Rendering Pass is set to "Before Refraction".

#### Ray Tracing

* Transparent Emissive Material are not taken into account.
* No [decals](../decal.md) are supported including Emissive Decals.

#### Mixed Tracing

* The Mixed tracing mode is only useful if Lit shader mode is Deferred and have the same limitation than Ray Tracing mode.
