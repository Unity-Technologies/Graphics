# Screen Space Size

Menu Path : **Output > Screen Space Size**

The **Screen Space Size** Block calculates the scaleXYZ property of each particle to reach a size relative to the pixel size or screen size.

## Block compatibility

This Block is compatible with the following Contexts:

- Any output Context

## Block settings

| **Setting**     | **Type** | **Description**                                              |
| --------------- | -------- | ------------------------------------------------------------ |
| **Size Mode**   | Enum     | Specifies how to resize the particles. This can be an absolute pixel value, or a size relative to the resolution. The options are:<br/>&#8226; **Pixel Absolute**: Uses a size in absolute pixel width.<br/>&#8226; **Pixel Relative To Resolution**: Uses a size in absolute pixel width assuming the screen is at a target resolution. At the target resolution, this is equivalent to **Pixel Absolute**.<br/>&#8226; **Ratio Relative To Width**: Uses a size relative to the current width of the rendering target, a value of 0.1 makes the ScaleXY represent 10% of the screen width.<br/>&#8226; **Ratio Relative To Height**: Uses a size relative to the current height of the rendering target, a value of 0.1 makes the ScaleXY represent 10% of the screen height.<br/>&#8226; **Ratio Relative To Height And Width**: Uses a size relative to the current height and width of the rendering target, a value of 0.1 makes the ScaleX represent 10% of the screen width and the ScaleY represent 10% of the screen height. Thus, ScaleXY has the same ratio as the screen. |
| **Size Z Mode** | Enum     | **(Inspector)** Specifies how to calculate the z-axis scale of the particle if the system uses the ScaleZ attribute. The options are:<br/>&#8226; **Ignore** : Doesn’t modify the particles' ScaleZ.<br/>&#8226; **Same As Size X** : Uses a particle's ScaleX value as its ScaleZ.<br/>&#8226; **Same As Size Y** : Uses a particle's ScaleY value as its ScaleZ.<br/>&#8226; **Min Of Size XY** : Uses the lowest value between a particle's ScaleX and ScaleY as its ScaleZ.<br/>&#8226; **Max Of Size XY** : Uses the highest value between a particle's ScaleX and ScaleY as its ScaleZ.<br/>&#8226; **Average Of Size XY** : Uses the mean of a particle's ScaleX and ScaleY as its ScaleZ. |

## Block properties

| **Input**                | **Type** | **Description**                                              |
| ------------------------ | -------- | ------------------------------------------------------------ |
| **Pixel Size**           | float    | The size of the particle in pixels.<br/>This property only appears if you set **Size Mode** to **Pixel Absolute** or **Pixel Relative To Resolution**. |
| **Relative Size**        | float    | The ratio of the particle in relation to the selected size mode. A value of **1.0** means that the particle resizes to match the specified screen dimension.<br/>This property only appears if you set **Size Mode** to **Ratio Relative To Height**, **Ratio Relative To Width**, or **Ratio Relative To Height And Width**. |
| **Reference Resolution** | Vector2  | The screen resolution to set the particle’s pixel size relative to. <br/>This property only appears if you set **Size Mode** to **Pixel Relative To Resolution**. |
