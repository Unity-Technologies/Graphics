# Get Attribute: spawnTime

Menu Path : **Operator > Attribute > Get Attribute: spawnTime**

The **Get Attribute: spawnTime** returns the spawnTime, which is a [standard attribute](Reference-Attributes.md), of a simulated element depending on [Location](Attributes.md#attribute-locations). This Operator outputs the internal time that the Spawn Context spawned the simulated element.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator properties

| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| spawnTime  | float    | The value of the spawnTime attribute, based on **Location**.<br/>If this attribute has not been written to, this Operator returns the default attribute value. |

## Details

The value the attribute returns uses the system’s space (either local-space or world-space).