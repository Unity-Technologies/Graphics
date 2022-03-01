# Shader Stage

## Description

**Shader Stage** refers to the part of the shader pipeline a [Node](Node.md) or [Port](Port.md) is part of. For example, **Vertex** or **Fragment**.

In Shader Graph, **Shader Stage** is defined per port but often all ports on a node are locked to the same **Shader Stage**. Ports on some nodes are unavailable in certain **Shader Stages** due to limitations in the underlying shader language. See the [Node Library](Node-Library.md) documentation for nodes that have **Shader Stage** restrictions.

## Shader Stage List

| Name        | Description                        |
|:------------|:-----------------------------------|
| Vertex      | Operations calculated per vertex   |
| Fragment    | Operations calculated per fragment |
