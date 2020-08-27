# Data Types

## Description

There are a number of **Data Types** in [Shader Graph](Shader-Graph.md). Each **Port** on a [Node](Node.md) has an associated **Data Type** that defines what edges can be connected to it. The **Data Types** have colors for usability, these colors are applied to ports and edges of that **Data Type**.

Some **Data Types** have associated [Property Types](Property-Types.md) for exposing these values to the [Inspector](https://docs.unity3d.com/Manual/UsingTheInspector.html) for [Materials](https://docs.unity3d.com/Manual/class-Material.html) that use the shader.

## Data Types

| Name        | Color           | Description |
|:------------|:----------------|:------------|
| Vector 1 | Light Blue | A **Vector 1** or scalar value |
| Vector 2 | Green | A **Vector 2** value |
| Vector 3 | Yellow | A **Vector 3** value |
| Vector 4 | Pink | A **Vector 4** value |
| Dynamic Vector | Light Blue | See **Dynamic Data Types** below |
| Matrix 2 | Blue | A **Matrix 2x2** value |
| Matrix 3 | Blue | A **Matrix 3x3** value |
| Matrix 4 | Blue | A **Matrix 4x4** value |
| Dynamic Matrix | Blue | See **Dynamic Data Types** below |
| Dynamic | Blue | See **Dynamic Data Types** below |
| Boolean | Purple | A **Boolean** value. Defined as a float in the generated shader |
| Texture 2D | Red | A [Texture 2D](https://docs.unity3d.com/Manual/class-TextureImporter.html) asset |
| Texture 2D Array | Red | A [Texture 2D Array](https://docs.unity3d.com/Manual/class-TextureImporter.html) asset |
| Texture 3D | Red | A [Texture 3D](https://docs.unity3d.com/Manual/class-TextureImporter.html) asset |
| Cubemap | Red | A [Cubemap](https://docs.unity3d.com/Manual/class-Cubemap.html) asset |
| Virtual Texture | Gray | A [Texture Stack](https://docs.unity3d.com/2020.1/Documentation/Manual/svt-use-in-shader-graph.html) |
| Gradient | Gray | A **Gradient** value. Defined as a struct in the generated shader |
| SamplerState | Gray | A state used for sampling a texture |

## Promoting/Truncating

All **Vector** types can be promoted or truncated to match any **Vector** type [Port](Port.md). This behaviour occurs only when the [Port](Port.md) in question is not of type **Dynamic Vector**. When truncating, excess channels are simply removed. When promoting, the extra required channels are filled by default values. These values values are (0, 0, 0, 1).

## Dynamic Data Types

Some **Data Types** are dynamic. This means a port using these **Data Types** can change their underlying **Concrete Data Type** based on what **Data Type** is connected to it. By default, [Nodes](Node.md) using dynamic **Data Types** can only have one **Concrete Data Type**, meaning that once a connected edge has applied its **Data Type** to that port, all other **Dynamic Data Type** slots of that [Node](Node.md) will apply the same **Data Type**.

One notable exception to this is the [Multiply Node](Multiply-Node.md) which allows both **Dynamic** **Matrix** and **Vector** types.

### Dynamic Vector

The **Dynamic Vector** type allows connected edges of any **Vector** type. All connected edges are automatically truncated to the type with the lowest dimension, unless the lowest dimension is 1, in which case the **Vector 1** is promoted.

### Dynamic Matrix

The **Dynamic Matrix** type allows connected edges of any **Matrix** type. All connected edges are automatically truncated to the type with the lowest dimension.

### Dynamic

The **Dynamic** type is a special case. [Nodes](Node.md) that support it must define how it is validated. In the case of the [Multiply Node](Multiply-Node.md), it allows connections of any **Vector** or **Matrix** type, ensuring the correct multiplication is applied depending on the mix of **Data Types**. 