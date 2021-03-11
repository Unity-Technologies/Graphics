# Get Attribute: oldPosition

Menu Path : **Operator > Attribute > Get Attribute: oldPosition**

The **Get Attribute: oldPosition** returns the oldPosition, which is a [standard attribute](Reference-Attributes.md), of a simulated element depending on [Location](Attributes.md#attribute-locations). This oldPosition attribute is a helper which you can use the store the current position of a simulated element before you integrate the element's velocity.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator properties

| **Output**  | **Type** | **Description**                                              |
| ----------- | -------- | ------------------------------------------------------------ |
| oldPosition | Vector3  | The value of the oldPosition attribute, based on **Location**.<br/>If this attribute has not been written to, this Operator returns the default attribute value. |

## Details

The value the attribute returns uses the system’s space (either local-space or world-space).
