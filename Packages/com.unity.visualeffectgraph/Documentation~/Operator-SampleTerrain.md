# Sample Terrain Operator

Menu Path : **Operator > Sampling > Sample Terrain**

The **Sample Terrain** Operator reads the heightmap of a Unity Terrain and outputs the terrain elevation at the sampled location. You can sample the terrain with a normalized texture coordinate, or with a world space position.

The Operator samples the terrain's heightmap texture at the center of the closest texel. Because a Unity heightmap stores one extra sample along each axis, for example 513x513 samples for a 512x512 terrain, the Operator remaps the incoming coordinate so that a **UV** of (0, 0) reads the first sample and a **UV** of (1, 1) reads the last one.

To feed the **Terrain** port at runtime, expose a [TerrainType](Type-TerrainType.md) property and add a **Terrain** [property binder](PropertyBinders.md) to the GameObject that holds the Visual Effect component.

## Operator settings

| **Setting**       | **Type** | **Description**                                              |
| ----------------- | -------- | ------------------------------------------------------------ |
| **Sampling Type** | Enum     | The type of coordinate this Operator samples the terrain with. The options are:<ul><li>**UV**: Samples the terrain with a normalized UV coordinate, where (0, 0) is the corner of the terrain at its origin and (1, 1) is the opposite corner.</li><li>**Position**: Samples the terrain with a world space position. The Operator converts the position into a UV coordinate and ignores its Y component.</li></ul> |

## Operator properties

| **Input**    | **Type**                             | **Description**                                              |
| ------------ | ------------------------------------ | ------------------------------------------------------------ |
| **Terrain**  | [TerrainType](Type-TerrainType.md)   | The terrain this Operator samples from.                      |
| **UV**       | Vector2                              | The texture coordinate to sample the terrain at. This property only appears if you set **Sampling Type** to **UV**. |
| **Position** | [Position](Type-Position.md)         | The world space position to sample the terrain at. This property only appears if you set **Sampling Type** to **Position**. |

| **Output**             | **Type** | **Description**                                              |
| ---------------------- | -------- | ------------------------------------------------------------ |
| **Height (Normalized)**| float    | The terrain elevation at the sampled location, in the range 0 to 1, where 1 is the terrain's maximum height. |
| **Height (World)**     | float    | The terrain elevation at the sampled location, in world space. This value includes the Y position of the Terrain GameObject, so you can connect it directly to a world space position. |

## Limitations

The Operator has the following limitations:

- This Operator only runs on the GPU, so it doesn't work when plugged into **Spawner Context** ports.
- The Operator doesn't clamp the sampled coordinate. If a **UV** falls outside the 0 to 1 range, or a **Position** falls outside the terrain's footprint, the result depends on the wrap mode of the heightmap texture.
- The Operator samples the heightmap texture, not the terrain's collider, so it doesn't account for terrain trees, details, or holes.

## Additional resources

- [TerrainType](Type-TerrainType.md)
- [Property binders](PropertyBinders.md)
- [Sample Texture2D Operator](Operator-SampleTexture2D.md)
