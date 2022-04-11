# Changes introduced by the new structure


* The packages were moved from root to Packages/.
* The testing packages were moved from root to Tests/SRPTests/Packages
* The test projects were moved from root to Tests/SRPTests/Projects **_except PostProcessing_Tests which stayed in the TestProjects/ folder._**

<table>
  <tr>
   <td>
<strong>Old Path</strong>
   </td>
   <td><strong>New Path</strong>
   </td>
  </tr>
  <tr>
   <td>com.unity.render-pipelines.core
   </td>
   <td><strong>Packages/</strong>com.unity.render-pipelines.core
   </td>
  </tr>
  <tr>
   <td>com.unity.render-pipelines.high-definition-config
   </td>
   <td><strong>Packages/</strong>com.unity.render-pipelines.high-definition-config
   </td>
  </tr>
  <tr>
   <td>com.unity.render-pipelines.high-definition
   </td>
   <td><strong>Packages/</strong>com.unity.render-pipelines.high-definition
   </td>
  </tr>
  <tr>
   <td>com.unity.render-pipelines.universal
   </td>
   <td><strong>Packages/</strong>com.unity.render-pipelines.universal
   </td>
  </tr>
  <tr>
   <td>com.unity.shaderanalysis
   </td>
   <td><strong>Packages/</strong>com.unity.shaderanalysis
   </td>
  </tr>
  <tr>
   <td>com.unity.shadergraph
   </td>
   <td><strong>Packages/</strong>com.unity.shadergraph
   </td>
  </tr>
  <tr>
   <td>com.unity.visualeffectgraph
   </td>
   <td><strong>Packages/</strong>com.unity.visualeffectgraph
   </td>
  </tr>
  <tr>
   <td>com.unity.testing.graphics-performance
   </td>
   <td><strong>Tests/SRPTests/Packages</strong>/com.unity.testing.graphics-performance
   </td>
  </tr>
  <tr>
   <td>com.unity.testing.hdrp
   </td>
   <td><strong>Tests/SRPTests/Packages</strong>/com.unity.testing.hdrp
   </td>
  </tr>
  <tr>
   <td>com.unity.testing.urp-upgrade
   </td>
   <td><strong>Tests/SRPTests/Packages</strong>/com.unity.testing.urp-upgrade
   </td>
  </tr>
  <tr>
   <td>com.unity.testing.urp
   </td>
   <td><strong>Tests/SRPTests/Packages</strong>/com.unity.testing.urp
   </td>
  </tr>
  <tr>
   <td>com.unity.testing.visualeffectgraph
   </td>
   <td><strong>Tests/SRPTests/Packages</strong>/com.unity.testing.visualeffectgraph
   </td>
  </tr>
  <tr>
   <td>com.unity.testing.xr
   </td>
   <td><strong>Tests/SRPTests/Packages</strong>/com.unity.testing.xr
   </td>
  </tr>
  <tr>
   <td>TestProjects/BatchRendererGroup_HDRP
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/BatchRendererGroup_HDRP
   </td>
  </tr>
  <tr>
   <td>TestProjects/BatchRendererGroup_URP
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/BatchRendererGroup_URP
   </td>
  </tr>
  <tr>
   <td>TestProjects/BuiltInGraphicsTest_Foundation
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/BuiltInGraphicsTest_Foundation
   </td>
  </tr>
  <tr>
   <td>TestProjects/BuiltInGraphicsTest_Lighting
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/BuiltInGraphicsTest_Lighting
   </td>
  </tr>
  <tr>
   <td>TestProjects/HDRP_DXR_Tests
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/HDRP_DXR_Tests
   </td>
  </tr>
  <tr>
   <td>TestProjects/HDRP_PerformanceTests
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/HDRP_PerformanceTests
   </td>
  </tr>
  <tr>
   <td>TestProjects/HDRP_RuntimeTests
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/HDRP_RuntimeTests
   </td>
  </tr>
  <tr>
   <td>TestProjects/HDRP_Tests
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/HDRP_Tests
   </td>
  </tr>
  <tr>
   <td>TestProjects/Lightmapping
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/Lightmapping
   </td>
  </tr>
  <tr>
   <td>TestProjects/SRP_SmokeTest
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/SRP_SmokeTest
   </td>
  </tr>
  <tr>
   <td>TestProjects/ShaderGraph
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/ShaderGraph
   </td>
  </tr>
  <tr>
   <td>TestProjects/ShaderGraphUniversalStereo
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/ShaderGraphUniversalStereo
   </td>
  </tr>
  <tr>
   <td>TestProjects/UniversalGfxTestStereo
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/UniversalGfxTestStereo
   </td>
  </tr>
  <tr>
   <td>TestProjects/UniversalGraphicsTest_2D
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/UniversalGraphicsTest_2D
   </td>
  </tr>
  <tr>
   <td>TestProjects/UniversalGraphicsTest_Foundation
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/UniversalGraphicsTest_Foundation
   </td>
  </tr>
  <tr>
   <td>TestProjects/UniversalGraphicsTest_Lighting
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/UniversalGraphicsTest_Lighting
   </td>
  </tr>
  <tr>
   <td>TestProjects/UniversalGraphicsTest_PostPro
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/UniversalGraphicsTest_PostPro
   </td>
  </tr>
  <tr>
   <td>TestProjects/UniversalGraphicsTest_Terrain
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/UniversalGraphicsTest_Terrain
   </td>
  </tr>
  <tr>
   <td>TestProjects/UniversalUpgradeTest
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/UniversalUpgradeTest
   </td>
  </tr>
  <tr>
   <td>TestProjects/VisualEffectGraph_HDRP
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/VisualEffectGraph_HDRP
   </td>
  </tr>
  <tr>
   <td>TestProjects/VisualEffectGraph_URP
   </td>
   <td><strong>Tests/SRPTests/Projects</strong>/VisualEffectGraph_URP
   </td>
  </tr>
</table>


# Guidelines to merge with your custom changes

Breaking down the merges into 2 steps makes it more manageable:


1. First merge <last mainline commit before the restructure>:
    * Master: [fa006468e96219788e5bf60e8e887e3d3bd7557f](https://github.com/Unity-Technologies/Graphics/commit/fa006468e96219788e5bf60e8e887e3d3bd7557f)
    * 2022.1: [107fe47144883a35e93f6f2a88ee691d9059bdf3](https://github.com/Unity-Technologies/Graphics/commit/107fe47144883a35e93f6f2a88ee691d9059bdf3)
    * 2021.3: [ad85b534d7ac336305f8e0d87c02cc23cbcbd6be](https://github.com/Unity-Technologies/Graphics/commit/ad85b534d7ac336305f8e0d87c02cc23cbcbd6be)
2. Then merge the actual restructured mainline

In case you want to merge <branch> into <mainline>, it’s safer to first merge <mainline> into <branch> than the other way around. ([https://stackoverflow.com/a/57313561/5804755](https://stackoverflow.com/a/57313561/5804755)). Side effect: you now have <mainline> into <branch>.


# Solution


1. `git checkout <branch>`
2. `git merge <last mainline commit before the restructure> // deal with "regular" Merge Conflicts`
3. Manually restructure the repo as was done on the mainline (described above). This can be done via the file explorer or if you feel more adventurous, via script (your mileage may vary depending on your OS - it may just be easier to do it by hand)
4. `git add .`
5. `git commit -m "Apply new structure"`
6. `git merge -s ours <mainline>`
7. // If you want to merge <branch> into the mainline

    ```
    git checkout <mainline>
    git merge <branch>
