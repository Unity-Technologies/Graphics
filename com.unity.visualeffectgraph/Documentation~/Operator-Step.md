# Step

Menu Path : **Operator > Math > Arithmetic > Step**

The **Step** Operator compares an input value to a threshold and returns whether the input is above or below the threshold:

- If **X** is greater than the threshold, the result is **1**.
- If **X** is less than or equal to the threshold, the result is **0**.

For example, for a threshold of 0.5, an input of (0.3, 0.5, 1)  outputs (0, 0, 1).

The **Step** Operator accepts input values of various types. For the list of types this Operator can use, see [Available Types](#available-types). This Operator always returns a value in the same type as its input.

## Operator properties

| **Input**     | **Type**                                | **Description**                                          |
| ------------- | --------------------------------------- | -------------------------------------------------------- |
| **Value**     | [Configurable](#operator-configuration) | The value this Operator evaluates.                       |
| **Threshold** | [Configurable](#operator-configuration) | An input of either float type or the same type as **X**. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent | The value of the input subtracted from one.<br/>The **Type** changes to match the type of **Value**. |

## Operator configuration

To view the Operator's configuration, click the **cog** icon in the Operator's header. Use the drop-down to select the type for the **Value** and **Threshold** port. For the list of types these properties support, see [Available types](#available-types).



### Available types

You can use the following types for your **input** ports:

- **float**
- **Vector**
- **Vector2**
- **Vector3**
- **Vector4**
- **Position**
- **Direction**
