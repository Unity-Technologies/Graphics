

# Sample Buffer

**Menu Path : Operator > Sampling > Sample Buffer**

The Sample Buffer Operator allows you to fetch a structured buffer

## Operator settings

| **Input** | **Type** | **Description**                                              |
| --------- | -------- | ------------------------------------------------------------ |
| **Mode**  | Enum     | The wrap mode to use for the sequence. The options are:<br/>&#8226; **Clamp**: Clamps the index between the first and last vertices.<br/>&#8226; **Wrap**: Wraps the index around to the other side of the vertex list. <br/>&#8226; **Mirror**: Mirrors the vertex list so out of range indices move back and forth through the list. |

### Operator Properties

| **Input**  | **Type**                                | **Description**                                              |
| ---------- | --------------------------------------- | ------------------------------------------------------------ |
| **Input**  | [Configurable](#operator-configuration) | The structure type                                           |
| **Buffer** | GraphicsBuffer                          | The graphics buffer to fetch. This value must be linked to an exposed property. |
| **Index**  | uint                                    | The index of the fetched element.                            |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **s**      | Dependent | The sampled structure at index considering the addressing mode. |

## Operator configuration

To view the Operator's configuration, click the **cog** icon in the Operator's header.

### Available types

The type has to be declared in project code thanks to a structure with the *[VFXType]* attribute specifying *VFXTypeAttribute.Usage.GraphicsBuffer*.

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

#### Limitations

- The GraphicBuffer fetching is expecting a resource created with the target [structured](https://docs.unity3d.com/ScriptReference/GraphicsBuffer.Target.Structured.html)
- The stride of the GraphicsBuffer declaration must match with the structure stride.
- The structure must be blittable (e.g.: This structure can't store a reference to a Texture2D but it can store a reference to another blittable structure).
- The supported type stored in the structured are blittable public type handled by the VFX :
  - **bool**
  - **float**
  - **int**
  - **uint**
  - **Vector2**
  - **Vector3**
  - **Vector4**
  - **Matrix4x4**
  - **Another custom structure declared with [VFXType]**
