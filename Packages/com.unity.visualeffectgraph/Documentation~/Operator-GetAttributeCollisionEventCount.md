# Get Attribute: collisionEventCount

Menu Path : **Operator** > **Attribute** > **Get Collision Event Count**

The **Get Collision Event Count** operator outputs the number of times a particle has hit a surface since it was spawned.

To write to the **Get Collision Event Count** attribute, add a [Collision Shape Block](Block-CollisionShape.md) to your graph and set its **Collision Attributes** property to **Write Punctual Content Only** or **Write Always**.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator settings

| **Setting** | **Type** | **Description** |
|---|---|---|
| **Location** | [Enum](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.6/manual/Snippets/Attributes.md#attribute-locations) | The location of the attribute. The options are:<br>• **Current**: Gets the value of the attribute from the current system data container. For example, particle data from a Particle System.<br>• **Source**: Gets the value of the attribute from the previous system data container read from. You can only read from this **Location** in the first Context of a system after a system data change. For example, in an Initialize Particle Context. |


## Operator properties

| **Output**          | **Type** | **Description**                                              |
| -------------------- | -------- | ------------------------------------------------------------ |
| Collision Event Count | uint     | The value of the collisionEventCount attribute, which is the number of times the particle has hit a surface, based on **Location**. If the particle hasn't collided with any surfaces, the operator returns the default attribute value. |



## Details

The value the operator returns uses the system’s space, either local space or world space.
