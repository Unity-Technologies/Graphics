# Exp

Menu Path : **Operator > Math > Exp**

The **Exp** Operator raises a base number to a specified power. It supports three common bases: Exp, Exp<sub>2</sub>, and Exp<sub>10</sub>.

Exp raises the number **E**, which is a mathematical constant approximately equal to 2.718281828459, to a specified power. Exponential functions with a base **E** are frequently used in mathematics and science.

Exp<sub>2</sub> raises the number **2** to a specified power. For example, Exp2(3) equals to *2\*2\*2*, which is 8. 

Exp<sub>10</sub> raises the number **10** to a specified power. For example, Exp10(3) equals to *10\*10\*10*, which is 1000.

## Operator settings

| **Property** | **Type** | **Description**                                              |
| ------------ | -------- | ------------------------------------------------------------ |
| **Base**     | Enum     | Specify the base for the Operator to use. The options are:<br/>&#8226; **Base 2**: Raises the number **2** to the power you specify in **X**.<br/>&#8226; **Base 10**: Raises the number **10** to the power you specify in **X**.<br/>&#8226; **Base E**: Raises **E** (2.718... ) to the power you specify in **X**. |

## Operator properties

| **Input** | **Type**                                | **Description**                 |
| --------- | --------------------------------------- | ------------------------------- |
| **X**     | [Configurable](#operator-configuration) | The power to raise the base to. |

| **Output** | **Type**  | **Description**                 |
| ---------- | --------- | ------------------------------- |
| **Out**    | Dependent | The base to the power of **X**. |

## Operator configuration

To view the Operator's configuration, click the **cog** icon in the Operator's header. Use the drop-down to select the type for the **X** port. For the list of types this property supports, see [Available types](#available-types).

### Available types

You can use the following types for your **input** ports:

* **float**
* **Position**
* **Vector**
* **Vector2**
* **Vector3**