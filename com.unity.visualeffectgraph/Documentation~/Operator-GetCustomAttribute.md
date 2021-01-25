# Get Custom Attribute

Menu Path : **Operator > Attribute > Get Custom Attribute**

The **Get Custom Attribute** Operator returns the value of a named custom Attribute of a given type depending on its location.

## Operator settings

| **Setting**        | **Type**                                                     | **Description**                                              |
| ------------------ | ------------------------------------------------------------ | ------------------------------------------------------------ |
| **Attribute**      | string                                                       | (**Inspector**) The name of the custom attribute. This is case sensitive. |
| **Location**       | Location Enum                                                | The location of the attribute                                |
| **Attribute Type** | Enum ([Attribute-compatible base types](VisualEffectGraphTypeReference.md#attribute-compatible-types)) | (**Inspector**) The type to use for the attribute.           |

## Operator properties

| **Output**          | **Type**  | **Description**                                              |
| ------------------- | --------- | ------------------------------------------------------------ |
| **CustomAttribute** | Dependent | The value of the custom attribute (based on its Location). If the attribute has not yet been written to, this is the default value for the output's type.<br/>The type of this output matches the type you specify in **Attribute Type**. |

## Details

If the custom attribute this Operator reads from has not yet been written to, the output value is the default value for the output's type.
