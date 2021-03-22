# Sample Signed Distance Field

**Menu Path : Operator > Sampling > Sample Signed Distance Field**

The Sample Signed Distance Field Operator allows you to fetch a distance field stored in a Texture3D.

A Signed Distance Field (SDF) determines the distance from a point in space to the surface of a shape. By convention, this function is negative for points inside of the shape, and positive outside. The SDF is equal to zero on the surface of the object.

### Operator Properties

| **Input**       | **Type**                           | **Description**                                              |
| --------------- | ---------------------------------- | ------------------------------------------------------------ |
| **texture**     | Texture3D                          | The 3D texture that stores the SDF.                          |
| **position**    | [Position](Type-Position.md)       | The position to sample the SDF from.                         |
| **orientedBox** | [OrientedBox](Type-OrientedBox.md) | The oriented box that specifies the transformation to apply to the SDF. |
| **Level**       | float                              | The mipmap level.                                            |

| **Output**    | **Type** | **Description**                                              |
| ------------- | -------- | ------------------------------------------------------------ |
| **distance**  | float    | The signed distance from the **position** to the surface the SDF defines. This value is positive when **position** is outside of the shape and negative when **position** is inside the shape. |
| **direction** | Vector3  | The direction that points to the closest point on the surface the SDF defines. |

### Additional notes

You can set the position, orientation, and scale of the SDF using an [OrientedBox](Type-OrientedBox.md). The center of the OrientedBox corresponds to the center of the sdf.

#### Limitations

For this Operator to output correct distances in world coordinates, the dimensions (size) of the OrientedBox must match the dimensions of the box you used to bake the SDF. If you do not set this up correctly, distances from inside and outside of the texture bounds have a different scale,  which means the output does not exhibit the expected behavior.

Also, if you apply a non-uniform scale to the sdf (i.e. not proportional to the dimensions of the box you used to bake it), this results in distorted distances.
