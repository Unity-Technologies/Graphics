# Log

Menu Path : **Operator > Math > Log**

The **Log** Operator outputs the logarithm of a number. This determines how many times you need to multiply a base number to get another final number. This Operator is useful to solve mathematical problems that involve compound interest. It is the reverse of the [**Exp**](Operator-Exp.md) Operator.

The Log Operator supports the following bases:

- Log,
- Log<sub>2</sub>
- Log<sub>10</sub>

Log, also known as a natural logarithm, outputs the logarithm of a number to the base of e, which is a mathematical constant approximately equal to 2.718281828459.

Log<sub>2</sub> outputs the logarithm of a number to the base of **2** and it is the reverse of raising a number to a certain power. For example, an input of 16 outputs 4. This is because the base 2 multiplied 4 times equals 16.

Log<sub>10</sub> outputs the logarithm of a number to the base of **10**. For example, an input of 100 outputs 2. This is because multiplying the base 10 twice by itself equals 100.

## Operator settings

| **Property** | **Type** | **Description**                                              |
| ------------ | -------- | ------------------------------------------------------------ |
| **Base**     | Enum     | Specifies the base this Operator uses. The options are:<br/>&#8226; **Base 2**: Sets the Operator to use a base of 2.<br/>&#8226; **Base 10**: Sets the Operator to use a base of 10.<br/>&#8226; **Base E**: Sets the Operator to use a base of the mathematical constant *e*. |

## Operator properties

| **Input** | **Type**                                | **Description**                                       |
| --------- | --------------------------------------- | ----------------------------------------------------- |
| **X**     | [Configurable](#operator-configuration) | The number this Operator calculates the logarithm of. |

## Operator configuration

To view the Operator's configuration, click the cog icon in the Operator's header. Use the drop-down to select the type for the X port. For the list of types these properties support, see [Available types](#available-types).

### Available types

You can use the following types for your input ports:

- **Position**
- **float**
- **Vector**
- **Vector2**
- **Vector3**
