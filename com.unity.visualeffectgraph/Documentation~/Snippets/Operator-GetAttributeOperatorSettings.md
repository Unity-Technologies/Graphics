## Operator settings

| **Setting**  | **Type**                                  | **Description**                                              |
| ------------ | ----------------------------------------- | ------------------------------------------------------------ |
| **Location** | [Enum](Attributes.md#attribute-locations) | The location of the attribute. The options are:<br/>&#8226; **Current**: Gets the value of the attribute from the current system data container. For example, particle data from a Particle System.<br/>&#8226; **Source**: Gets the value of the attribute from the previous system data container read from. You can only read from this **Location** in the first Context of a system after a system data change. For example, in an Initialize Particle Context. |