# VFX type reference

This page references all the types the Visual Effect Graph uses. Some of these are standard C# and Unity types whereas others are unique to the Visual Effect Graph.

## Base types

Base types refer to standard C# and Unity types that you can use to store basic data.

### Attribute-compatible types

Attribute-Compatible Types are types that you can use for attribute storage in Systems or SpawnEvent payloads.

| **Type**    | **Description**                                              |
| ----------- | ------------------------------------------------------------ |
| **float**   | A 32-bit floating-point value.                               |
| **int**     | A 32-bit signed integer value.                               |
| **uint**    | A 32-bit unsigned integer value.                             |
| **bool**    | A 1-bit boolean value.                                       |
| **Vector2** | A two-dimensional 32 bit Floating Point Vector.              |
| **Vector3** | A three-dimensional 32 bit Floating Point Vector.            |
| **Vector3** | A four-Dimensional 32 bit Floating Point Vector.             |
| **Color**   | A four-component (Red, Green, Blue, Alpha) 32-bit floating-point linear Color. |

### Other base types

The following base types are not attribute-compatible which means you can not use them as attributes for particles, but you can set them on the Visual Effect component if they are exposed properties.

| **Type**             | **Description**                                              |
| -------------------- | ------------------------------------------------------------ |
| **Gradient**         | Unity's [Gradient](https://docs.unity3d.com/ScriptReference/Gradient.html) type. |
| **AnimationCurve**   | Unity's [AnimationCurve](https://docs.unity3d.com/ScriptReference/AnimationCurve.html) type. |
| **Texture2D**        | Unity's [Texture2D](https://docs.unity3d.com/ScriptReference/Texture2D.html) type. |
| **Texture3D**        | Unity's [Texture3D](https://docs.unity3d.com/ScriptReference/Texture3D.html) type. |
| **TextureCube**      | Unity's [CubeMap](https://docs.unity3d.com/ScriptReference/Cubemap.html) type. |
| **Texture2DArray**   | Unity's [Texture2DArray](https://docs.unity3d.com/ScriptReference/Texture2DArray.html) type. |
| **TextureCubeArray** | Unity's [CubeMapArray](https://docs.unity3d.com/ScriptReference/CubemapArray.html) type. |
| **Mesh**             | Unity's [Mesh](https://docs.unity3d.com/ScriptReference/Mesh.html) type. |

## Advanced types

This section describes the advanced types that the Visual Effect Graph includes. These are either advanced versions of the base types or composite types (made up of multiple properties).

### Spaceable base types

Spaceable base types are Vector types that embed a space alongside their value. They also use a vector semantic when performing space transformations.

| **Type**      | **Description**                                              |
| ------------- | ------------------------------------------------------------ |
| **Position**  | A world-space or local-space three-component position vector. For more information on this type and its properties, see [Position](Type-Position.md). |
| **Vector**    | A world-space or local-space three-component vector. For more information on this type and its properties, see [Vector](Type-Vector.md). |
| **Direction** | A world-space or local-space three-component normalized direction. Values of this type are always normalized when you retrieve them. For more information on this type and its properties, see [Direction](Type-Direction.md). |

### Shape types

Shape Types are Advanced types that define a shape based on a composition on base types.

| **Type**        | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| **Sphere**      | A sphere defined by a center position and a radius. For more information on this type and its properties, see [Sphere](Type-Sphere.md). |
| **ArcSphere**   | A solid-arc of a sphere, defined by an angle and a [Sphere](Type-Sphere.md). For more information on this type and its properties, see [ArcSphere](Type-ArcSphere.md). |
| **AABox**       | An axis-aligned 3D Box, defined by a center and a 3D size. For more information on this type and its properties, see [AABox](Type-AAbox.md). |
| **Plane**       | A 3D infinite plane, defined by a position and a normal vector. For more information on this type and its properties, see [Plane](Type-Plane.md). |
| **OrientedBox** | An oriented box defined by a position, a Euler angle (in degrees), and a scale. For more information on this type and its properties, see [OrientedBox](Type-OrientedBox.md). |
| **Circle**      | A 3D circle oriented on the XY plane, defined by a position and a radius. For more information on this type and its properties, see [Circle](Type-Circle.md). |
| **ArcCircle**   | A solid-arc of a circle, defined by an angle and a [Circle](Type-Circle.md). For more information on this type and its properties, see [ArcCircle](Type-ArcCircle.md). |
| **Torus**       | A 3D torus oriented on the XY plane, defined by a position, a major radius (torus radius), and a minor radius (torus thickness). For more information on this type and its properties, see [Torus](Type-Torus.md). |
| **ArcTorus**    | A solid-arc of a torus defined by an angle and a 3D [Torus](Type-Torus.md). For more information on this type and its properties, see [ArcTorus](Type-ArcTorus.md). |
| **Cone**        | A 3D cone defined by a height, an upper radius, and a lower radius. For more information on this type and its properties, see [Cone](Type-Cone.md). |
| **ArcCone**     | A solid-arc of 3D Cone defined by an angle and a [Cone](Type-Cone.md). For more information on this type and its properties, see [ArcCone](Type-ArcCone.md). |
| **Line**        | A line defined by two positions. For more information on this type and its properties, see [Line](Type-Line.md). |
| **Transform**   | A translation, rotation, and scaling component defined by a position, a Euler angle (expressed in degrees), and a scale. For more information on this type and its properties, see [Transform](Type-Transform.md). |

### Other types

| **Type**        | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| **TerrainType** | A Unity Terrain defined by a set of bounds, a heightmap, and a height. For more information on this type and its properties, see [TerrainType](Type-TerrainType.md). |
| **Camera**      | A Unity Camera defined by a Transform, field of view, near-plane, far-plane, aspect ratio, resolution. You can also access the color and depth buffer. For more information on this type and its properties, see [Camera](Type-Camera.md). |
