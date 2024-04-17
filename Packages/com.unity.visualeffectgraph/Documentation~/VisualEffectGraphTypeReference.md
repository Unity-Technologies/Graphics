# VFX type reference

This page references all the types the Visual Effect Graph uses. Some of these are standard C# and Unity types whereas others are unique to the Visual Effect Graph.

## Base types

Base types refer to standard C# and Unity types that you can use to store basic data.

### Attribute-base types

Attribute-base Types are types that you can use for new attribute storage in Blackboard's corresponding tab section.

| **Type**    | **Color**                                                     | **Description**                                              |
| ----------- | ------------------------------------------------------------- | ------------------------------------------------------------ |
| **float**   | <div style="width:20px;height:20px;background-color:#84e4e6"/> | A 32-bit floating-point value.                               |
| **int**     | <div style="width:20px;height:20px;background-color:#9480e6"/> | A 32-bit signed integer value.                               |
| **uint**    | <div style="width:20px;height:20px;background-color:#6e55db"/> | A 32-bit unsigned integer value.                             |
| **bool**    | <div style="width:20px;height:20px;background-color:#d9b3ff"/>| A 1-bit boolean value.                                       |
| **Vector2** | <div style="width:20px;height:20px;background-color:#9aef92"/>| A two-dimensional 32-bit floating-point vector.              |
| **Vector3** | <div style="width:20px;height:20px;background-color:#f6ff9a"/> | A three-dimensional 32-bit floating-point vector.            |
| **Vector4** | <div style="width:20px;height:20px;background-color:#fbcbf4"/> | A four-dimensional 32-bit floating-point vector.             |
| **Color**   | <div style="width:20px;height:20px;background-color:#fbcbf4"/> | A four-component (Red, Green, Blue, Alpha) 32-bit floating-point linear Color. |


### Other base types

The following base types are compatible with properties, but not with attributes.

| **Type**             | **Color**                                                     | **Description**                                              |
| -------------------- | ------------------------------------------------------------- | ------------------------------------------------------------ |
| **AnimationCurve**   | <div style="width:20px;height:20px;background-color:#FFAA6B"/> | Unity's [AnimationCurve](https://docs.unity3d.com/ScriptReference/AnimationCurve.html) type. |
| **Gradient**         | <div style="width:20px;height:20px;background-color:#FFAA6B"/> | Unity's [Gradient](https://docs.unity3d.com/ScriptReference/Gradient.html) type. |
| **Texture2D**        | <div style="width:20px;height:20px;background-color:#FF8B8B"/> | Unity's [Texture2D](https://docs.unity3d.com/ScriptReference/Texture2D.html) type. |
| **Texture3D**        | <div style="width:20px;height:20px;background-color:#FF8B8B"/> | Unity's [Texture3D](https://docs.unity3d.com/ScriptReference/Texture3D.html) type. |
| **TextureCube**      | <div style="width:20px;height:20px;background-color:#FF8B8B"/> | Unity's [CubeMap](https://docs.unity3d.com/ScriptReference/Cubemap.html) type. |
| **Texture2DArray**   | <div style="width:20px;height:20px;background-color:#FF8B8B"/> | Unity's [Texture2DArray](https://docs.unity3d.com/ScriptReference/Texture2DArray.html) type. |
| **TextureCubeArray** | <div style="width:20px;height:20px;background-color:#FF8B8B"/> | Unity's [CubeMapArray](https://docs.unity3d.com/ScriptReference/CubemapArray.html) type. |
| **Mesh**             | <div style="width:20px;height:20px;background-color:#7AA3EA"/> | Unity's [Mesh](https://docs.unity3d.com/ScriptReference/Mesh.html) type. |


## Advanced types

This section describes the advanced types that the Visual Effect Graph includes. These are either advanced versions of the base types or composite types (made up of multiple properties).

### Spaceable base types

Spaceable base types are Vector types that embed a space alongside their value. They also use a vector semantic when performing space transformations.

