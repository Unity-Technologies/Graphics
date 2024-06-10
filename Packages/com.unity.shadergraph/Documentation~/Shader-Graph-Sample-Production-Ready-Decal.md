# Decals
Decals allow you to apply local material modifications to specific places in the world. You might think of things like applying graffiti tags to a wall or scattering fallen leaves below a tree. But decals can be used for a lot more. In these examples, we see decals making things look wet, making surfaces appear to have flowing water across them, projecting water caustics, and blending specific materials onto other objects.

Decals are available to use in both HDRP and URP, but they need to be enabled in both render pipelines. To use decals, refer to the documentation in both [HDRP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.0/manual/decals.html) and [URP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/renderer-feature-decal.html).

#### Material Projection
This decal uses triplanar projection to project a material in 3D space. It projects materials correctly onto any mesh that intersects the decal volume.  It can be used to apply terrain materials on to other objects like rocks so that they blend in better with the terrain.
#### Water Caustics
When light shines through rippling water, the water warps and focuses the light, casting really interesting rippling patterns on surfaces under the water.  This shader creates these rippling caustic patterns. If you place decals using this shader under your water planes, you’ll get projected caustics that imitate the behavior of light shining through the water.
#### Running Water
This decal creates the appearance of flowing water across whatever surfaces are inside the decal. It can be used on the banks of streams and around waterfalls to support the appearance of water flowing. With material parameters, you can control the speed of the water flow, the opacity of both the wetness and the water, and the strength of the flowing water normals.
#### Water Wetness
The wetness decal makes surfaces look wet by darkening color and increasing smoothness. It uses very simple math and no texture samples so it is very performance efficient. It can be used along the banks of bodies of water to better integrate the water with the environment.