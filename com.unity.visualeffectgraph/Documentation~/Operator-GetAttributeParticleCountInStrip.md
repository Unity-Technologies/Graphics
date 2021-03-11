# Get Attribute: particleCountInStrip

Menu Path : **Operator > Attribute > Get Attribute: particleCountInStrip**

The **Get Attribute: particleCountInStrip** returns the particleCountInStrip, which is a [standard attribute](Reference-Attributes.md), of a simulated element depending on [Location](Attributes.md#attribute-locations). This Operator outputs the current number of particles in the particle strip the current particle belongs to.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator properties

| **Output**           | **Type** | **Description**                                              |
| -------------------- | -------- | ------------------------------------------------------------ |
| particleCountInStrip | int      | The value of the particleCountInStrip attribute, based on **Location**.<br/>If this attribute has not been written to, this Operator returns the default attribute value. |

## Details

The value the attribute returns uses the system’s space (either local-space or world-space).
