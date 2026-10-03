# Get Attribute: hasCollisionEvent

Menu Path : **Operator** > **Attribute** > **Get Has Collision Event**

The **Get Has Collision Event** operator returns `true` if a particle has hit a surface. This attribute is only available in the [Update](Context-Update.md) context.

To write to the hasCollisionEvent attribute, add a [Collision Shape Block](Block-CollisionShape.md) to your graph and set its **Collision Attributes** property to **Write Punctual Contact Only** or **Write Always**.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator settings

| **Setting** | **Type** | **Description** |
|---|---|---|
| **Location** | [Enum](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.6/manual/Snippets/Attributes.md#attribute-locations) | The location of the attribute. The options are:<br>• **Current**: Gets the value of the attribute from the current system data container. For example, particle data from a Particle System.<br>• **Source**: Gets the value of the attribute from the previous system data container read from. You can only read from this **Location** in the first Context of a system after a system data change. For example, in an Initialize Particle Context. |

## Operator properties

| **Output**          | **Type** | **Description**                                              |
| -------------------- | -------- | ------------------------------------------------------------ |
| Has Collision Event | bool     | The value of the hasCollisionEvent attribute, based on **Location**. Returns `true` if the particle has hit a surface, and `false` otherwise. If this attribute has not been written to, this Operator returns the default attribute value. |

## Details

You can only use the **Get Has Collision Event** operator in the same Context as the Collision Shape Block that writes to it.
