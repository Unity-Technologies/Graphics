# Position Node

## Description

Provides access to the mesh vertex's or fragment's **Position**, depending on the effective [Shader Stage](Shader-Stage.md) of the graph section that the [Node](Node.md) is part of. Use the **Space** drop-down parameter to select the coordinate space of the output value.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Out | Output      |    Vector 3 | None | **Position** for the Mesh Vertex/Fragment. |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Space | Dropdown | Object, View, World, Tangent, Absolute World | Selects the coordinate space of **Position** to output. |

## World and Absolute World
The Position Node provides drop-down options for both **World** and **Absolute World** space positions. The **Absolute World** option always returns the absolute world position of the object in the Scene for all Scriptable Render Pipelines. The **World** option returns the default world space of the selected Scriptable Render Pipeline. 

The [High Definition Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html) uses [Camera Relative](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?preview=1&subfolder=/manual/Camera-Relative-Rendering.html) as its default world space. 

The [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html) uses **Absolute World** as its default world space.

### Upgrading from previous versions
If you use a Position Node in **World** space on a graph authored in Shader Graph version 6.7.0 or earlier, it automatically upgrades the selection to **Absolute World**. This ensures that the calculations on your graph remain accurate to your expectations, since the **World** output might change.

If you use a Position Node in **World** space in the High Definition Render Pipeline to manually calculate [Camera Relative](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?preview=1&subfolder=/manual/Camera-Relative-Rendering.html) world space, you can now change your node from **Absolute World** to **World**, which lets you use [Camera Relative](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?preview=1&subfolder=/manual/Camera-Relative-Rendering.html) world space out of the box.