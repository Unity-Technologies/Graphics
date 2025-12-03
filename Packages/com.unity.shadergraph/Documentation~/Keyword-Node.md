# Keyword node

## Description

You can use a Keyword node to create branches or shader variants that reference [Keywords](Keywords.md) on the [Blackboard](Blackboard.md).

Based on the Keyword's definition, the node either generates shader variants or dynamic branches. For more information, refer to [Declare shader keywords](https://docs.unity3d.com/Manual/SL-MultipleProgramVariants-declare.html).

The appearance of a Keyword node, including its available ports, changes based on the Keyword it references and its definition.

## Creating new Keyword Nodes
Because each Keyword node references a specific Keyword, you must first define at least one Keyword on the Blackboard. Drag a Keyword from the Blackboard to the workspace to make a Keyword node that corresponds to that Keyword.

You can also right-click anywhere on the workspace, and use the **Create Node** menu to make a new Keyword node. Under **Keywords**, there is a list of Keywords that you defined on the Blackboard. Click on a Keyword in that list to create a corresponding Keyword node.
