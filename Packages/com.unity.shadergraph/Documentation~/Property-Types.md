# Property Types

## Description

**Property Types** are the types of [Property](https://docs.unity3d.com/Manual/SL-Properties.html) than can be defined on the [Blackboard](Blackboard.md) for use in the **Graph**. These [Properties](https://docs.unity3d.com/Manual/SL-Properties.html) are exposed to the [Inspector](https://docs.unity3d.com/Manual/UsingTheInspector.html) for [Materials](https://docs.unity3d.com/Manual/class-Material.html) that use the shader.

Each property has an associated **Data Type**. See [Data Types](Data-Types.md) for more information.

## Common Parameters

All properties have the following common parameters in addition to those specific to their [Data Types](Data-Types.md).

| Parameter | Description |
| :--- | :--- |
| **Name** | The display name of the property. |
| **Reference** | The internal name for the property in the shader.<br /><br />If you overwrite this parameter, be aware of the following:<ul><li>If **Reference** doesn't begin with an underscore, Unity automatically adds one.</li><li>If **Reference** contains any characters which are unsupported in HLSL, Unity removes them.</li><li>You can revert to the default **Reference**: right-click on the **Reference** field label, and select **Reset Reference**.</li></ul> |
| **Show In Inspector** | Displays the property in the material inspector.<br/>If you disable this option, it includes an `[HideInInspector]` attribute to the material property (refer to [Properties block reference in ShaderLab](https://docs.unity3d.com/Manual/SL-Properties.html#material-property-attributes) for more details). |
| **Read Only** | Adds a [`PerRendererData`](https://docs.unity3d.com/ScriptReference/Rendering.ShaderPropertyFlags.html) attribute to the material property to display the value as read-only in the material inspector. |
| **Custom Attributes** | A list of entries that allow you to call custom functions you scripted to create additional [material property drawers](https://docs.unity3d.com/ScriptReference/MaterialPropertyDrawer.html), like static decorators or complex controls.<br/>The **Custom Material Property Drawers** sample, available in the Package Manager among other [Shader Graph samples](ShaderGraph-Samples.md), shows how to display a Vector2 as a min/max slider, for example.<br/><br/>**Note**: When you declare the custom functions in the script, make sure to suffix their names with `Drawer` or `Decorator`.<br/><br/>In the list, use **+** or **-** to add or remove entries. Each entry corresponds to a function call which requires the following parameters:<ul><li>**Name**: A shorthened version of the function name, without its `Drawer` or `Decorator` suffix.</li><li>**Value**: The input values for the function as the script expects them.</li></ul>**Note**: A property can only have one drawer at any given time. |

## Float

Defines a **Float** value.

Parameters specific to Float properties in addition to the [common parameters](#common-parameters):

| Parameter | Description |
| :--- | :--- |
| **Mode** | Select the UI mode in which you want to display the Property and manipulate its value in the material inspector. You need to define a specific subset of parameters according to the option you select.<br /><br />The options are:<ul><li>**Default**: Displays a scalar input field in the material inspector. Only requires a **Default Value**.</li><li>**Slider**: Defines the Float property in [`Range`](https://docs.unity3d.com/Manual/SL-Properties.html#material-property-declaration-syntax-by-type) mode to display a slider field in the material inspector. Use [additional parameters](#slider) to define the slider type.</li><li>**Integer**: Displays an integer input field in the material inspector. Only requires a **Default Value**.</li><li>**Enum**: Adds an [`Enum`](https://docs.unity3d.com/ScriptReference/MaterialPropertyDrawer.html) attribute to the Float property to display a drop-down with a list of specific values in the material inspector. Use [additional parameters](#enum) to define the enum type.</li></ul> |
| **Default Value** | The default value of the [Property](https://docs.unity3d.com/Manual/SL-Properties.html). <br />The value might be either a float or an integer according to the **Mode** and options you select. |

### Slider

Additional parameters available when you set the Float property **Mode** to **Slider**.

| Parameter | Description |
| :--- | :--- |
| **Slider Type** | Select the slider response type to apply when you move the slider to change the value in the material inspector.<br /><br />The options are:<ul><li>**Default**: Displays a slider with a linear response. The value responds linearly within the slider range.</li><li>**Power**: Adds a [`PowerSlider`](https://docs.unity3d.com/ScriptReference/MaterialPropertyDrawer.html) attribute to the Float property to display a slider with a non-linear response. The value responds exponentially within the slider range according to the specified **Power** value.</li><li>**Integer**: Adds an [`IntRange`](https://docs.unity3d.com/ScriptReference/MaterialPropertyDrawer.html) attribute to the Float property to display a slider with an integer value response. The value responds in integer steps within the slider range.</li></ul> |
| **Min** | The minimum value of the slider range. |
| **Max** | The maximum value of the slider range. |
| **Power** | The exponent to use for non-linear response between **Min** and **Max** when you set the **Slider Type** to **Power**. |

### Enum

Additional parameters available when you set the Float property **Mode** to **Enum**.

| Parameter | Description |
| :--- | :--- |
| **Enum Type** | Select the source type to use for the dropdown entries in the material inspector.<br /><br />The options are:<ul><li>**Explicit Values**: Use a list of **Entries** you directly specify in this interface.</li><li>**Type Reference**: Use a **C# Enum Type** reference that contains predefined entries.</li></ul> |
| **Entries** | The list of dropdown entries to define when you set **Enum Type** to **Explicit Values**.<br /><br />Use **+** or **-** to add or remove entries. You have to define each entry with the following parameters:<ul><li>**Name**: The entry name to display in the dropdown in the material inspector.</li><li>**Value**: The value to apply to the Float property when you select its **Name** in the dropdown in the material inspector.</li></ul>**Note**: The **Entries** option allows you to define up to 7 entries. If you need a dropdown with more entries, use the **Type Reference** option. |
| **C# Enum Type** | The existing Enum Type reference to use when you set **Enum Type** to **Type Reference**.<br />Specify the full path of the type with the namespace. For example, to get Unity's predefined blend mode values: `UnityEngine.Rendering.BlendMode`. |

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
