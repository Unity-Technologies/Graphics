# Get Attribute: stripIndex

Menu Path : **Operator > Attribute > Get Attribute: stripIndex**

The **Get Attribute: stripIndex** returns the stripIndex, which is a [standard attribute](Reference-Attributes.md), of a simulated element depending on [Location](Attributes.md#attribute-locations). This Operator outputs the index of the particle strip this particle belongs to.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator properties

| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| stripIndex | uint     | The value of the stripIndex attribute, based on **Location**.<br/>If this attribute has not been written to, this Operator returns the default attribute value. |

## Details

The value the attribute returns uses the system’s space (either local-space or world-space).