# Remap (Remap)

Menu Path : **Operator > Math > Remap > Remap**

The **Remap** Operator linearly remaps input values from an old range to a new range, with an optional clamp.

This Operator accepts input values of various types. You can compare the input to a minimum and maximum value of the same type, or compare it to a float value. For the list of types this Operator can use, see [Available Types](#available-types).

## Operator settings

| **Setting** | **Type** | **Description**                                              |
| ----------- | -------- | ------------------------------------------------------------ |
| **Clamp**   | bool     | Clamps the input value between **NewRangeMin** and **NewRangeMax**. |

## Operator properties

| **Input**       | **Type**                                | **Description**                                              |
| --------------- | --------------------------------------- | ------------------------------------------------------------ |
| **Input**       | [Configurable](#operator-configuration) | The value this Operator evaluates.                           |
| **OldRangeMin** | [Configurable](#operator-configuration) | The lower bound of the input range. If you enable **Clamp**, this value must be less than **OldRangeMax**<br/>The **Type** can be float or the same as Input. |
| **OldRangeMax** | [Configurable](#operator-configuration) | The upper bound of the input range. If you enable **Clamp**, this value must be greater than **OldRangeMin**.<br/>The **Type** can be float or the same as Input. |
| **NewRangeMin** | [Configurable](#operator-configuration) | The lower bound of the output range. If **Input** is the same as **OldRangeMin**, the Operator outputs this value.<br/>The **Type** can be float or the same as Input. |
| **NewRangeMax** | [Configurable](#operator-configuration) | The upper bound of the output range. If **Input** is the same as **OldRangeMax**, the Operator outputs this value.<br/>The **Type** can be float or the same as Input. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| Output     | Dependent | The remapped value.<br>The **Type** changes to match the **Input** type. |

## Operator configuration

To view the Operator’s configuration, click the **cog** icon in the Operator’s header.

| **Input**       | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| **Input**       | The value type for the **Input** port and Output value. For the list of types this property supports, see [Available types](#available-types). |
| **OldRangeMin** | The value type for the **OldRangeMin** port. For the list of types this property supports, see [Available types](#available-types). |
| **OldRangeMax** | The value type for the **OldRangeMax** port. For the list of types this property supports, see [Available types](#available-types). |
| **NewRangeMin** | The value type for the **NewRangeMin** port. For the list of types this property supports, see [Available types](#available-types). |
| **NewRangeMax** | The value type for the **NewRangeMax** port. For the list of types this property supports, see [Available types](#available-types). |



### Available types

You can use the following types for your **Input values** and **Output** ports:

- **Float**
- **Vector2**
- **Vector3**
- **Vector4**
- **Direction**
- **Position**
- **Vector**
