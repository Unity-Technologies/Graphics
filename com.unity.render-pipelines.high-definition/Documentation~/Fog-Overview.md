# Fog overview

HDRP provides different methods of producing fog within your Unity Project. HDRP’s [Volume](Volumes.html) framework allows you to easily manage localized fog in different areas of your Scene. In each Volume, you can choose the fog you want to use by altering the Fog Type property in the Volume’s [Visual Environment](Visual-Environment.html) override. HDRP provides the following types of fog:

- **Linear Fog**: Increases fog density linearly with view distance and world space height. This is useful for applying fog to rendering larger, lower priority, areas of your Scene as it is less resource intensive as the other fog types. 
- **Exponential Fog**: Increases fog density exponentially with view distance and world space height. This fog type offers a more realistic fog effect than linear. 
- [Volumetric Fog](Volumetric-Fog.html): Realistically simulates the interaction of lights with fog, which allows for physically-plausible rendering of glow and crepuscular rays.