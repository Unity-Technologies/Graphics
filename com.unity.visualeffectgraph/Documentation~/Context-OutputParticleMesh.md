# Output Particle Mesh

Menu Path : **Context > Output Particle Mesh**

*(Output Particle (Lit) Mesh)*

The Output Particle Mesh Context allows you to render particles using meshes. They come in a regular (unlit) and a [Lit](Context-OutputLitSettings.md) variety (HDRP-only).

Below is a list of settings and properties specific to the Output Particle Mesh Context. For information about the generic output settings this Context shares with all other Contexts, see [Global Output Settings and Properties](Context-OutputSharedSettings.md).

## Context settings

| **Setting**    | **Type**      | **Description**                                              |
| -------------- | ------------- | ------------------------------------------------------------ |
| **Mesh Count** | uint (slider) | **(Inspector)** The number of different meshes to use with this output (from 1 to 4). You can select a mesh for a particle by index. This uses the particle's *meshIndex* attribute. |
| **Lod**        | bool          | **(Inspector)** Indicates whether the particle mesh uses [levels of details](https://docs.unity3d.com/Manual/LevelOfDetail.html) (LOD).If you enable this setting, the Context bases mesh selection on the particle's apparent size on screen. To specify values for the LOD mesh selection, use the **Lod Values** property. |

## Context properties

| **Input**             | **Type**    | **Description**                                              |
| --------------------- | ----------- | ------------------------------------------------------------ |
| **Mesh [N]**          | Mesh        | The mesh(es) to use to render particles. The number of mesh input fields depends on the **Mesh Count** setting. |
| **Sub Mesh Mask [N]** | uint (mask) | The sub mesh mask(s) to use for each mesh. The number of sub mesh mask fields depends on the **Mesh Count** setting. |
| **Lod Values**        | Vector4     | The threshold values the Context uses to choose between LOD levels. The values represent a percentage of the viewport along one dimension (For instance, a Value of 10.0 means 10% of the screen). The Context tests values from left to right (0 to n) and selects the LOD level only if the percentage of the particle on screen is above the threshold. This means you have to specify LOD values in decreasing order. Note that you can also use LOD with a mesh count of 1 to cull small particles on screen. This property only appears if you enable the **LOD** setting. |
| **Radius Scale**      | float       | The scale to apply when selecting the LOD level per particle. By default, the LOD system assumes meshes bounding boxes are unit boxes. If your mesh bounding boxes is smaller/bigger than the unit box, you can use this property to apply a scale so that lod thresholds are consistent with apparent size. This property only appears if you enable the **LOD** setting. |

## Limitations

Note that you cannot specify mesh and sub mesh masks because they are CPU expressions and not GPU expressions. If you want to use a variety of meshes to render your particles, you have to use the multi mesh system instead by specifying a mesh count more than 1 (up to 4) and use the *meshIndex* attribute

Per-particle sorting and multi-mesh are not global to the output. Sorting only happens between particles using the same mesh index. This means that particle using mesh 0 always render above particle using mesh 1 for instance no matter what their distance from the camera is.

## Performance considerations

Using multi meshes actually issues different draw calls. This means that there can be some CPU overhead when using this feature. It’s equivalent to using several outputs, but is a bit more optimized. It also means that using LOD as an optimization is only suitable for large systems with many particles.

If you use a mesh with many vertices and your system has a low occupancy (alive particle count / capacity), it might be better to switch rendering mode (a setting in the output inspector) to indirect to remove workload at the vertex stage and gain performance.