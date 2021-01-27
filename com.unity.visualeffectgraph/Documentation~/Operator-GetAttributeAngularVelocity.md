# Get Attribute: angularVelocity

Menu Path : **Operator > Attribute > Get Attribute: angularVelocity**

The **Get Attribute: angularVelocity** returns the angularVelocity, which is a [standard attribute](Reference-Attributes.md), of a simulated element depending on [Location](Attributes.md#attribute-locations). This Operator outputs the Euler rotation speed, in degrees per second, of the simulated element.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator properties

| **Output**      | **Type** | **Description**                                              |
| --------------- | -------- | ------------------------------------------------------------ |
| angularVelocity | Vector3  | The value of the angularVelocity attribute, based on **Location**.<br/>If this attribute has not been written to, this Operator returns the default attribute value. |

## Details

The value the attribute returns uses the system’s space (either local-space or world-space).
