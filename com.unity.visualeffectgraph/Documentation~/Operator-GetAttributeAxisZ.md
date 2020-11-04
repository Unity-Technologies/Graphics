# Get Attribute: axisZ

Menu Path : **Operator > Attribute > Get Attribute: axisZ**

The **Get Attribute: axisZ** returns the axisZ, which is a [standard attribute](Reference-Attributes.md), of a simulated element depending on [Location](Attributes.md#attribute-locations). This Operator outputs the forward axis of the simulated element.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator properties

| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| axisZ      | Vector3  | The value of the axisZ attribute, based on **Location**.<br/>If this attribute has not been written to, this Operator returns the default attribute value. |

## Details

The value the attribute returns uses the system’s space (either local-space or world-space).