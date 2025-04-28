# Set Position (Depth)

Menu Path : **Position > Set Position (Depth)**

The **Set Position (Depth)** Block calculates a position based on a Camera and its depth buffer then stores the position to the [position attribute](Reference-Attributes.md).

To calculate the position, this Block first determines the position in screen space, based on different modes (**Random**, **Sequential**, or a **Custom** provided value), then uses the depth buffer and the Camera's properties to convert the position into world space.

You can either use the first [main Camera](https://docs.unity3d.com/ScriptReference/Camera-main.html) in the Scene or provide your own Camera.

To provide your own Camera, create a new Camera property in the [Blackboard](Blackboard.md) and expose it. Then use an [HDRP Camera Property Binder](PropertyBinders.md).

Optionally, this Block can use the calculated screen-space position to sample the color of a pixel in the last color buffer and transfer the result to the color attribute. The last color buffer is the one prior to the post-process pass, available during rendering. This can either be pre-refraction or distortion depending on the camera’s [Frame Settings](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Frame-Settings.html).

Culling options allow you to set the alive attribute to true or false depending on the selected mode. You can use this to kill particles that are on the far plane or outside a specific depth range.

**Important:** For this Block to calculate positions, the specified Camera must be enabled and the Game view must be visible.

<video src="Images/Block-SetPosition(Depth)Main.mp4" title="Particles forming the surfaces of three cubes at different orientations." width="320" height="auto" autoplay="true" loop="true" controls></video>

## Block compatibility

This Block is compatible with the following Contexts:

- [Initialize](Context-Initialize.md)

## Block settings

| **Setting**              | **Type** | **Description**                                              |
| ------------------------ | -------- | ------------------------------------------------------------ |
| **Camera**               | Enum     | The method to decide which Camera to use to calculate depth collision. The options are:<br/>&#8226; **Main**: Uses the main Camera in the Scene. This requires a Camera in the Scene to have the **MainCamera** tag.<br/>&#8226; **Custom**: Allows you to specify a particular Camera to use. |
| **Mode**                 | Enum     | The method this Block uses to distribute sample points across the screen. The options are:<br/>&#8226; **Random**: Calculates a random position on screen and determines the world-space position based on the Camera's properties.<br/>&#8226; **Sequential**: Calculates a sequential position on screen (rows then columns) and determines the world-space position based on the Camera's properties.<br/>&#8226; **Custom**: Determines the world-space position of a screen-space coordinate you specify. |
| **Cull Mode**            | Enum     | The method this Block uses to cull positions. The options are:<br/>&#8226; **None**: Does not cull any positions.<br/>&#8226; **Far Plane**: Culls positions that would be on the far plane of the camera.<br/>&#8226; **Range**: Culls positions that are outside a range you specify. |
| **Inherit Scene Color**  | Bool     | **(Inspector)** Specifies whether the color attribute inherits the Scene color at the calculated position. |
| **Composition Position** | Enum     | **(Inspector)** Specifies how this Block composes the position attribute. The options are:<br/>&#8226; **Set**: Overwrites the position attribute with the new value.<br/>&#8226; **Add**: Adds the new value to the position attribute value.<br/>&#8226; **Multiply**: Multiplies the position attribute value by the new value.<br/>&#8226; **Blend**: Interpolates between the position attribute value and the new value. You can specify the blend factor between 0 and 1. |
| **Composition Color**    | Enum     | **(Inspector)** Specifies how this Block composes the color attribute. The options are:<br/>&#8226; **Set**: Overwrites the color attribute with the new value.<br/>&#8226; **Add**: Adds the new value to the color attribute value.<br/>&#8226; **Multiply**: Multiplies the color attribute value by the new value.<br/>&#8226; **Blend**: Interpolates between the color attribute value and the new value. You can specify the blend factor between 0 and 1. |

## Block properties

| **Input**          | **Type** | **Description**                                              |
| ------------------ | -------- | ------------------------------------------------------------ |
| **Camera**         | Camera   | The Camera to use to calculate the positions. This Block uses the depth buffer from this Camera to calculate the particle positions. <br/>This property only appears if you set **Camera** to **Custom**. |
| **Z Multiplier**   | float    | A multiplier to apply to the particles so they appear closer or further from their depth. |
| **UV Spawn**       | Vector2  | The normalized screen-space position (0..1 range) to sample the depth and calculate the position. <br/>This property only appears if you set **Mode** to **Custom**. |
| **Depth Range**    | Vector2  | Specifies the depth range to spawn particles in. Particles whose position is outside this depth range have their **alive** attribute set to false.<br/>This property only appears if you set **Cull Mode** to **Range**. |
| **Blend Position** | float    | The blend percentage between the current position attribute value and the newly calculated position value.<br/>This property only appears if you set **Composition Position** to **Blend**. |
| **Blend Color**    | float    | The blend percentage between the current direction attribute value and the newly calculated direction value.<br/>This property only appears if you set **Composition Direction** to **Blend**. |

## Details

**Compatibility :** This Block is currently only compatible with the [High Definition Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html), with any graphics API, except metal.
