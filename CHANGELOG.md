# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
 - Planar Reflection Probe support roughness (gaussian convolution of captured probe)

### Changed
 - Depth and color pyramid are properly computed and sampled when the camera renders inside a viewport of a RTHandle.
 - Forced Planar Probe update modes to (Realtime, Every Update, Mirror Camera)
 - Removed Planar Probe mirror plane position and normal fields in inspector, always display mirror plane and normal gizmos

## [0.1.6] - 2018-xx-yy

### Changelog starting

Started Changelog