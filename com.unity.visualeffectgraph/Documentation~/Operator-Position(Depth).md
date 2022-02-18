# Position (Depth)

Menu Path : **Operator > Sampling > Position (Depth)**

The **Position (Depth)** Operator samples the depth-buffer of a Camera and retrieves the position in world space. You can use this depth information to project particles into the scene.

## Operator settings

| **Setting**             | **Type** | **Description**                                              |
| ----------------------- | -------- | ------------------------------------------------------------ |
| **Camera**              | Enum     | Specifies which Camera to sample the depth of. The options are:<br/>&#8226;  **Main**: Uses the first Camera in the scene with the **MainCamera** tag.<br/>&#8226; **Custom**: Uses the Camera you specify in the **Camera** port. |
| **Mode**                | Enum     | Specifies the method this Operator uses to sample the depth-buffer. The options are:<br/>&#8226; **Random**: Uses a random UV value between 0 and 1 to sample the depth-buffer.<br/>&#8226; **Sequential**: Selects UVs based on the ParticleID attribute and the value of the **Grid Step** property. This samples positions sequentially every **Grid Step** number of pixels.<br/>&#8226; **Custom**: Allows you to specify UVs manually with the **UV Spawn** property. |
| **Cull Mode**           | Enum     | Specifies a filter to apply to a sampled position. If you use a filter, the output property **isAlive** describes whether the sampled position is valid or not. The options are:<br/>&#8226; **None**: Does not use a filter.<br/>&#8226; **Far Plane**: If the sampled position is on the far plane, this option sets **isAlive** to `false`.<br/>&#8226; **Range**: Allows you to specify a **Depth Range** which determines whether a sampled position is valid or not. If the sampled position is outside the **Depth Range**, this option sets **isAlive** to `false`. |
| **Inherit Scene Color** | bool     | (**Inspector**) Specifies whether or not this Operator outputs the Camera's scene color information in addition to depth-buffer position/. |

## Operator properties

| **Property**     | **Type** | **Description**                                              |
| ---------------- | -------- | ------------------------------------------------------------ |
| **Camera**       | Camera   | The Camera to use. <br/>This property only appears if you set **Camera** to **Custom**. |
| **Z Multiplier** | float    | A multiplier that offsets the sampled depth position. You can use this to avoid issues with z-fighting/overlapping when projecting particles into your scene. |
| **Grid Step**    | uint     | The size of the grid this Operator uses to sample the depth buffer in pixels. This is based on the particleID and the Operator samples positions sequentially for every strip of pixels. <br/>This property only appears if you set **Mode** to **Sequential**. |
| **UV Spawn**     | Vector2  | The UV this Operator uses to manually sample the depth buffer. <br/>This property only appears if you set **Mode** to **Custom**. |
| **Depth Range**  | Vector2  | The valid depth range for the sampled position. If the sampled position is within this range, the **isAlive** property is `true`, otherwise it is `false`. This allows you to filter out sampled positions. <br/>This property only appears if you set **Cull Mode** to **Range**. |

## Output properties

| **Property** | **Type** | **Description**                                              |
| ------------ | -------- | ------------------------------------------------------------ |
| **Position** | Position | The world-space position of the Camera's depth buffer sample. |
| **color**    | Color    | The color of the Camera's scene color buffer sample. <br/>This property only appears if you enable **Inherit Scene Color**. |
| **isAlive**  | bool     | Specifies whether the sampled position is valid with respect to the **Cull Mode** setting.<br/>&#8226; **Far Plane**: This is `false` when the sampled position is on the far plane and `true` otherwise.<br/>&#8226; **Range**: This is `false` when the sampled position is outside of the valid **Depth Range** and `true` otherwise. <br/>This property only appears if you set **Cull Mode** to **Far Plane** or **Range**. |

## Limitations

Currently, depth buffer sampling is only available in the High Definition Render Pipeline and does not work in the Universal Render Pipeline.
