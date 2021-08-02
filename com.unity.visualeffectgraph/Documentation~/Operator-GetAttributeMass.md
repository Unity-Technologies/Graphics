# Get Attribute: mass

Menu Path : **Operator > Attribute > Get Attribute: mass**

The **Get Attribute: mass** returns the mass, which is a [standard attribute](Reference-Attributes.md), of a simulated element depending on [Location](Attributes.md#attribute-locations). This Operator outputs the mass of the particle in kg/dm<sup>3</sup>.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator properties

| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| mass       | float    | The value of the mass attribute, based on **Location**.<br/>If this attribute has not been written to, this Operator returns the default attribute value. |

## Details

The value the attribute returns uses the system’s space (either local-space or world-space).