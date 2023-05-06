# Screen Position Node

## Description

Provides access to the screen position of the mesh vertex or fragment. The X and Y values represent the horizontal and vertical positions respectively. Use the **Mode** dropdown control to select the mode of the output value. The available modes are as follows:

- **Default** - Returns X and Y values that represent the normalized **Screen Position**. The normalized **Screen Position** is the **Screen Position** divided by the clip space position W component. The X and Y value ranges are between 0 and 1 with position `float2(0,0)` at the lower left corner of the screen. The Z and W values aren't used in this mode, so they're always 0. 

- **Raw** - Returns the raw **Screen Position** values, which are the **Screen Position** values before the clip space position W component is divided out. Position `float2(0,0)` is at the lower left corner of the screen. This mode is useful for projection.

- **Center** - Returns X and Y values that represent the normalized **Screen Position** offset so position `float2(0,0)` is at the center of the screen. The range of the X and Y values is –1 to 1. The Z and W values aren't used in this mode, so they're always 0. 

- **Tiled** - Returns **Screen Position** offset so position `float2(0,0)` is at the center of the screen and tiled using `frac`.


## Ports

| Name        | Direction           | Type     | Binding | Description |
|:------------|:--------------------|:---------|:--------|:------------|
| Out         | Output              | Vector 4 | None    | Get the **Screen Position** of the mesh. |

## Controls

| Name  | Type     | Options  | Description |
|:------|:---------|:---------|:------------|
| Mode  | Dropdown | Default, Raw, Center, Tiled | Select which coordinate space to use for the **Screen Position** output. |

## Generated Code Example

The following code examples represent one possible outcome for each mode.

**Default**

```
float4 Out = float4(IN.NDCPosition.xy, 0, 0);
```

**Raw**

```
float4 Out = IN.ScreenPosition;
```

**Center**

```
float4 Out = float4(IN.NDCPosition.xy * 2 - 1, 0, 0);
```

**Tiled**

```
float4 Out = frac(float4((IN.NDCPosition.x * 2 - 1) * _ScreenParams.x / _ScreenParams.y, IN.{0}.y * 2 - 1, 0, 0));
```
