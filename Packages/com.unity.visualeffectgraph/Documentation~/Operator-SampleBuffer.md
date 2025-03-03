

# Sample Graphics Buffer

**Menu Path : Operator > Sampling > Sample Graphics Buffer**

The Sample Graphics Buffer Operator enables you to fetch and sample a structured buffer. A structured buffer is a [GraphicsBuffer](https://docs.unity3d.com/ScriptReference/GraphicsBuffer.html) created using the [Structured](https://docs.unity3d.com/ScriptReference/GraphicsBuffer.Target.Structured.html) target.

## Operator settings

| **Input** | **Type** | **Description**                                              |
| --------- | -------- | ------------------------------------------------------------ |
| **Mode**  | Enum     | The wrap mode to use for the sequence. The options are:<br/>&#8226; **Clamp**: Clamps the index between the first and last vertices.<br/>&#8226; **Wrap**: Wraps the index around to the other side of the vertex list. <br/>&#8226; **Mirror**: Mirrors the vertex list so out of range indices move back and forth through the list. |

### Operator Properties

| **Input**  | **Type**                                | **Description**                                              |
| ---------- | --------------------------------------- | ------------------------------------------------------------ |
| **Input**  | [Configurable](#operator-configuration) | The structure type.                                          |
| **Buffer** | Graphics Buffer [property](Properties.md)| The structured buffer to fetch. You can only connect an exposed property to this input port. |
| **Index**  | uint                                    | The index of the element to fetch.                            |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **s**      | Dependent | The sampled structure at the index, taking into account the **Mode** setting. |

## Operator configuration

To set the struct type, select the cog icon. The available types are:

- float
- int
- uint
- Vector2
- Vector3
- Vector4
- Matrix4x4

### Add a custom type

To add a custom type to the list, add the `[VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]` attribute to a struct. For example, the following script adds a **MyCustomData** type:

```c#
using UnityEngine;
using UnityEngine.VFX;

[VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
struct MyCustomData
{
    public Vector3 myColor;
    public Vector3 myPosition;
}
```

After you set a custom type, open the **s** dropdown to display the properties of the struct and connect them to other Operators and [Contexts](Contexts.md).

## Limitations
The Operator has the following limitations:

- This Operator expects a GraphicsBuffer created using the [Structured](https://docs.unity3d.com/ScriptReference/GraphicsBuffer.Target.Structured.html) target.
- The stride of the GraphicsBuffer declaration must match with the structure stride.
- The structure must be blittable. This means the structure can't store a reference to a Texture2D, but it can store any other blittable structure.
- This Operator only supports structured buffers that use one of the blittable public types the Visual Effect Graph supports. For the list of available types, see [Available types](#available-types).
