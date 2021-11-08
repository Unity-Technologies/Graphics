

# Sample Buffer

**Menu Path : Operator > Sampling > Sample Buffer**

The Sample Buffer Operator enables you to fetch and sample a structured buffer. A structured buffer is a [GraphicsBuffer](https://docs.unity3d.com/ScriptReference/GraphicsBuffer.html) created using the [Structured](https://docs.unity3d.com/ScriptReference/GraphicsBuffer.Target.Structured.html) target.

## Operator settings

| **Input** | **Type** | **Description**                                              |
| --------- | -------- | ------------------------------------------------------------ |
| **Mode**  | Enum     | The wrap mode to use for the sequence. The options are:<br/>&#8226; **Clamp**: Clamps the index between the first and last vertices.<br/>&#8226; **Wrap**: Wraps the index around to the other side of the vertex list. <br/>&#8226; **Mirror**: Mirrors the vertex list so out of range indices move back and forth through the list. |

### Operator Properties

| **Input**  | **Type**                                | **Description**                                              |
| ---------- | --------------------------------------- | ------------------------------------------------------------ |
| **Input**  | [Configurable](#operator-configuration) | The structure type.                                          |
| **Buffer** | GraphicsBuffer                          | The structured buffer to fetch. You can only connect an exposed property to this input port. |
| **Index**  | uint                                    | The index of the element to fetch.                            |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **s**      | Dependent | The sampled structure at the index, taking into account the **Mode** setting. |

## Operator configuration

To view the Operator's configuration, click the **cog** icon in the Operator's header.

### Available types
This Operator supports sampling structured buffers that use blittable types. The list of built-in blittable types is:
  - float
  - int
  - uint
  - Vector2
  - Vector3
  - Vector4
  - Matrix4x4

You can also declare custom types. To do this, add the  `[VFXType]` attribute to a struct, and use the `VFXTypeAttribute.Usage.GraphicsBuffer` type. For example:

```c#
using UnityEngine;
using UnityEngine.VFX;

[VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
struct CustomData
{
    public Vector3 color;
    public Vector3 position;
}
```

## Limitations
The Operator has the following limitations:

- This Operator expects a GraphicsBuffer created using the [Structured](https://docs.unity3d.com/ScriptReference/GraphicsBuffer.Target.Structured.html) target.
- The stride of the GraphicsBuffer declaration must match with the structure stride.
- The structure must be blittable. This means the structure can't store a reference to a Texture2D, but it can store any other blittable structure.
- This Operator only supports structured buffers that use one of the blittable public types the Visual Effect Graph supports. For the list of available types, see [Available types](#available-types).
