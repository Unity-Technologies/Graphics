## Description

**Property Types** are the types of [Property](https://docs.unity3d.com/Manual/SL-Properties.html) than can be defined on the [Blackboard](Blackboard.md) for use in the **Graph**. These [Properties](https://docs.unity3d.com/Manual/SL-Properties.html) will be exposed to the [Inspector](https://docs.unity3d.com/Manual/UsingTheInspector.html) for [Materials](https://docs.unity3d.com/Manual/class-Material.html) that use the shader.

Each property has an associated **Data Type**. See [Data Types](Data-Types.md) for more information.

## Vector 1

Defines a **Vector 1** value.

| Data Type    | Modes |
|:-------------|:------|
| Vector 1 | Default, Slider, Integer |

#### Default

Displays a scalar input field in the material inspector.

| Field        | Type  | Description |
|:-------------|:------|:------------|
| Default | Vector 1 | The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). |

#### Slider

Displays a slider field in the material inspector.

| Field        | Type  | Description |
|:-------------|:------|:------------|
| Default | Vector 1 |  The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). |
| Min | Vector 1 | The minimum value of the slider. |
| Max | Vector 1 | The maximum value of the slider. |

#### Integer

Displays an integer input field in the material inspector.

| Field        | Type  | Description |
|:-------------|:------|:------------|
| Default | Integer | The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). |

## Vector 2

Defines a **Vector 2** value. Displays a **Vector 4** input field in the material inspector, where the z and w components are not used.

| Data Type    | Modes |
|:-------------|:------|
| Vector 2 |  |

| Field        | Type  | Description |
|:-------------|:------|:------------|
| Default | Vector 2 | The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). |

## Vector 3

Defines a **Vector 3** value. Displays a **Vector 4** input field in the material inspector, where the w component is not used.

| Data Type    | Modes |
|:-------------|:------|
| Vector 3 |  |

| Field        | Type  | Description |
|:-------------|:------|:------------|
| Default | Vector 3 | The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). |

## Vector 4

Defines a **Vector 4** value. Displays a **Vector 4** input field in the material inspector.

| Data Type    | Modes |
|:-------------|:------|
| Vector 4 |  |

| Field        | Type  | Description |
|:-------------|:------|:------------|
| Default | Vector 4 | The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). |

## Color

Defines a **Color** value.

| Data Type    | Modes |
|:-------------|:------|
| Color | Default, HDR |

#### Default

Displays an sRGB color field in the material inspector.

| Field        | Type  | Description |
|:-------------|:------|:------------|
| Default | Vector 4 | The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). |

#### HDR

Displays an HDR color field in the material inspector.

| Field        | Type  | Description |
|:-------------|:------|:------------|
| Default | Vector 4 | The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). |

## Texture

Defines a [Texture](https://docs.unity3d.com/Manual/class-TextureImporter.html) value. Displays an object field of type [Texture](https://docs.unity3d.com/Manual/class-TextureImporter.html) in the material inspector.

| Data Type    | Modes |
|:-------------|:------|
| Texture |  |

| Field        | Type  | Description |
|:-------------|:------|:------------|
| Default | Texture | The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). |

## Cubemap

Defines a [Cubemap](https://docs.unity3d.com/Manual/class-Cubemap.html) value. Displays an object field of type [Texture](https://docs.unity3d.com/Manual/class-TextureImporter.html) in the material inspector.

| Data Type    | Modes |
|:-------------|:------|
| Cubemap |  |

| Field        | Type  | Description |
|:-------------|:------|:------------|
| Default | Cubemap | The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). |

## Boolean

Defines a **Boolean** value. Displays a **Toggle** field in the material inspector. Note that internally to the shader this value is a **Vector 1**. The **Boolean** type in [Shader Graph](Shader-Graph.md) is merely for usability. 

| Data Type    | Modes |
|:-------------|:------|
| Boolean |  |

| Field        | Type  | Description |
|:-------------|:------|:------------|
| Default | Boolean | The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). |