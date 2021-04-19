# Get Attribute: targetPosition

Menu Path : **Operator > Attribute > Get Attribute: targetPosition**

The **Get Attribute: targetPosition** returns the targetPosition, which is a [standard attribute](Reference-Attributes.md), of a simulated element depending on [Location](Attributes.md#attribute-locations). This targetPosition attribute has multiple purposes. For example, you can use it as a storage helper if you want to store a position to reach, or, for a Line Renderer, use it as the end of each line particle.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator properties

| **Output**     | **Type** | **Description**                                              |
| -------------- | -------- | ------------------------------------------------------------ |
| targetPosition | Vector3  | The value of the targetPosition attribute, based on **Location**.<br/>If this attribute has not been written to, this Operator returns the default attribute value. |

## Details

The value the attribute returns uses the system’s space (either local-space or world-space).