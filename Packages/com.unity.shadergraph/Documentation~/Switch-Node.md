# Switch node

Use the Switch node to branch based on different cases with value comparisons.

## Input ports

| **Name** | **Type** | **Description** |
| :--- | :--- | :--- |
| **Predicate** | Float | The value to test against the conditions. |
| **Fallback** | Dynamic | The fallback value when the **Predicate** doesn't meet any of the conditions. |

The other input ports allow you to input different branches for each condition you set up for the node. The presence, number and order of these ports depend on the way you set up the conditions. Use one of the two following options:

* Set up conditions within the node in the [Node settings](#node-settings) through the **Conditions** table.

* Set up conditions through a [**Float** Property](Property-Types.md#float) you configure as an **Enum** and connect to the **Predicate** port.

## Output port

| **Name** | **Type** | **Description** |
| :--- | :--- | :--- |
| **Out** | Dynamic | The value from the input port that the **Predicate** meets the condition of, otherwise the value from the **Fallback** input port. |

## Node settings

| **Name** | **Description** |
| :--- | :--- |
| **Floor Predicate** | Rounds down the value of the predicate before testing it against the conditions.<br/>Enable this property to make sure the node still outputs values when the **Predicate** doesn't exactly match a **Condition**. |
| **Conditions** | Defines the list of conditions within the node and creates the corresponding input ports.<br/>To add a [condition](#condition), select **Add** (+).<br/><br/>**Note**: If you connect a **Float** Property configured as an **Enum** to the **Predicate** port, the Switch node ignores the **Conditions** list and uses the **Entries** of the connected Property instead. |

### Condition

| **Name** | **Description** |
| :--- | :--- |
| **Name** | The condition's name, which Unity also uses as the name for the corresponding input port. Unity automatically names the conditions with letters in alphabetical order and you can't edit them. |
| **Type** | The type of comparison to make between the **Predicate** and the condition's **Value**. The options are:<ul><li>Equal</li><li>Not Equal</li><li>Less</li><li>Less Or Equal</li><li>Greater</li><li>Greater Or Equal</li></ul> |
| **Value** | The value to test the **Predicate** against, according to the condition's **Type**. |