| **Type**      | **Color**                                                     | **Description**                                              |
| ------------- | ------------------------------------------------------------- | ------------------------------------------------------------ |
| **Position**  | <div style="width:20px;height:20px;background-color:#FCD76E"/>| A world-space or local-space three-component position vector. For more information on this type and its properties, see [Position](Type-Position.md). |
| **Vector**    | <div style="width:20px;height:20px;background-color:#FCD76E"/>| A world-space or local-space three-component vector. For more information on this type and its properties, see [Vector](Type-Vector.md). |
| **Direction** | <div style="width:20px;height:20px;background-color:#FCD76E"/>| A world-space or local-space three-component normalized direction. Values of this type are always normalized when you retrieve them. For more information on this type and its properties, see [Direction](Type-Direction.md). |


### Shape types

Shape Types are Advanced types that define a shape based on a composition on base types.

| **Type**        | **Color**                                                     | **Description**                                              |
| --------------- | ------------------------------------------------------------- | ------------------------------------------------------------ |
| **Sphere**      | <div style="width:20px;height:20px;background-color:#c8c8c8"/>| A sphere defined by a center position and a radius. For more information on this type and its properties, see [Sphere](Type-Sphere.md). |
| **ArcSphere**   | <div style="width:20px;height:20px;background-color:#c8c8c8"/>| An oriented box defined by a position, a Euler angle (in degrees), and a scale. For more information on this type and its properties, see [OrientedBox](Type-OrientedBox.md). |
| **Circle**      | <div style="width:20px;height:20px;background-color:#c8c8c8"/>| A 3D circle oriented on the XY plane, defined by a position and a radius. For more information on this type and its properties, see [Circle](Type-Circle.md). |
| **ArcCircle**   | <div style="width:20px;height:20px;background-color:#c8c8c8"/>| A solid-arc of a circle, defined by an angle and a [Circle](Type-Circle.md). For more information on this type and its properties, see [ArcCircle](Type-ArcCircle.md). |
| **Torus**       | <div style="width:20px;height:20px;background-color:#c8c8c8"/>| A 3D torus oriented on the XY plane, defined by a position, a major radius (torus radius), and a minor radius (torus thickness). For more information on this type and its properties, see [Torus](Type-Torus.md). |
| **ArcTorus**    | <div style="width:20px;height:20px;background-color:#c8c8c8"/>| A solid-arc of a torus defined by an angle and a 3D [Torus](Type-Torus.md). For more information on this type and its properties, see [ArcTorus](Type-ArcTorus.md). |
| **Cone**        | <div style="width:20px;height:20px;background-color:#c8c8c8"/>| A 3D cone defined by a height, an upper radius, and a lower radius. For more information on this type and its properties, see [Cone](Type-Cone.md). |
| **ArcCone**     | <div style="width:20px;height:20px;background-color:#c8c8c8"/>| A solid-arc of 3D Cone defined by an angle and a [Cone](Type-Cone.md). For more information on this type and its properties, see [ArcCone](Type-ArcCone.md). |
| **Line**        | <div style="width:20px;height:20px;background-color:#c8c8c8"/>| A line defined by two positions. For more information on this type and its properties, see [Line](Type-Line.md). |
| **Transform**   | <div style="width:20px;height:20px;background-color:#c8c8c8"/>| A translation, rotation, and scaling component defined by a position, a Euler angle (expressed in degrees), and a scale. For more information on this type and its properties, see [Transform](Type-Transform.md). |


### Other types

| **Type**        | **Color**                                                     | **Description**                                              |
| --------------- | ------------------------------------------------------------- | ------------------------------------------------------------ |
| **TerrainType** | <div style="width:20px;height:20px;background-color:#c8c8c8"/> | A Unity Terrain defined by a set of bounds, a heightmap, and a height. For more information on this type and its properties, see [TerrainType](Type-TerrainType.md). |
| **Camera**      | <div style="width:20px;height:20px;background-color:#c8c8c8"/> | A Unity Camera defined by a Transform, field of view, near-plane, far-plane, aspect ratio, resolution. You can also access the color and depth buffer. For more information on this type and its properties, see [Camera](Type-Camera.md). |
| **Flipbook**    | <div style="width:20px;height:20px;background-color:#9480e6"/> | A type that returns horizontal and vertical (signed integers) sizes of a flipbook. |

