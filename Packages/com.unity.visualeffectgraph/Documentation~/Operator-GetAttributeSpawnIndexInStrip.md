# Get Attribute: spawnIndexInStrip

Menu Path : **Operator** > **Attribute** > **Get Spawn Index In Strip**

The **Get Spawn Index In Strip** operator returns the spawn index of the particle in the particle strip the particle belongs to. 
Unlike the [particleIndexInStrip](Operator-GetAttributeParticleIndexInStrip.md) attribute, which is unique for each particle within a strip, this value can be the same for two or more particles in the same strip.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator settings

| **Setting** | **Type** | **Description** |
|---|---|---|
| **Location** | [Enum](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.6/manual/Snippets/Attributes.md#attribute-locations) | The location of the attribute. The options are:<br>• **Current**: Gets the value of the attribute from the current system data container. For example, particle data from a Particle System.<br>• **Source**: Gets the value of the attribute from the previous system data container read from. You can only read from this **Location** in the first Context of a system after a system data change. For example, in an Initialize Particle Context. |

## Operator properties

| **Output**        | **Type** | **Description** |
| ------------------ | -------- | ------------------------------------------------------------ |
| Spawn Index In Strip  | uint     | The value of the spawnIndexInStrip attribute, which is the spawn index of the particle in the particle strip the particle belongs to, based on **Location**. If this attribute has not been written to, this operator returns 0. |

## Details

The value the operator returns uses the system's space, either local space or world space.
