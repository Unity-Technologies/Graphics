# Set Attribute from Map

Menu Path : **Attribute > Set \<Attribute> From Map**

The **Set Attribute from Map** Block is a generic Block that calculates values based on data it samples from Textures and then composes those values into a given **attribute.**

To achieve different results, this Block can use various sampling modes. The sampling modes are:

- **Index**, **IndexRelative**, and **Sequential** sample modes use particle indices to sample the pixels of the texture. These modes can sample [Point Caches](PointCaches.md) or [Attribute Maps](PropertyBinders.md) in various ways.
- **Sample2DLOD** and **Sample3DLOD** sample modes use 2D and 3D coordinates and a LOD factor to sample the texture. You can use these modes for projection of various values such as color or depth.
- **Random** and **RandomUniformPerParticle** sample modes allow you to fetch random values from a pool of values stored in a texture, such as Point Caches or Attribute Maps.

After this Block samples a value from the Texture, it can also apply a scale and a bias to it. For example, if a texture stores unsigned-normalized values where 0 is middle gray, you can apply a bias of -0.5 to reinterpret the value as zero.

## Block compatibility

This Block is compatible with the following Contexts:

- [Initialize](Context-Initialize.md)
- [Update](Context-Update.md)
- Any output Context

## Block settings

| **Setting**     | **Type**  | **Description**                                              |
| --------------- | --------- | ------------------------------------------------------------ |
| **Attribute**   | Attribute | **(Inspector)** Specifies the attribute to write to.         |
| **Composition** | Enum      | **(Inspector)** Specifies how this Block composes the attribute. The options are:<br/>&#8226; **Set**: Overwrites the position attribute with the new value.<br/>&#8226; **Add**: Adds the new value to the position attribute value.<br/>&#8226; **Multiply**: Multiplies the position attribute value by the new value.<br/>&#8226; **Blend**: Interpolates between the position attribute value and the new value. You can specify the blend factor between 0 and 1. |
| **Sample Mode** | Enum      | Specifies how the Block samples the Texture. The options are:<br/>&#8226; **IndexRelative**: Uses the value read from a float port to determine the pixel index. The input value is expected in the 0..1 range and will be multiplied by the number of pixels of the texture to determine the index.<br/>&#8226; **Index**: Uses the value read from an integer port as the pixel index.<br/>&#8226; **Sequential**: Uses the particle ID attribute as the pixel index.<br/>&#8226; **Sample2DLOD**: uses the coordinate provided from a vector2 input port and the LOD from another input port to sample the 2D texture.<br/>&#8226; **Sample3DLOD**: uses the coordinate provided from a vector3 input port and the LOD from another input port to sample the 3D texture.<br/>&#8226; **Random**: uses a random 2D/3D Position to sample the texture<br/>&#8226; **RandomUniformPerParticle**: uses a unique (per particle) 2D/3D position to sample the texture |
| **Channels**    | Enum      | Specifies which channels of the attribute this Block affects. This Block does not affect channels you do not include in this property.<br/>This setting only appears if you set the **Attribute** to one with channels. |

##  Block properties

| **Input**           | **Type**            | **Description**                                              |
| ------------------- | ------------------- | ------------------------------------------------------------ |
| **Attribute Map**   | Texture2D/Texture3D | The texture this Block samples from.                         |
| **RelativePos**     | float               | The index to sample from relative to number of pixels in the texture. This property expects values in the range of 0 to 1 which the Block remaps to the range of 0 to N where N is the total number of pixels in the texture (width * height).<br/>This property only appears if you set **Sample Mode** to **Index Relative**. |
| **Index**           | uint                | The index to sample from. This property expects values in the range of 0 to N where N is the total number of pixels in the texture (width * height).<br/>This property only appears if you set **Sample Mode** to **Index**. |
| **Sample Position** | Vector2/Vector3     | The coordinate in the 2D or 3D Texture to sample from.<br/>This property only appears if you set **Sample Mode** to **Sample2DLOD** or **Sample3DLOD**. |
| **LOD**             | float               | The LOD of the 2D or 3D Texture to sample from.<br/>This property only appears if you set **Sample Mode** to **Sample2DLOD** or **Sample3DLOD**. |
| **Seed**            | uint                | The seed this Block uses to calculate random values.<br/>This property only appears if you set **Sample Mode** to **RandomUniformPerParticle**. |
| **Blend**           | float               | The blend percentage between the current of the attribute value and the newly calculated value.<br/>This property only appears if you set **Composition** or **Alpha Composition** to **Blend**. |