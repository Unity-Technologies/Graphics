# Get Attribute: particleCountInStrip Operator

Menu Path : **Operator** > **Attribute** > **Get Particle Count In Strip**

The **Get Particle Count In Strip** operator returns the current number of particles in the particle strip the particle belongs to.

This operator can return wrong values if you use it in the Initialize context, if the strip index property is not constant.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator settings

| **Setting** | **Type** | **Description** |
|---|---|---|
| **Location** | [Enum](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.6/manual/Snippets/Attributes.md#attribute-locations) | The location of the attribute. The options are:<br>• **Current**: Gets the value of the attribute from the current system data container. For example, particle data from a Particle System.<br>• **Source**: Gets the value of the attribute from the previous system data container read from. You can only read from this **Location** in the first Context of a system after a system data change. For example, in an Initialize Particle Context. |

## Operator properties

| **Output**           | **Type** | **Description**                                              |
| -------------------- | -------- | ------------------------------------------------------------ |
| Particle Count In Strip | int      | The value of the particleCountInStrip attribute, which is the current number of particles in the particle strip the particle belongs to, based on **Location**.<br/>If this attribute has not been written to, this operator returns the default attribute value. |

## Details

The value the operator returns uses the system’s space, either local space or world space.
