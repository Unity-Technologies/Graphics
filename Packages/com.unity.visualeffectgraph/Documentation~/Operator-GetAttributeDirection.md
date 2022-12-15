# Get Attribute: direction

Menu Path : **Operator > Attribute > Get Attribute: direction**

The **Get Attribute: direction** returns the direction, which is a [standard attribute](Reference-Attributes.md), of a simulated element depending on [Location](Attributes.md#attribute-locations).  This Operator outputs the simulated element's direction based on its shape. This drives the initial direction in Set Velocity Blocks.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator properties

| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| direction  | Vector3  | The value of the direction attribute, based on **Location**. <br/>If this attribute has not been written to, this Operator returns the default attribute value. |

## Details

The value the attribute returns uses the system’s space (either local-space or world-space).
