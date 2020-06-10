# Get Attribute: alpha

Menu Path : **Operator > Attribute > Get Attribute: alpha**

The **Get Attribute: alpha** returns the alpha, which is a [standard attribute](Reference-Attributes.md), of a simulated element depending on its [Location](Attributes.md#attribute-locations).

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator properties

| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| age        |          | The value of the alpha attribute, based on **Location**.<br/>If this attribute has not been written to, this Operator returns the default attribute value. |

## Notes

The value the attribute returns uses the system’s space (either local-space or world-space).