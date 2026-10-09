# Changelog

## 3.0.0 - Unreleased

### Added

- Scan for VSE devices from the wizard; the scanner dataflow `DCW-Device-Scanner` carries one scanner per device kind
- Enter username and password for IO-Link masters that require authentication, when scanning or adding them manually
- Lock icon next to scanned IO-Link devices that require authentication
- Generate a master-diagnostic FunctionBlock from the IO-Link master's diagnostic nodes
- Serial number of IO-Link devices in the device tree tooltip
- Multi-select in the process data grid with row, group and select-all checkboxes, and grouping by container
- Bulk bar to enable and disable the selection per cloud or for all clouds, with a settings panel for interval, function, recordings, raw data and event triggers
- Bulk reset of the selection's settings to the defaults of a newly discovered data point
- Sidebar panel "Information" with a throughput projection per period, per-cloud and per-device share bars and a list of unsaved changes
- Datapoint usage meter under the title, replacing the datapoint-limit banner
- Live View line stating how long it has been watching, how many values arrived and at what rate
- Labelled save button with a "saved / not saved" status
- Themed in-app dialog (Cancel / Discard / Save) when leaving the page with unsaved changes
- MQTT 5.0 messages carry the value's timestamp as user property `Timestamp` and, where the device reports one, its unit as user property `unit`

### Changed

- Integrate with the driver's `IoTCoreConfiguration` connection model; every subscriber references a per-master identifier
- Live View uses the layout of the configuration grid, with grouping, sticky header and virtualization
- Path column removed; each row shows its origin as a second line under its name
- Live View values are right-aligned, with the unit beside them
- Redesigned device tree tooltip with grouped sections per node type
- Alias dialog shows the device being renamed, counts against the 64-character limit and previews the resulting name
- Aliased nodes are named the same way in breadcrumbs as in the tree
- Add dialog for IO-Link masters names its two steps when credentials are required
- Masters the scan cannot confirm online are shown offline, together with their children
- Unfold the device tree to a value that goes offline without rebuilding it
- Dataflow containers are created only when a FunctionBlock is placed in them
- Scanners are named "IO-Link Scan" and "VSE Scan" and placed below each other in the dataflow
- Devices already in the tree are no longer offered in the scan results
- Required SVG icons from the MonochromeIcon repository are now provided at runtime through the ViciOne.Ui.MonochromeIcons.Assets package
- `ResizableSidebar` and `SplitView` replaced by the components from `ViciOne.Ui.Blazor.Components`
- `PoolingGrid` renamed to `AggregationInterval`, `PoolingMode` to `AggregationFunction`
- `VseAddresses` moved from `DataCollectionWizard.Public` into the client
- Tests run on Microsoft.Testing.Platform instead of Microsoft.NET.Test.Sdk
- NuGet packages are pushed to the GitLab package registry
- Renovate monitors dependencies and creates update merge requests
- Rename the company to `ViciOne open automation gmbh` in the license and the package metadata

### Fix

- `DeviceTreeGuard` left nodes offline after repeated reconnects
- `DeviceTreeGuard` raised no online event for a master last persisted online
- `IsDeployInProgressAsync` returned true after the deployment had completed
- Device scanner dataflow was removed when a device was deleted
- Live grid did not repaint when a device went offline or came back
- `VseDataflowGenerator` threw for assigned-name nodes
- Search box and list styles were missing in the scan dialogs
- Grammar and spelling in the resource files

### Dependencies

