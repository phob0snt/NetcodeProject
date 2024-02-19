# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.2.1] - 2023-06-28

### Changed
- Fix a crash on Android when calling GetSortedMultiplayQosResultsAsync.

## [1.2.0] - 2023-03-13

### Changed
- Updated Code Generation to latest and regenerated SDK code
- Re-generate code with latest spec and generator
- Add new QoS APIs for specific Relay and Multiplay QoS servers searching and result

## [1.1.0] - 2022-11-08

### Changed
- The SDK will now compile on WebGL, but will throw an exception when invoked

## [1.0.2] - 2022-07-13

### Changed
- Updated dependencies: Collections to 1.2.4

## [1.0.1] - 2022-05-27

### Changed
- Updated dependencies: Core to 1.4.0, Newtonsoft JSON to 3.0.2
- Fixed an issue making the public interface (QosService.Instance.GetSortedQosResultsAsync(...))
unusable from the default Unity assembly (Assembly-CSharp)

## [1.0.0] - 2022-05-04

### Added
- Metrics reporting for latency & packet loss

### Changed
- Update Core/Auth/Collections dependencies
- Use proper assembly name Unity.Services.QoS instead of Unity.ucg.QoS (this makes the SDK unsupported on some older editor versions)

## [1.0.0-pre.3] - 2022-03-22

### Changed
- Use IQosResults & QosResult from Core instead of local package
- Rename private QosResult struct used by QosJob to InternalQosResult

### Removed
- IQosResults interface, QosResult, they are in Core now

## [1.0.0-pre.2] - 2022-03-18

### Changed
- Updated dependency versions

## [1.0.0-pre.1] - 2022-03-17

### Added
- QoS discovery API client
- SDK public API
- Real QoS measurements
- Public API documentation
