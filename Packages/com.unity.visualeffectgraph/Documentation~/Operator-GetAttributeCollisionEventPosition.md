# Get Attribute: collisionEventPosition

Menu Path : **Operator** > **Attribute** > **Get Collision Event Position**

The **Get Collision Event Position** operator returns the the coordinates of the point where the particle hit a surface during the current frame.

To write to the collisionEventPosition attribute, add a [Collision Shape Block](Block-CollisionShape.md) to your graph and set its **Collision Attributes** property to **Write Punctual Contact Only** or **Write Always**.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator settings

| **Setting** | **Type** | **Description** |
|---|---|---|
| **Location** | [Enum](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.6/manual/Snippets/Attributes.md#attribute-locations) | The location of the attribute. The options are:<br>• **Current**: Gets the value of the attribute from the current system data container. For example, particle data from a Particle System.<br>• **Source**: Gets the value of the attribute from the previous system data container read from. You can only read from this **Location** in the first Context of a system after a system data change. For example, in an Initialize Particle Context. |

## Operator properties

| **Output**              | **Type** | **Description**                                              |
| ------------------------- | -------- | ------------------------------------------------------------ |
| Collision Event Position | Vector3  | The value of the collisionEventPosition attribute, which represents the coordinates of the point of impact, based on **Location**. If the particle hasn't collided with any surfaces, this operator returns (0,0,0). |

## Details

The value the operator returns uses the system's space, either local space or world space.


