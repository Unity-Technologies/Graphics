# Set SpawnEvent \<Attribute>

Menu Path: **Spawn > Set SpawnEvent \<Attribute>**

The **Set SpawnEvent** Block modifies the content of attributes stored in the context [event attribute](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXSpawnerState-vfxEventAttribute.html).

## Block compatibility

This Block is compatible with the following Contexts:

- [Spawn](Context-Spawn.md)

## Block settings

| **Input**       | **Type** | **Description**                                              |
| --------------- | -------- | ------------------------------------------------------------ |
| **Attribute**   | Enum     | **(Inspector)** Specifies the attribute to set the value for. The list of options contains |
| **Random Mode** | Enum     | **(Inspector)** Determines whether and how the system randomises the value of the attribute. The options are:<br/>&#8226; **Off**: Does not calculate a random value for the attribute. Uses the value you provide in the input directly.<br/>&#8226; **Per Component**: Calculates a random value for each of the attribute's components.<br/>&#8226; **Uniform**: Calculates a single random value and uses it for all of the attribute's components. |


## Block properties

| **Input**             | **Type**                 | **Description**                 |
| --------------------- | ------------------------ | ------------------------------- |
| **\<Attribute name>** | Depends on the attribute | The value to set the attribute. |

## Remarks

The system automatically transfers event attributes between spawn events, however, to retrieve the spawn event in an Initialize Context, you have to use **Inherit Source Attribute** or the Get Attribute Operator with **Location** set to **Source**.

![](Images/Block-SetSpawnEventExample.gif)