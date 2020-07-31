# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [7.3.0-preview] - 2020-07-09
* Added optional callback on ImageAssert triggered after all cameras are rendered.

## [7.2.3-preview] - 2020-07-06
* Enable multiple scenes per test filter and clean up UI a bit.
* Fixes a memory allocation in the Profiler.Get function that was counted as memory allocation in the render loop of SRP.

## [7.2.2-preview] - 2020-06-08
* Wrap built in xr checks in 2020_2_OR_NEWER due to built in xr deprecation in 2020.2 and higher.
* Test filter fixes for multiple matching filters

## [7.2.1-preview] - 2020-05-01
* Backwards compatibility to 2019.3

## [7.2.0-preview] - 2020-04-30
* Add the option for tests to use the back buffer instead of rendering to a render texture first
* Fix LoadedXRDevice to use XR SDK first

## [7.1.13-preview] - 2020-04-06
* Update reference versions of json and utp

## [7.1.12-preview] - 2020-03-24
* Bug fix for where all scenes would be baked when only one was selected.
* Bug fix for Xbox where tests would fail due to XR APIs

## [7.1.11-preview] - 2020-03-20
* Fix for OSX Metal automation

## [7.1.10-preview] - 2020-03-20
* Add build targets for DX12 and OSX Metal

## [7.1.9-preview] - 2010-03-19
* Use Standalone XR settings for Editor play mode XR

## [7.1.8-preview] - 2020-03-18
* Fix Test Result Window

## [7.1.7-preview] - 2020-03-17
* Change MockHMD folder to None for playmode

## [7.1.6-preview] - 2020-03-16
* Improved messaging in GC Alloc
* Test filters no longer override disabled tests in build settings
* Adds a check so if vr is supported and that array is empty, set xrsdk to MockHMD

## [7.1.5-preview] - 2020-02-14
* Fixing issues where Standalone tests wouldn't work for some projects

## [7.1.4-preview] - 2020-02-13
* Adding GC Alloc changes for HDRP

## [7.1.3-preview] - 2019-11-25
* Updating dependency names

## [7.1.2-preview] - 2019-11-04
* Adding com.unity.nuget.test-protocol and com.unity.newtonsoft-json as dependencies

## [7.1.1-preview] - 2019-09-23
* Adding script for testing with different Graphics APIs

## [7.1.0-preview] - 2019-09-09
* Separated Graphics Test Framework into its own repository

## [6.6.0] - 2019-04-01

## [6.5.0] - 2019-03-07

## [6.4.0] - 2019-02-21

## [6.3.0] - 2019-02-18

## [6.2.0] - 2019-02-15

## [6.1.0] - 2019-02-13

## [6.0.0] - 2019-02-23

## [5.2.0] - 2018-11-27

## [5.1.0] - 2018-11-18

## [5.0.0-preview] - 2018-09-28

## [4.0.0-preview] - 2019-09-21

## [3.3.0]

## [3.2.0]

## [3.1.0]

## [0.1.0] - 2018-05-04

### This is the first release of *Unity Package com.unity.testframework.graphics*.

* ImageAssert for comparing images
* Automatic management of reference images and test case generation