- `ViciOne.Driver.IoTCore.Contracts` package, replaced by `ViciOne.DeviceTree.Contracts` version `2.1.0`
- `ViciOne.Suite.DataPort.Anna.Contracts` package, removed
- `ViciOne.Ui.Shared.Dx` package, removed
- `ViciOne.Ui.MonochromeIcons.Assets` package, replaced by `ViciOne.Ui.MonochromeIcons.Components`
- `xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk` and `coverlet.collector` packages, replaced by `xunit.v3.mtp-v2` version `4.0.1` and `Microsoft.Testing.Extensions.CodeCoverage` version `18.12.0`
- `.NET` packages, update to version `10.0.12`
- `AspNetCore.SassCompiler` package, update version to `1.105.1`
- `ViciOne.Ui.Design` package, update version to `2.5.0`
- `ViciOne.Ui.MonochromeIcons.Components` package, update version to `4.18.0`
- `ViciOne.Ui.Blazor.Components` package, update version to `6.5.0`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `3.0.0-beta2`
- `ViciOne.Suite.Sdk` package, update version to `3.4.0`
- `NSubstitute` package, update version to `6.2.0`

## 2.1.2 - 2026-06-23

### Changed

- Replaced local SVG resources with SVGs provided by the MonochromeIcon NuGet packages and removed unused resource files
- Use text from ViciOne.Ui.Localization
- Replace circle indicators with a 3-segment SVG bracket (top=error, center=warning, bottom=new)
- Replaced `DxTabs` with `ViciOne.Ui.Blazor.Components.TabStrip`

### Dependencies

- `ViciOne.Ui.Blazor.Components` package, update version to `5.16.0`
- `ViciOne.Ui.Design` package, update version to `2.1.0`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `2.2.0`
- `ViciOne.Suite.Sdk` package, update version to `2.1.0`

## 2.1.1 - 2026-05-21

### Changed

- Format Live View values using client culture

### Fix

- Fix cloud grid cells not being disabled for unsupported cloud connections

## 2.1.0 - 2026-05-18

### Changed

- Replaced `DxTextBox` with `ViciOne.Ui.Blazor.Components.TextBox`
- Replaced `Shared.Dx.Components.SearchBox` with `ViciOne.Ui.Blazor.Components.SearchBox`
- Renamed Live View column from "Last updated" to "Last changed" and added a Tooltip
- Optimize Live View rendering performance with refresh batching
- Support virtual scrolling in process data grid to improve performance with large datasets
- Adjust validation for Hostname and IP address

### Fix

- Generate correct container name for sensors
- Icon for deleting ProcessData was misaligned when only a single action button was available
- `DeviceTreeGuard` did not dispose MQTT subscription handles on shutdown, causing leaked subscriptions

### Dependencies

- `ViciOne.Ui.Blazor.Components` package, update version to `5.9.0`
- `ViciOne.Driver.IoTCore.Contracts` package, update version to `2.0.1`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `2.1.0`
- `ViciOne.Ui.TreeEditor` package, update version to `2.0.1`

## 2.0.1 - 2026-04-17

### Fix

- `KeyNotFoundException` when selecting a tree node that contains newly discovered data nodes (`DeviceTreeProcessData`) not yet present in the path Dictionary
- Text alignment has been adjusted in the dialogs for adding devices and editing aliases

## 2.0.0 - 2026-04-16

### Changed

- Replaced `DxDialog` with `ViciOne.Ui.Blazor.Components.Dialog`

### Fix

- Save button blocked for ~60 seconds on standalone instances due to internal message bus deadlock
- UI remained disabled after a failed or rejected cluster deployment
- Cluster commit failures did not release the update lock, permanently blocking subsequent save and delete operations
- Update lock not released when consumers threw exceptions between ticket acquisition and deployment
- Connection change processing did not release update lock on early return or exception
- Replaced blocking `ManualResetEvent` calls with async `SemaphoreSlim` to prevent thread pool starvation during IO-Link scanning and deployment waiting
- Thread-safety issue on `ClusterBuilder` property getter that could cause inconsistent reads
- `SemaphoreSlim` in LiveView page could cause `ObjectDisposedException` on release after dispose

### Dependencies

