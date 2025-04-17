# Caustics in the water system
Caustics are a consequence of light rays refracted or reflected by a curved surface and projected onto another object.

## Opacity
Water that is opaque or partially opaque due to particulates does not refract light as much as clear water. This is because water full of mud (for example) absorbs more light than it refracts. In the context of HDRP, this means that water with a lower **Absorption Distance** value has caustics that are less visible.

## Wave size
HDRP uses the **Ripples** **Simulation Band** for **Caustics** calculations by default if the **Ripples** band is active. If you wish to base caustics on larger waves in a **River** or **Ocean, Sea, or Lake** water surface, you need to adjust the **Simulation Band** setting. It may also be necessary to adjust the **Virtual Plane Distance** to ensure a plausible result.  To prevent aliasing artifacts, you can increase the **Caustics Resolution**.

## Limitations
In the current water implementation, caustics do not have an effect above the water unless you script this behavior. For example, if you have a boat sitting in the water, HDRP does not project caustics on the part of its hull that is not submerged, and a swimming pool inside of a room does not bounce caustics off the walls or ceiling.
You can manually implement this effect by creating a **Decal Projector** in the following way:
```cs
this.GetComponent<DecalProjector>().material.SetTexture("_Base_Color", waterSurface.GetFoamBuffer(out Vector2 _));
```

Caustics have the following limitations with transparents GameObjects:
* When the camera is above a water surface, HDRP computes caustics using the position of any opaque object behind a transparent.
* HDRP doesn't apply caustics to transparent GameObjects when the camera is underwater.
* Caustics do not react to current maps and simulation masks.

## Additional resources
* <a href="settings-and-properties-related-to-the-water-system.md">Settings and properties related to the water system</a>
