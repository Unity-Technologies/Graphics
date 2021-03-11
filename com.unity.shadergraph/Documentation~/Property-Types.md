# Property Types

## Description

**Property Types** are the types of [Property](https://docs.unity3d.com/Manual/SL-Properties.html) than can be defined on the [Blackboard](Blackboard.md) for use in the **Graph**. These [Properties](https://docs.unity3d.com/Manual/SL-Properties.html) will be exposed to the [Inspector](https://docs.unity3d.com/Manual/UsingTheInspector.html) for [Materials](https://docs.unity3d.com/Manual/class-Material.html) that use the shader.

Each property has an associated **Data Type**. See [Data Types](Data-Types.md) for more information.

## Common Parameters

In addition to values specific to their [Data Types](Data-Types.md), most properties have the following common parameters.

| Name        | Type  | Description |
|:------------ |:---|:---|
| Display Name | String | The display name of the property |
| Exposed | Boolean | If true this property will be exposed on the material inspector |
| Reference Name | String | The internal name used for the property inside the shader |
| Override Property Declaration | Boolean | An advanced option to enable explicit control of the shader declaration for this property |
| Shader Declaration | Enumeration | Controls the shader declaration of this property |

NOTE: If you overwrite the **Reference Name** parameter be aware of the following conditions:
- If your **Reference Name** does not begin with an underscore, one will be automatically appended.
- If your **Reference Name** contains any characters which are unsupported in HLSL they will be removed.
- You can revert to the default **Reference Name** by right clicking on it and selecting **Reset Reference**.

## Float

Defines a **Float** value.

| Data Type    | Modes |
|:-------------|:------|
| Float    | Default, Slider, Integer |

#### Default

Displays a scalar input field in the material inspector.

| Field        | Type  | Description |
|:-------------|:------|:------------|
| Default | Float    | The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). |

#### Slider

Displays a slider field in the material inspector.

| Field        | Type  | Description |
|:-------------|:------|:------------|
| Default | Float    |  The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). |
| Min | Float    | The minimum value of the slider. |
| Max | Float    | The maximum value of the slider. |

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

NOTE: In versions prior to 10.0, Shader Graph didn't correct HDR colors for the project colorspace. Version 10.0 corrected this behavior. HDR color properties that you created with older versions maintain the old behavior, but you can use the [Graph Inspector](Internal-Inspector.md) to upgrade them. To mimic the old behavior in a gamma space project, you can use the [Colorspace Conversion Node](Colorspace-Conversion-Node.md) to convert a new HDR **Color** property from **RGB** to **Linear** space.

## Texture 2D

Defines a [Texture 2D](https://docs.unity3d.com/Manual/class-TextureImporter.html) value. Displays an object field of type [Texture](https://docs.unity3d.com/Manual/class-TextureImporter.html) in the material inspector.

| Data Type    | Modes |
|:-------------|:------|
| Texture | White, Black, Grey, Bump |

| Field        | Type  | Description |
|:-------------|:------|:------------|
| Default | Texture | The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). |

## Texture 3D

Defines a [Texture 3D](https://docs.unity3d.com/Manual/class-TextureImporter.html) value. Displays an object field of type [Texture 3D](https://docs.unity3d.com/Manual/class-TextureImporter.html) in the material inspector.

| Data Type    | Modes |
|:-------------|:------|
| Texture |  |

| Field        | Type  | Description |
|:-------------|:------|:------------|
| Default | Texture | The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). |

## Texture 2D Array

Defines a [Texture 2D Array](https://docs.unity3d.com/Manual/class-TextureImporter.html) value. Displays an object field of type [Texture 2D Array](https://docs.unity3d.com/Manual/class-TextureImporter.html) in the material inspector.

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
<a name="virtual-texture"> </a>
## Virtual Texture

Defines a [Texture Stack](https://docs.unity3d.com/2020.1/Documentation/Manual/svt-use-in-shader-graph.html), which appears as object fields of type  [Texture](https://docs.unity3d.com/Manual/class-TextureImporter.html) in the Material Inspector. The number of fields correspond to the number of layers in the property.

| Data Type | Modes |
|:----------|-------|
| Virtual Texture | |

| Field        | Type  | Description |
|:-------------|:------|:------------|
| Default | Texture | The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). |

## Boolean

Defines a **Boolean** value. Displays a **ToggleUI** field in the material inspector. Note that internally to the shader this value is a **Float**. The **Boolean** type in [Shader Graph](Shader-Graph.md) is merely for usability.

| Data Type    | Modes |
|:-------------|:------|
| Boolean |  |

| Field        | Type  | Description |
|:-------------|:------|:------------|
| Default | Boolean | The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). |
