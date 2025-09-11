# Utility nodes

Enable essential logic operations, previews, and sub-graph referencing.

| **Topic**                      | **Description**                                                                    |
|--------------------------------|------------------------------------------------------------------------------------|
| [Preview](Preview-Node.md)     | Provides a preview window and passes the input value through without modification. |
| [Sub-Graph](Sub-graph-Node.md) | Provides a reference to a Sub-graph asset.                                         |

## Logic

| **Topic**                          | **Description**                                                                                   |
|------------------------------------|---------------------------------------------------------------------------------------------------|
| [All](All-Node.md)                 | Returns true if all components of the input In are non-zero.                                      |
| [And](And-Node.md)                 | Returns true if both the inputs A and B are true.                                                 |
| [Any](Any-Node.md)                 | Returns true if any of the components of the input In are non-zero.                               |
| [Branch](Branch-Node.md)           | Provides a dynamic branch to the shader.                                                          |
| [Comparison](Comparison-Node.md)   | Compares the two input values A and B based on the condition selected on the dropdown.            |
| [Is Infinite](Is-Infinite-Node.md) | Returns true if any of the components of the input In is an infinite value.                       |
| [Is NaN](Is-NaN-Node.md)           | Returns true if any of the components of the input In is not a number (NaN).                      |
| [Nand](Nand-Node.md)               | Returns true if both the inputs A and B are false.                                                |
| [Not](Not-Node.md)                 | Returns the opposite of input In. If In is true, the output is false. Otherwise, it returns true. |
| [Or](Or-Node.md)                   | Returns true if either input A or input B is true.                                                |