- `Microsoft` packages, update to version `10.0.6`
- `AspNetCore.SassCompiler` packages, update version to `1.99.0`
- `ViciOne.Core.Dataflow.DataModel.Generation` package, update version to `1.0.0`
- `ViciOne.Suite.DataPort.Anna.Contracts` package, update version to `1.0.0`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `2.0.0`
- `ViciOne.Suite.Sdk` packages, update version to `2.0.0`
- `ViciOne.Ui.Blazor.Components` package, update version to `5.8.0`
- `ViciOne.Ui.MonochromeIcons.Assets` packages, update version to `4.7.0`
- `ViciOne.Ui.TreeEditor` package, update version to `2.0.0`

## 1.4.2 - 2026-02-21

## Fix

- DeviceTree could not be updated correctly when connections change

### Dependencies

- `ViciOne.Suite.ClusterManagement.Public` package, update version to `1.4.0`

## 1.4.1 - 2025-11-19

### Fix

- Save button does not work if no changes to the tree are made

### Dependencies

- `ViciOne.Suite.ClusterManagement.Public` package, update version to `1.3.0`

## 1.4.0 - 2025-11-17

### Changed

- Made sure only one change to the cluster is requested at a time
- Replaced `ApplyDeviceTreeCommand` with new `IDeviceTreeUpdater`

### Dependencies

- `ViciOne.Suite.ClusterManagement.Public` package, update version to `1.2.7`

## 1.3.5 - 2025-11-14

### Changed

- IsNew will be reset when DeviceTree is saved
- Updates to the DeviceTree no longer reset the users selection

### Fix

- Save button can become reenabled on rebrowse if there are new nodes
- Thread safety issues when the DeviceTree changes
- Event trigger display shows objects of previously selected raw data node

## 1.3.4 - 2025-11-06

### Changed

- Default for moneo logging nodes is now "Last"

### Fix

- Rebrowse resets New flags
- Save button can become disabled on rebrowse
- Sporadically the DeviceTree root appears twice

### Dependencies

- `ViciOne.Driver.IoTCore.Contracts` package, update version to `1.21.0`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `1.2.6`

## 1.3.3 - 2025-10-30

### Fix

- Set input and output Connectors for converter FunctionBlocks to the correct value comparison method

## 1.3.2 - 2025-10-28

### Added

- Send process values to the moneo|cloud only when they are available

### Dependencies

- `ViciOne.Driver.IoTCore.Contracts` package, update version to `1.20.1`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `1.2.5`

## 1.3.1 - 2025-10-27

### Changed

- Set BrokerReceiveMaximum to 100 for Mqtt connection

## 1.3.0 - 2025-10-23

### Changed

- Wait for all connection changes
- Show unit for warning and damage of vse object
- Raise event when DeviceTree changes

### Dependencies

- `ViciOne.Driver.IoTCore.Contracts` package, update version to `1.19.0`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `1.2.4`
- `ViciOne.Suite.DataPort.Anna.Contracts` package, update version to `0.38.0`

## 1.2.2 - 2025-09-20

### Changed

- Replace `BooleanToDouble` FunctionBlock with seperate double output Connector

### Dependencies

- `ViciOne.Driver.IoTCore.Contracts` package, update version to `1.18.3`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `1.2.2`

## 1.2.1 - 2025-09-12

### Changed

- Change unit for minute to min
- Change sorting of DeviceTree

### Fix

- Fix tooltip text overflow
- Fix for deleting offline nodes of VSE

### Dependencies

- `ViciOne.Driver.IoTCore.Contracts` package, update version to `1.18.2`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `1.2.1`

### Fixed

- Fix text Overflow in tooltip

## 1.2.0 - 2025-09-08

### Added

- Add PoolingMode Last for moneo|cloud
- Insert custom implemantation of DCP scanner
- Add lock mechanism for Data Collection Wizard actions during cluster application

### Changed

- Optimize FB Name generation for VSE
- Set alias max length to 64 Chars
- Remove empty information for tooltip
- Activate Autonomous Migration to prevent the Data Collection Wizard configuration from being deleted during updates

