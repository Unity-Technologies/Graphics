# Is Front Face Node

## Description

Returns true if currently rendering a front face and false if rendering a back face. This value is always true unless the [Master Node](Master-Node.md)'s **Two Sided** value is set to true in the **Material Options**. This is useful for [Branching](Branch-Node.md).

NOTE: This [Node](Node.md) can only be used in the **Fragment** [Shader Stage](Shader-Stage.md).

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Out | Output      |    Boolean | None | Output value |
