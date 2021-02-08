# Set Attribute

Menu Path : **Attribute > Set > [Add/Blend/Inherit/Multiply/Set] \<Attribute> **

The **Set Attribute** Block is a generic block that allows you to write values to an attribute using composition.

You can either set the value of the attribute directly, or use two different random modes, **Uniform** and **Per-Component**, to set the attribute to a random value.

- **Uniform** calculates a single random number in range 0 to 1 then, to produce the final value, uses the random value to interpolate between the two range values (**A** and **B**).

- **Per-Component** calculates a random number in range 0 to 1 for each component in the attribute type then, to produce the final value, uses each random value to interpolate between each component of the two range values (**A** and **B**).

![](Images/Block-SetAttributeExample.gif)


You can also use this Block to inherit a source attribute and set it directly to the current attribute. For instance, you can inherit the position of a parent particle, and set it to particles generated from a GPU Event. If you set the **Source** setting to **Source**, the Block does not display any input properties for the attribute and instead uses the [source attribute](Attributes.md#attribute-locations) value (or the default source attribute value if the attribute is not present).

## Block compatibility

This Block is compatible with the following Contexts:

- [Initialize](Context-Initialize.md)
- [Update](Context-Update.md)
- Any output Context

## Block settings

| **Setting**     | **Type**                   | **Description**                                              |
| --------------- | -------------------------- | ------------------------------------------------------------ |
| **Attribute**   | Attribute (Inspector Only) | **(Inspector)** Specifies the attribute to write to.         |
| **Composition** | Enum (Inspector Only)      | **(Inspector)** Specifies how this Block composes the attribute. The options are:<br/>&#8226; **Set**: Overwrites the position attribute with the new value.<br/>&#8226; **Add**: Adds the new value to the position attribute value.<br/>&#8226; **Multiply**: Multiplies the position attribute value by the new value.<br/>&#8226; **Blend**: Interpolates between the position attribute value and the new value. You can specify the blend factor between 0 and 1. |
| **Source**      | Enum (Inspector Only)      | **(Inspector)** Specifies the source of the attribute. The options are:<br/>&#8226; **Slot**: Calculates the value from the Block’s input property ports.<br/>&#8226; **Source**: Takes the value from the source attribute of the same name. |
| **Random**      | Enum                       | **(Inspector)** Specifies how the Block calculates the value to compose to the attribute. The options are:<br/>&#8226; **Off** : Does not calculate a random value for the attribute. Uses the value you provide in the input directly.<br/>&#8226; **PerComponent**: Calculates a random value for each of the attribute's components.<br/>&#8226; **Uniform** : Calculates a single random value and uses it for each of the attribute's components. |
| **Channels**    | Enum                       | Specifies which channels of the attribute to affect. This Operator has no effect on channels you do not specify with this setting. This setting is only visible if the **Attribute** you set is one with channels. |

##  Block properties

| **Input**        | **Type**                 | **Description**                                              |
| ---------------- | ------------------------ | ------------------------------------------------------------ |
| **\<Attribute>** | Depends on the attribute | The value to compose to the attribute.<br/>This property only appears if you set **Source** to **Slot** and **Random** to **Off**. |
| **A**            | Depends on the attribute | The first end of the random range the Block uses to calculate the value for the attribute.<br/>This property only appears if you set **Source** to **Slot** and **Random** to **PerComponent** or **Uniform**. |
| **B**            | Depends on the attribute | The other end of the random range the Block uses to calculate the value for the attribute.<br/>This property only appears if you set **Source** to **Slot** and **Random** to **PerComponent** or **Uniform**. |
| **Blend**        | Float (Range 0..1)       | The blend percentage between the current position attribute value and the newly calculated position value.<br/>This property only appears if you set **Composition** to **Blend**. |
