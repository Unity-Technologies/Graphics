# Get Attribute: oldVelocity

Menu Path : **Operator** > **Attribute** > **Get Old Velocity**

The **Get Old Velocity** operator returns the velocity of a particle at the beginning of the context, before VFX Graph applies any forces.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator settings

| **Setting** | **Type** | **Description** |
|---|---|---|
| **Location** | [Enum](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.6/manual/Snippets/Attributes.md#attribute-locations) | The location of the attribute. The options are:<br>• **Current**: Gets the value of the attribute from the current system data container. For example, particle data from a Particle System.<br>• **Source**: Gets the value of the attribute from the previous system data container read from. You can only read from this **Location** in the first Context of a system after a system data change. For example, in an Initialize Particle Context. |

## Operator properties

| **Output**  | **Type** | **Description** |
| ----------- | -------- | ------------------------------------------------------------ |
| Old Velocity | Vector3  | The value of the oldVelocity attribute, which represents the velocity of the particle at the beginning of the context, before any force integration, based on **Location**. If this attribute has not been written to, this operator returns 0. |

## Details

The value the operator returns uses the system's space, either local space or world space.
