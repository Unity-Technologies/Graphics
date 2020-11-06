# Get Attribute: lifetime

Menu Path : **Operator > Attribute > Get Attribute: lifetime**

The **Get Attribute: lifetime** returns the lifetime, which is a [standard attribute](Reference-Attributes.md), of a simulated element depending on [Location](Attributes.md#attribute-locations). This Operator outputs the amount of time the simulated elements should live for.<

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator properties

| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| lifetime   | float    | The value of the lifetime attribute, based on **Location**.<br/>If this attribute has not been written to, this Operator returns the default attribute value. |

## Details

The value the attribute returns uses the system’s space (either local-space or world-space).

