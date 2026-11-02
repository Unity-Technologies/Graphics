# Get Attribute: spawnCount

Menu Path : **Operator** > **Attribute** > **Get Spawn Count**

The **Get Spawn Count** operator returns the number of particles that have been spawned in the current frame.
[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator settings

| **Setting** | **Type** | **Description** |
|---|---|---|
| **Location** | [Enum](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.6/manual/Snippets/Attributes.md#attribute-locations) | The location of the attribute. The options are:<br>• **Current**: Gets the value of the attribute from the current system data container. For example, in a Spawn Context.<br>• **Source**: Gets the value of the attribute from the previous system data container read from. You can only read from this **Location** in the first Context of a system after a system data change. For example, in an Initialize Particle Context. |

## Operator properties

| **Output** | **Type** | **Description** |
| ---------- | -------- | ------------------------------------------------------------ |
| Spawn Count | float    | The value of the spawnCount attribute, which represents the number of particles spawned in the current frame, based on **Location**. If this attribute has not been written to, this operator returns 0. |

## Details

The value the operator returns uses the system's space, either local space or world space.
