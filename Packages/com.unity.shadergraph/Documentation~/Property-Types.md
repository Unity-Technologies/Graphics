# Property Types

## Description

**Property Types** are the types of [Property](https://docs.unity3d.com/Manual/SL-Properties.html) than can be defined on the [Blackboard](Blackboard.md) for use in the **Graph**. These [Properties](https://docs.unity3d.com/Manual/SL-Properties.html) are exposed to the [Inspector](https://docs.unity3d.com/Manual/UsingTheInspector.html) for [Materials](https://docs.unity3d.com/Manual/class-Material.html) that use the shader.

Each property has an associated **Data Type**. See [Data Types](Data-Types.md) for more information.

## Common Parameters

All properties have the following common parameters in addition to those specific to their [Data Types](Data-Types.md).

| Parameter | Description |
| :--- | :--- |
| **Name** | The display name of the property. |
| **Reference** | The internal name for the property in the shader. Use this **Reference** name instead of the display **Name** when you reference the property in a script.<br /><br />If you overwrite this parameter, be aware of the following:<ul><li>If the string doesn't begin with an underscore, Unity automatically adds one.</li><li>If the string contains any characters that HLSL does not support, Unity removes them.</li><li>You can revert to the default value: right-click on the **Reference** field label, and select **Reset Reference**.</li></ul> |
| **Precision** | Sets the data precision mode of the Property. The options are **Inherit**, **Single**, **Half**, and **Use Graph Precision**.<br />For more details, refer to [Precision Modes](Precision-Modes.md). |
| **Scope** | Specifies where you expect to edit the property for materials. The options are:<ul><li>**Global**: Makes the property editable at a global level, through a C# script only, for all materials that use it. Selecting this option hides or grays out all parameters that relate to the Inspector UI display.</li><li>**Per Material**: Makes the property independently editable per material, either through a C# script, or in the Inspector UI if you enable **Show In Inspector**.</li><li>**Hybrid Per Instance**: Has the same effect as **Per Material**, unless you're using [DOTS instancing](https://docs.unity3d.com/Packages/com.unity.entities.graphics@latest/index.html?subfolder=/manual/dots-instancing-shader.html).</li></ul> |
| **Show In Inspector** | Displays the property in the material inspector.<br/>If you disable this option, it includes an `[HideInInspector]` attribute to the material property (refer to [Properties block reference in ShaderLab](https://docs.unity3d.com/Manual/SL-Properties.html#material-property-attributes) for more details). |

## Float

Defines a **Float** value.

Parameters specific to Float properties in addition to the [common parameters](#common-parameters):

| Parameter | Description |
| :--- | :--- |
| **Mode** | Select the UI mode in which you want to display the Property and manipulate its value in the material inspector. You need to define a specific subset of parameters according to the option you select.<br /><br />The options are:<ul><li>**Default**: Displays a scalar input field in the material inspector. Only requires a **Default Value**.</li><li>**Slider**: Defines the Float property in [`Range`](https://docs.unity3d.com/Manual/SL-Properties.html#material-property-declaration-syntax-by-type) mode to display a slider field in the material inspector. Use [additional parameters](#slider) to define the slider range.</li><li>**Integer**: Displays an integer input field in the material inspector. Only requires a **Default Value**.</li></ul> |
| **Default Value** | The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). <br />The value might be either a float or an integer according to the **Mode** you select. |

### Slider

Additional parameters available when you set the Float property **Mode** to **Slider**.

| Parameter | Description |
| :--- | :--- |
| **Min** | The minimum value of the slider range. |
| **Max** | The maximum value of the slider range. |

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

Defines a **Color** value.  If the Property Inspector displays **Main Color**, this is the [Main Color](https://docs.unity3d.com/Manual/SL-Properties.html) for the shader. To select or deselect this node as the **Main Color**, right-click it in the graph or Blackboard and select **Set as Main Color** or **Clear Main Color**. Corresponds to the [`MainColor`](https://docs.unity3d.com/Manual/SL-Properties.html) ShaderLab Properties attribute.

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

Defines a [Texture 2D](https://docs.unity3d.com/Manual/class-TextureImporter.html) value. Displays an object field of type [Texture](https://docs.unity3d.com/Manual/class-TextureImporter.html) in the material inspector. If the Property Inspector displays **Main Texture**, this is the `Main Texture` for the shader. To select or deselect this node as the `Main Texture`, right-click on it in the graph or Blackboard and select **Set as Main Texture** or **Clear Main Texture**. Corresponds to the [`MainTexture`](https://docs.unity3d.com/Manual/SL-Properties.html) ShaderLab Properties attribute.

| Data Type    | Modes |
|:-------------|:------|
| Texture | White, Black, Grey, Bump |

| Field        | Type  | Description |
|:-------------|:------|:------------|
| Default | Texture | The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). |
| Use Tiling and Offset | Boolean | When set to false, activates the property [NoScaleOffset](https://docs.unity3d.com/Manual/SL-Properties.html), to enable manipulation of scale and offset separately from other texture properties. See [SplitTextureTransformNode](Split-Texture-Transform-Node.md).|

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

Defines a **Boolean** value. Displays a **ToggleUI** field in the material inspector. Note that internally to the shader this value is a **Float**. The **Boolean** type in Shader Graph is merely for usability.

| Data Type    | Modes |
|:-------------|:------|
| Boolean |  |

| Field        | Type  | Description |
|:-------------|:------|:------------|
| Default | Boolean | The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). |

## Matrix 2x2

Defines a Matrix 2. Matrices do not display in the **Inspector** window of the material.

| Field   | Type     |
|:--------|:---------|
| Default | Matrix 2 |

## Matrix 3x3

Defines a Matrix 3 value. Can't be displayed in the material inspector.

| Field   | Type     |
|:--------|:---------|
| Default | Matrix 3 |

## Matrix 4x4

Defines a Matrix 4 value. Can't be displayed in the material inspector.

| Field   | Type     |
|:--------|:---------|
| Default | Matrix 4 |
