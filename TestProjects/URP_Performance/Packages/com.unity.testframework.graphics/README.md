# Graphics Tests Framework

This package provides a foundation for writing tests for Graphics features in Unity projects.

Currently it contains:

* ImageAssert, for doing image renders and comparisons with reference images
* Automatic collection and deployment of reference images
* Automatic generation of tests from scenes in the project

It's currently a bit rough, but as improvments to Unity's test framework are made we should be able to make it nicer, as well as expand the functionality it offers.

See [the Documentation](Documentation~/com.unity.testframework.graphics.md) for more information.

### Package CI Summary

Package Name | Latest CI Status
------------ | ---------
com.unity.testframework.graphics | [![](https://badges.cds.internal.unity3d.com/packages/com.unity.testframework.graphics/build-badge.svg?branch=master&testWorkflow=package-isolation)](https://badges.cds.internal.unity3d.com/packages/com.unity.testframework.graphics/build-info?branch=master&testWorkflow=package-isolation) [![](https://badges.cds.internal.unity3d.com/packages/com.unity.testframework.graphics/dependencies-badge.svg?branch=master&testWorkflow=updated-dependencies)](https://badges.cds.internal.unity3d.com/packages/com.unity.testframework.graphics/dependencies-info?branch=master&testWorkflow=updated-dependencies) [![](https://badges.cds.internal.unity3d.com/packages/com.unity.testframework.graphics/dependants-badge.svg)](https://badges.cds.internal.unity3d.com/packages/com.unity.testframework.graphics/dependants-info) [![](https://badges.cds.internal.unity3d.com/packages/com.unity.testframework.graphics/warnings-badge.svg?branch=master)](https://badges.cds.internal.unity3d.com/packages/com.unity.testframework.graphics/warnings-info?branch=master) [![](https://badges.cds.internal.unity3d.com/packages/com.unity.testframework.graphics/template-badge.svg?branch=master&testWorkflow=template-published)](https://badges.cds.internal.unity3d.com/packages/com.unity.testframework.graphics/template-info?branch=master&testWorkflow=template-published) ![ReleaseBadge](https://badges.cds.internal.unity3d.com/packages/com.unity.testframework.graphics/release-badge.svg) ![ReleaseBadge](https://badges.cds.internal.unity3d.com/packages/com.unity.testframework.graphics/candidates-badge.svg)