### Fixed

- Fix updating of ApplicationSpecificTag
- Fix MQTT properties for multiple devices
- Fix recursive loop for deleting offline nodes
- OnChange logging now generates the correct changes to the cluster

### Dependencies

- `ViciOne.Driver.IoTCore.Contracts` package, update version to `1.18.1`
- `ViciOne.Suite.DataPort.Anna.Contracts` package, update version to `0.37.0`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `1.2.0`
- `AspNetCore.SassCompiler` package, update version to `1.92.0`

## 1.1.0 - 2025-08-01

### Added

- Maintain units in DeviceTree for use in moneo connect

### Dependencies

- `ViciOne.Driver.IoTCore.Contracts` package, update version to `1.17.0`
- `ViciOne.Suite.DataPort.Anna.Contracts` package, update version to `0.36.0`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `1.1.0`

## 1.0.0 - 2025-07-25

### Added

- Restricted access to Data Collection Wizard and Live View pages

### Changed

- Lock Actions (Add Devices, Save, Rescan) when deploying Cluster
- Changed default pooling value from 10 seconds to 1 Minute for moeno|cloud
- Disabled tree expand/collapse buttons when nothing selected (DM & LV)

### Dependencies

- `ViciOne.Suite.Sdk` packages, update version to `1.0.0`
- `ViciOne.Driver.IoTCore.Contracts` package, update version to `1.16.1`
- `ViciOne.Suite.DataPort.Anna.Contracts` package, update version to `0.35.0`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `1.0.0`
- `ViciOne.Ui.Shared.Dx` package, update version to `0.15.0`

### Fixed

- Fix CMS Anna identifiers

## 0.20.0 - 2025-07-15

### Added

- Add localization for `Devices` in Sidebar and DeviceTree

### Removed

- Remove message banner for device count

### Dependencies

- `ViciOne.Suite.ClusterManagement.Public` package, update version to `0.36.0`
- `ViciOne.Suite.DataPort.Anna.Contracts` package, update version to `0.34.0`
- `ViciOne.Suite.Sdk` packages, update version to `0.31.0`
- `ViciOne.Driver.IoTCore.Contracts` package, update version to `1.16.0`
- `ViciOne.Ui.Blazor.Components` package, update version to `3.8.7`
- `ViciOne.Ui.Shared.Dx` package, update version to `0.14.0`

## 0.19.0 - 2025-06-19

### Added

- Use TimeProvider in Live View

### Dependencies

- `ViciOne.Suite.ClusterManagement.Public` package, update version to `0.34.5`
- `ViciOne.Suite.Sdk` packages, update version to `0.30.4`
- `ViciOne.Ui.Blazor.Components` package, update version to `3.8.5`
- `ViciOne.Ui.MonochromeIcons.Assets` package, update version to `3.6.0`
- `ViciOne.Driver.IoTCore.Contracts` package, update version to `1.15.1`
- `AspNetCore.SassCompiler` package, update version to `1.89.2`
- `Microsoft.NET.Test.Sdk` package, update version to `17.14.1`

### Fixed

- Add multiple IO-Link Master via scan

## 0.18.2 - 2025-06-04

### Fixed

- Fix IO-Link Master address validation during onboarding

## 0.18.1 - 2025-05-28

### Added

- Set elevated privileges for EngineHost

## 0.18.0 - 2025-05-27

### Added

- Use latest IoddStore

### Dependencies

- `AspNetCore.SassCompiler` package, update version to `1.89.0`
- `FastDeepCloner` package, update version to `1.3.6`
- `ViciOne.Core.Dataflow.DataModel.Generation` package, update version to `0.48.0`
- `ViciOne.Cluster.Builder` package, update version to `0.10.0`
- `ViciOne.Driver.IoTCore.Contracts` package, update version to `1.15.0`
- `ViciOne.Suite.DataPort.Anna.Contracts` package, update version to `0.33.0`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `0.34.0`
- `ViciOne.Ui.Blazor.Components` package, update version to `3.7.1`
- `Microsoft.NET.Test.Sdk` package, update version to `17.14.0`
- `ViciOne.Suite.Sdk` packages, update version to `0.30.0`
- `ViciOne.Ui.TreeEditor` package, update version to `0.9.0`

