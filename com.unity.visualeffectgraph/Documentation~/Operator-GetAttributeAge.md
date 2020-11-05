# Get Attribute: age

Menu Path : **Operator > Attribute > Get Attribute: age**

The **Get Attribute: age** returns the age, which is a [standard attribute](Reference-Attributes.md), of a simulated element depending on [Location](Attributes.md#attribute-locations). This Operator outputs the time, in seconds, since the simulated element spawned.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator properties

| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| age        | float    | The value of the age attribute, based on **Location**.<br/>If this attribute has not been written to, this Operator returns the default attribute value, which is `0.0`. |

## Details

The value the attribute returns uses the system’s space (either local-space or world-space).