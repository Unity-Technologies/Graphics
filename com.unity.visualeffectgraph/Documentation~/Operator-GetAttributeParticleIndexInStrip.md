# Get Attribute: particleIndexInStrip

Menu Path : **Operator > Attribute > Get Attribute: particleIndexInStrip**

The **Get Attribute: particleIndexInStrip** returns the particleIndexInStrip, which is a [standard attribute](Reference-Attributes.md), of a simulated element depending on [Location](Attributes.md#attribute-locations). This Operator outputs the index of the simulated element in the particle string ring buffer.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator properties

| **Output**           | **Type** | **Description**                                              |
| -------------------- | -------- | ------------------------------------------------------------ |
| particleIndexInStrip | uint     | The value of the particleIndexInStrip attribute, based on **Location**.<br/>If this attribute has not been written to, this Operator returns the default attribute value. |

## Details

The value the attribute returns uses the system’s space (either local-space or world-space).
