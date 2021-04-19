# Get Attribute: velocity

Menu Path : **Operator > Attribute > Get Attribute: velocity**

The **Get Attribute: velocity** returns the velocity, which is a [standard attribute](Reference-Attributes.md), of a simulated element depending on [Location](Attributes.md#attribute-locations). This Operator outputs the current velocity of the simulated element, in the simulation space of the [System](Systems.md).

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator properties

| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| velocity   | Vector3  | The value of the velocity attribute, based on **Location**.<br/>If this attribute has not been written to, this Operator returns the default attribute value. |

## Details

The value the attribute returns uses the system’s space (either local-space or world-space).