# Screen Position Node

## Description

Provides access to the mesh vertex or fragment's **Screen Position**. The mode of output value can be selected with the **Mode** dropdown parameter.

**Default**

Returns **Screen Position**. This mode divides **Screen Position** by the clip space position W component.

**Raw**

Returns **Screen Position**. This mode does not divide **Screen Position** by the clip space position W component. This is useful for projection.

**Center**

Returns **Screen Position** offset so position `float2(0,0)` is at the center of the screen.

**Tiled**

Returns **Screen Position** offset so position `float2(0,0)` is at the center of the screen and tiled using `frac`.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Out | Output      |    Vector 4 | None | Mesh's **Screen Position**. |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Mode | Dropdown | Default, Raw, Center, Tiled | Selects coordinate space of **Position** to output. |
