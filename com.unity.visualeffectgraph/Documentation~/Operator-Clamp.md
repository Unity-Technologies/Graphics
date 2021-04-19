# Clamp

Menu Path : **Operator > Math > Clamp > Clamp**  

The **Clamp** Operator limits an input value between a lower and upper bound. 

If the input value is greater than **Max**, this Operator returns **Max**. If the input value is less than **Min**, this Operator returns **Min**. If the Input value is between **Max** and **Min**, the result is the same as the input.

This Operator accepts input values of various types. For the list of types this Operator can use, see [Available Types](#available-types). **Min** and **Max** input can be either the same type as **Input** or a float.

## Operator properties

| **Input** | **Type**                                | **Description**                                              |
| --------- | --------------------------------------- | ------------------------------------------------------------ |
| **Input** | [Configurable](#operator-configuration) | The value this Operator evaluates.                           |
| **Min**   | [Configurable](#operator-configuration) | The minimum value of the clamp.  An input of either float type or the same type as **Input**. |
| **Max**   | [Configurable](#operator-configuration) | The maximum value of the clamp. An input of either float type or the same type as **Input**. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent | The input restricted to a value between **Min** and **Max**.<br/>The **Type** changes to match the type of **Input**. |

## Operator configuration

To view the Operator’s configuration, click the **cog** icon in the Operator’s header. For input, you can choose a type beyond all [Available Types](#available-types). 



### Available types

You can use the following types for your **input** ports:

- **Float**
- **Int**
- **uint**
- **Vector**
- **Vector2**
- **Vector3**
- **Vector4**
- **Position**
- **Direction**