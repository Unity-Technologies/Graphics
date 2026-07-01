# Is Front Face Node

## Description

Returns true if currently rendering a front face and false if rendering a back face. This value is always true unless the **Render Face** property in the [Graph Settings](Graph-Settings-Tab.md) is set to **Both**. This is useful for [Branching](Branch-Node.md).

NOTE: This [Node](Node.md) can only be used in the **Fragment** [Shader Stage](Shader-Stage.md).

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Out | Output      |    Boolean | None | Output value |