## 0.17.1 - 2025-05-06

### Fixed

- Fix JSModule null argument exception
- Fix correlation issue on dataflow deployment

## 0.17.0 - 2025-05-05

### Added

- Add Tooltip for DeviceTree
- Add scan for IO-Link master

### Changed

- Replace loading spinner with component provided
- Change RunMode of DataFormatter to Cyclic
- Change RunMode of moneo Dataport to Change
- Filter DeviceTree on subtitles as well

### Dependencies

- `AspNetCore.SassCompiler` package, update version to `1.87.0`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `0.33.1`
- `ViciOne.Ui.Blazor.Components` package, update version to `3.6.0`
- `ViciOne.Ui.MonochromeIcons.Assets` package, update version to `3.4.0`
- `ViciOne.Ui.Shared.Dx` package, update version to `0.11.1`
- `ViciOne.Suite.Sdk` packages, update version to `0.29.0`
- `ViciOne.Cluster.Builder` package, update version to `0.9.0`
- `ViciOne.Driver.IoTCore.Contracts` package, update version to `1.14.0`
- `ViciOne.Suite.DataPort.Anna.Contracts` package, update version to `0.31.0`

## 0.16.1 - 2025-03-21

### Fixed

- Workaround for assign connector value type error

## 0.16.0 - 2025-03-21

### Dependencies

- `AspNetCore.SassCompiler` package, update version to `1.86.0`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `0.32.1`
- `ViciOne.Suite.Sdk` packages, update version to `0.28.0`
- `ViciOne.Driver.IoTCore.Contracts` package, update version to `1.13.3`

## 0.15.1 - 2025-03-10

### Fixed

- When the moneo connection is deleted, the related DataPort is now correctly cleaned up

## 0.15.0 - 2025-03-07

### Dependencies

- `ViciOne.Suite.ClusterManagement.Public` package, update version to `0.31.3`
- `ViciOne.Suite.Sdk` packages, update version to `0.27.3`
- `ViciOne.Driver.IoTCore.Contracts` package, update version to `1.13.2`

## 0.14.0 - 2025-03-06

### Dependencies

- `AspNetCore.SassCompiler` package, update version to `1.85.1`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `0.31.1`
- `ViciOne.Suite.Sdk` packages, update version to `0.27.2`
- `ViciOne.Ui.Shared.Dx` package, update version to `0.9.0`
- `ViciOne.Ui.Blazor.Components` package, update version to `3.3.1`
- `ViciOne.Suite.DataPort.Anna.Contracts` package, update version to `0.30.0`

## 0.13.0 - 2025-02-14

### Changed

- UI redesign for `Data Collection Wizard` and `Live View`

- `.NET` packages, update version to `9.0.2`
- `ViciOne.Cluster.Builder` package, update version to `0.7.0`
- `ViciOne.Suite.DataPort.Anna.Contracts` package, update version to `0.29.0`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `0.29.1`
- `ViciOne.Suite.Sdk` packages, update version to `0.26.4`
- Rename database from `devicemgmt` to `data-collection-wizard`

### Added

- Add message banner for device / data point limits
- Add Anna queue size for raw data and process data

## 0.12.0 - 2025-02-07

### Added

- `ViciOne.Ui.MonochromeIcons.Assets` package, added version `3.3.0`

### Changed

- UI redesign for `Data Collection Wizard` and `Live View`

