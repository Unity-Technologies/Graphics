# Get Attribute: spawnIndex

Menu Path : **Operator > Attribute > Get Attribute: spawnIndex**

The **Get Attribute: spawnIndex** returns the spawnIndex, which is a [standard attribute](Reference-Attributes.md), of a simulated element depending on [Location](Attributes.md#attribute-locations). This Operator outputs the index of the particle when it spawned.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator properties

| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| spawnIndex | uint     | The value of the spawnIndex attribute, based on **Location**.<br/>If this attribute has not been written to, this Operator returns the default attribute value. |

## Details

The value the attribute returns uses the system’s space (either local-space or world-space).
