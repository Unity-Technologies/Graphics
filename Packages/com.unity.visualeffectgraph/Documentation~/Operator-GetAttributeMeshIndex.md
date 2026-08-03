# Get Attribute: meshIndex

Menu Path : **Operator** > **Attribute** > **Get Mesh Index**

The **Get Mesh Index** operator returns the index of the mesh a particle will use to render. This is only relevant when the **Mesh Count** setting on an Output Particle Mesh context is set higher than 1.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator settings

| **Setting** | **Type** | **Description** |
|---|---|---|
| **Location** | [Enum](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.6/manual/Snippets/Attributes.md#attribute-locations) | The location of the attribute. The options are:<br>• **Current**: Gets the value of the attribute from the current system data container. For example, particle data from a Particle System.<br>• **Source**: Gets the value of the attribute from the previous system data container read from. You can only read from this **Location** in the first Context of a system after a system data change. For example, in an Initialize Particle Context. |

## Operator properties

| **Output** | **Type** | **Description** |
| ---------- | -------- | ------------------------------------------------------------ |
| Mesh Index  | uint     | The value of the meshIndex attribute, which represents the index of the mesh to use, based on **Location**. If this attribute has not been written to, this operator returns 0.|

## Details

The value the operator returns uses the system's space, either local space or world space.
