# Get Attribute: texIndex

Menu Path : **Operator > Attribute > Get Attribute: texIndex**

The **Get Attribute: texIndex** returns the texIndex, which is a [standard attribute](Reference-Attributes.md), of a simulated element depending on [Location](Attributes.md#attribute-locations). This Operator outputs the animation frame to use to sample flipbook UVs for the simulated element.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator properties

| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| texIndex   | float    | The value of the texIndex attribute, based on **Location**.<br/>If this attribute has not been written to, this Operator returns the default attribute value. |

## Details

The value the attribute returns uses the system’s space (either local-space or world-space).
