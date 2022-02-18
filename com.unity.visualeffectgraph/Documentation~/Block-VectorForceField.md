# Vector Field Force

Menu Path : **Force > Vector Field Force**

The **Vector Field Force** Block uses [vector fields](VectorFields.md) to apply a force to the particles. This Block is useful for adding specific forces created in advance and stored in vector field assets.

![](Images/Block-VectorForceFieldExample.gif)
![](Images/Block-VectorForceFieldExample2D.gif)
2D view of a Vector Field

## Block compatibility

This Block is compatible with the following Contexts:

- [Update](Context-Update.md)

## Block settings

| **Setting**            | **Type** | **Description**                                              |
| ---------------------- | -------- | ------------------------------------------------------------ |
| **Data Encoding**      | Enum     | The encoding format for the vector field data. The options are:<br/>&#8226; **Signed**: This Block uses the data as is (typically for float formats).<br/>&#8226; **Unsigned Normalized**: Data is centered on gray and scaled/biased (typically for 8 bits per component formats). |
| **Mode**               | Enum     | The mode this Block uses to apply the force to the particles. The options are:<br/>&#8226; **Absolute**: Applies the force to the particle as an absolute value.<br/>&#8226; **Relative**: Applies the force relative to the particle's velocity. |
| **Closed Field**       | Bool     | (**Inspector**) Indicates whether the Block considers the field to be closed or not. If you enable the checkbox, the Block considers the field to be closed which means that it does not affect any particles outside of the **Field Transform**. If you disable this checkbox, the field wraps and the Block affects particles outside of the **Field Transform**.. |
| **Conserve Magnitude** | Bool     | (**Inspector**) Indicates whether the Block conserves the magnitude of the field when the size of the **Field Transform** changes. |

## Block properties

| **Input**           | **Type**                       | **Description**                                              |
| ------------------- | ------------------------------ | ------------------------------------------------------------ |
| **Vector Field**    | Texture3D                      | The vector field this Block uses to apply force to the particles. |
| **Field Transform** | [Transform](Type-Transform.md) | The transform with which to position, scale, or rotate the field. |
| **Intensity**       | Float                          | The intensity of the field. Higher values increase the particle velocity. |
| **Drag**            | Float                          | The drag coefficient. Higher drag leads to a stronger force influence over the particle velocity.<br/>This property only appears if you set **Mode** to **Relative**. |