- `.NET` packages, update version to `9.0.1`
- `AspNetCore.SassCompiler` package, update version to `1.83.4`
- `ViciOne.Ui.Blazor.Components` package, update version to `3.1.1`
- `ViciOne.Cluster.Builder` package, update version to `0.6.0`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `0.29.0`
- `ViciOne.Suite.Sdk` packages, update version to `0.26.0`
- `ViciOne.Ui.Shared.Dx` package, update version to `0.8.0`

## 0.11.0 - 2025-01-14

### Changed

- `ViciOne.Suite.ClusterManagement.Public` package, update version to `0.27.0`

## 0.10.0 - 2024-12-20

### Changed

- `.NET` packages, update version to `9.0.0`
- `AspNetCore.SassCompiler` package, update version to `1.83.0`
- `ViciOne.Cluster.Builder` package, update version to `0.5.0`
- `ViciOne.Core.Dataflow.DataModel.Generation` package, update version to `0.46.0`
- `ViciOne.Suite.DataPort.Anna.Contracts` package, update version to `0.27.0`
- `ViciOne.Driver.IoTCore.Contracts` package, update version to `1.11.0`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `0.26.0`
- `ViciOne.Suite.Sdk` packages, update version to `0.25.0`
- `ViciOne.Ui.Shared.Dx` package, update version to `0.7.0`
- `ViciOne.Ui.TreeEditor` package, update version to `0.7.9`

## 0.9.0 - 2024-12-09

### Changed

- Update validation for device addresses

### Fixed

- Fix loading screen if a timeout between engine and frontend occurs

## 0.8.0 - 2024-12-03

### Changed

- `ViciOne.Suite.ClusterManagement.Public` package, update version to `0.24.0`
- `ViciOne.Suite.Sdk` packages, update version to `0.20.0`

### Fixed

- Fix page title localization

## 0.7.0 - 2024-11-19

### Changed

- `AspNetCore.SassCompiler` package, update version to `1.80.6`
- `ViciOne.Suite.Sdk` packages, update version to `0.19.0`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `0.24.0`
- `ViciOne.Ui.Shared.Dx` package, update version to `0.6.0`
- `ViciOne.Ui.TreeEditor` package, update version to `0.7.5`

## 0.6.0 - 2024-11-06

### Added

- Add moneo connect status

### Changed

- Change ModuleId to `ViciOne.Suite.DeviceManagement`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `0.23.0`

## 0.5.0 - 2024-10-29

### Added

- Add initial module metadata
- Add IO-Link Master Onboarding
- Add moneo connect communication

### Changed

- `ViciOne.Cluster.Builder` package, update version to `0.4.0`
- `ViciOne.Suite.Sdk` packages, update version to `0.18.0`
- `ViciOne.Suite.ClusterManagement.Public` package, update version to `0.22.0`
- `ViciOne.Suite.DataPort.Anna.Contracts` package, update version to `0.20.0`

## 0.4.0 - 2024-09-06

### Changed

- `ViciOne.Suite.DataPort.Anna.Contracts` package, update version to `0.17.0`

### Added

- moneo MQTT DataPort

## 0.3.0 - 2024-09-03

### Changed

- `ViciOne.Suite.Sdk` packages, update version to `0.15.0`
- `ViciOne.Suite.DataPort.Anna.Contracts` package, update version to `0.15.0`

## 0.2.0 - 2024-08-23

### Added

- ANNA DataPort
- DeviceTree updates when opening Data Collection Wizard
- Edit aliases of MasterDevices

### Fixed

- Collapsing DeviceTree

## 0.1.2 - 2024-08-22

### Changed

- `ViciOne.Cluster.Builder` package, update version to `0.2.0.1275346-ci`
- `ViciOne.Suite.Sdk` packages, update version to `0.14.0`

## 0.1.1 - 2024-08-08

### Changed

- `ViciOne.Ui.Shared.Dx` package, update version to `0.1.0.1269238`

## 0.1.0 - 2024-07-29

- Initial Release
