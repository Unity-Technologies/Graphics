# Smoothstep

Menu Path : **Operator > Math > Arithmetic > Smoothstep**

The **Smoothstep** Operator calculates the linear interpolation of a value between two border values with smoothing at the limits.

This Operator returns a value between **X** and **Y**. Where this value is between **X** and **Y** depends on the value of **S**:

- If **S** is greater than 1, the result is **Y**.
- If **S** is less than 0 the result is **X.**

- If **S** is between 0 and 1 then the result is a smooth transition between **X** and **Y**. The result = (Y - X) * ( 3S2 - 2S3 ) + **X**

This Operator accepts input values of various types. For the list of types this Operator can use, see [Available Types](#available-types). The **X** and **Y** input are always of the same type. **S** changes to be the same type as **X** and **Y**.

![](Images/Operator-SmoothstepDiagram.png)

## Operator properties

| **Input** | **Type**                                | **Description**                                              |
| --------- | --------------------------------------- | ------------------------------------------------------------ |
| **X**     | [Configurable](#operator-configuration) | The value to interpolate from.                               |
| **Y**     | [Configurable](#operator-configuration) | The value to interpolate to.                                 |
| **S**     | [Configurable](#operator-configuration) | A value for the interpolation. An input of either float type or the same type as **X**. |

| **Output** | **Type**    | **Description**                                              |
| ---------- | ----------- | ------------------------------------------------------------ |
| **Out**    | Output Port | The linear interpolation of **S** between **X** and **Y** with smoothing at the limits.<br/>The **Type** changes to match the type of **X** and **Y**. |

## Operator configuration

To view the **Smoothstep** Operator’s configuration, click the **cog** icon in the Operator’s header. **X** and **Y** must be the same type among [Available Types](#available-types). If **S** is a vector type, Unity calculates the interpolation value by value.



### Available types

You can use the following types for your **input** ports:

- **float**
- **Vector**
- **Vector2**
- **Vector3**
- **Vector4**
- **Position**
- **Direction**
