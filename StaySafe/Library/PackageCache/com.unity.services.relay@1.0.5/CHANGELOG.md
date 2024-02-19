# Changelog

## [1.0.5] - 2022-11-23
* Update "SimpleRelay" sample
* Replaced "InteractiveRelayUtp" sample with "SimpleRelayUtp"
* Update QoS dependency to 1.1.0
* Update UTP dependency to 1.3.0
* Generated the latest version of the API client

## [1.0.4] - 2022-07-18
* Update QoS dependency to 1.0.2
* Update UTP dependency to 1.1.0

## [1.0.3] - 2022-05-30
* Update com.unity.nuget.newtonsoft-json dependency to 3.0.2
* Update com.unity.services.qos dependency to 1.0.1

## [1.0.2] - 2022-05-11
* Updated Relay QoS failure warnings
* Updated packaged documentation

## [1.0.1] - 2022-05-05

* Update to core and authentication dependencies
* Replace internal Qos functionality with standalone Qos package, consumed from Core
* Update samples to use Qos by default

## [1.0.1-pre.6] - 2022-03-03

* removed Authentication package from dependencies
* Replaced "Relay" and "IRelayServiceSDK", replaced with "RelayService" and "IRelayService"
* Update com.unity.nuget.newtonsoft-json dependency to 3.0.1

## [1.0.1-pre.5] - 2022-02-14

* fixed a compilation error when using QoS

## [1.0.1-pre.4] - 2022-01-26

* update core and authentication dependencies
* update transport (UTP) dependency
* Add QoS as default region-selection behaviour when `com.unity.ucg.qos` package is present.
* Add _upm field

## [1.0.1-pre.3] - 2021-10-20

* Renamed Package Display Name from "Relay Allocations" to "Relay".

## [1.0.1-pre.2] - 2021-10-08

* Adding Get Regions to SimpleRelay sample.
* Updated LICENSE.md with new license disclaimer. 
* Added handling for invalid request schema code (15001).
* SimpleRelay sample now discoverable from UPM.
* Configuration.SetBasePath now publically accessible via WrappedRelayService.
* Updates to latest version of Relay Service API
* Updated README.

## [1.0.1-pre.1] - 2021-08-24

* Bug fixes.

## [1.0.0-pre.4] - 2021-08-16

* Updates to latest version of Relay Service API
* Improved error handling and messages
* Simple Relay sample added.

## [1.0.0-pre.3] - 2021-08-06

* Updates to latest version of Relay Service API
* Multi-region support

## [1.0.0-pre.2] - 2021-07-30

* Update Third Party Notices
* Update package description
* Use generator version `v0.4.0`
    * uses `com.unity.services.core` `v1.1.0-pre.4`
    * allows compatibility with `com.unity.services.authentication` `v0.7.1`
* Adds `ListRegions`
* `CreateAllocationAsync` takes optional argument `region`

## [0.0.1-preview.9] - 2021-07-07

* Update documentation, points to dashboard

## [0.0.1-preview.8] - 2021-07-07

* Update documentation folder

## [0.0.1-preview.7] - 2021-07-05

* Improve README

## [0.0.1-preview.6] - 2021-06-23

* Update `JoinAllocation` response object to include `404s`.

## [0.0.1-preview.5] - 2021-06-17

* Use generator version `v0.2.0`
    * uses `com.unity.services.core` `v1.1.0`
    * allows compatibility with `com.unity.services.authentication` `v0.5.0`
* Sample project updated to use `com.unity.services.authentication` `v0.5.0`
* No breaking changes on Relay SDK

## [0.0.1-preview.4] - 2021-06-16

* Use generator version `v0.1.0`
    * Fixed obsolete warning in Unity 2020 with `HttpClientResponse` by making use
    of `UnityWebRequest`
    * BREAKING CHANGE: use `RelayService.Configuration.BasePath` instead of `Configuration.BasePath`
* Update to closed beta version of the allocations HTTP API
    * Adds `AllocationIdBytes` to `JoinAllocation` and `Allocation` response objects
    * Adds `ServerEndpoints` to `JoinAllocation` and `Allocation` response objects
    * `RelayServer` (used in `JoinAllocation` and `Allocation` response objects) is now deprecated. Instead `ServerEndpoints` should be used.


## [0.0.1-preview.3] - 2021-05-12

* Update to newer allocations API, error type changed.

## [0.0.1-preview.2] - 2021-04-30

* Use generator version `v0.0.25-preview`
* Obtain Access Token from Core SDK
* Use production gateway URL by default
* BREAKING CHANGE: wraps API response body in a `Response<T>` object
* Cleanup markdown documents
* Inline documentation

## [0.0.1-preview.1] - 2021-04-23

* Removed `Samples~` folder.
* No functional changes.

## [0.0.1-preview] - 2021-04-19

First internal release
