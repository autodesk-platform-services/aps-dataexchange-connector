# DataExchange Connector UI Migration Guide

This guide documents SDK upgrades for the **Sample UI Connector**. The most recent
migration is listed first; earlier migrations are preserved below for reference.

- [🔄 Migration Guide: SDK 8.1.0 Upgrade](#-migration-guide-sdk-810-upgrade) — **latest**
- [🔄 Migration Guide: SDK 8.0.0 Upgrade](#-migration-guide-sdk-800-upgrade)
- [🔄 Migration Guide: SDK 7.6.0-beta Upgrade](#-migration-guide-sdk-760-beta-upgrade)
- [🔄 Migration Guide: SDK 7.5.0 Upgrade](#-migration-guide-sdk-750-upgrade)
- [🔄 Migration Guide: SDK 7.2.1-beta Upgrade](#-migration-guide-sdk-721-beta-upgrade)

---

## 🔄 Migration Guide: SDK 8.1.0 Upgrade

This section documents the migration from SDK 8.0.0 to **Autodesk Data Exchange SDK 8.1.0**.

### 📋 Overview of Changes

- **SDK Version**: Upgraded to `Autodesk.DataExchange 8.1.0`
- **UI SDK Version**: Upgraded to `Autodesk.DataExchange.UI 8.1.0`
- **Breaking Changes**: None that affect source code — two `InteropBridgeOptions` URL properties are now
  `[Obsolete]`. Everything else in this release is additive.
- **Build result**: 0 errors (`msbuild SampleConnector.sln -p:Configuration=Debug -p:Platform=x64`)

### 🚀 Key Dependency Updates

| Package | Previous Version | New Version | Impact |
|---------|------------------|-------------|---------|
| `Autodesk.DataExchange` | `8.0.0` | `8.1.0` | **Minor** - additive |
| `Autodesk.DataExchange.UI` | `8.0.0` | `8.1.0` | **Minor** - two deprecations, otherwise additive |

### ⚠️ Breaking Changes

#### 1. `InteropBridgeOptions` support URLs are deprecated

`ContactSupportUrl` and `FeedbackUrl` are now marked `[Obsolete]`. They still work, but new code
should stop setting them.

#### 3. Low-level Mesh API surface

`MeshIO.ReadFromDirectArrays`, `MeshIO.WriteToDirectArrays`, and the public
`MeshBinarySerializer`/`MeshBinaryDeserializer` entry points were removed, and
`MeshIO.WriteTo(List<...>)` gained a trailing optional `int maxDegreeOfParallelism = -1`. Connectors
that use `ElementDataModel.CreateMeshGeometry` — including this sample — are unaffected.

### ✨ New Capabilities

#### SAT download

`IClient.DownloadCompleteExchangeAsSAT` joins the existing STEP, OBJ, IFC, and USD download methods,
backed by the new `GeometryFormat.Sat` and `BrepOutputFormat.SAT` values. The sample now downloads
SAT alongside the other formats in `CustomReadWriteModel.DownloadExchangeGeometryAsync`:

```csharp
var satResult = this.Client.DownloadCompleteExchangeAsSAT(exchangeIdentifier, downloadPath, cancellationToken);
```

#### Cancellation and progress

- `UserCancelledException` and `CancellationClassifier` (`IsUserCancellation`, `IsTimeout`) let a
  connector tell a deliberate cancellation apart from a timeout.
- `IInteropBridge.UpdateProgressSubText(string, DateTime?)` adds secondary progress text with an
  optional last-synced timestamp.
- `IInteropBridge.UpdateFeatureCapabilities(FeatureCapabilities)` toggles UI capabilities after the
  bridge has been created; previously these could only be set through `InteropBridgeOptions`.

#### Other additions

`DeletedFolders.AccessDenied` reports folders that could not be checked because of permissions.

---

## 🔄 Migration Guide: SDK 8.0.0 Upgrade

This section documents the migration from SDK 7.6.0-beta to **Autodesk Data Exchange SDK 8.0.0**.

### 📋 Overview of Changes

- **SDK Version**: Upgraded to `Autodesk.DataExchange 8.0.0`
- **UI SDK Version**: Upgraded to `Autodesk.DataExchange.UI 8.0.0`
- **Breaking Changes**: Yes — signature changes, one type removal, one assembly consolidation,
  and the deletion of the APIs that were marked `[Obsolete]` in 7.6.0-beta. `IStorage.Save()` now
  requires the key of the entry being persisted, `IClient.DownloadCompleteExchangeAsOBJ` now takes a
  `DataExchangeIdentifier` instead of separate exchange/collection id strings, the
  `ElementProperties` type is gone entirely, and `Autodesk.DataExchange.BaseModels.dll` no longer
  ships as a standalone assembly.
- **Build result**: 0 errors after fixes applied (`msbuild SampleConnector.sln -p:Configuration=Debug -p:Platform=x64`)

### 🚀 Key Dependency Updates

| Package | Previous Version | New Version | Impact |
|---------|------------------|-------------|---------|
| `Autodesk.DataExchange` | `7.6.0-beta` | `8.0.0` | **Major** - breaking changes |
| `Autodesk.DataExchange.UI` | `7.6.0-beta` | `8.0.0` | **Major** - breaking changes |

### ⚠️ Breaking Changes

#### 1. `IStorage.Save()` requires the key to persist

The parameterless `Save()` — which flushed every in-memory entry — is gone. The new signature is
`Save(string key, string group = null)`, so persistence is now scoped to the entry that changed
(and optionally its group, matching the `group` overloads of `Add`/`Get`/`Remove`).

**Before (7.6.0-beta):**
```csharp
_sDKOptions.Storage.Add("LocalExchanges", localStorage);
_sDKOptions.Storage.Save();
```

**After (8.0.0):**
```csharp
_sDKOptions.Storage.Add("LocalExchanges", localStorage);
_sDKOptions.Storage.Save("LocalExchanges");
```

**Migration Action:** Pass to `Save` the same key that was passed to `Add`. If a code path called
`Save()` unconditionally after a conditional `Add`, move the `Save` call next to the `Add` so the
key is always in scope — see `SampleHostWindow.Destroy`.

#### 2. `IClient.DownloadCompleteExchangeAsOBJ` takes a `DataExchangeIdentifier`

The `(string exchangeId, string collectionId, ...)` overload was removed. Only `OBJ` was affected:
`STEP`, `IFC`, and `USD` already took a `DataExchangeIdentifier` in 7.6.0-beta, so as of 8.0.0 all
four `DownloadCompleteExchangeAs*` methods share one shape:
`(DataExchangeIdentifier dataExchangeIdentifier, string path = null, CancellationToken cancellationToken = default)`.

**Before (7.6.0-beta):**
```csharp
var objResult = this.Client.DownloadCompleteExchangeAsOBJ(
    exchangeIdentifier.ExchangeId,
    exchangeIdentifier.CollectionId,
    downloadPath,
    cancellationToken);
```

**After (8.0.0):**
```csharp
var objResult = this.Client.DownloadCompleteExchangeAsOBJ(exchangeIdentifier, downloadPath, cancellationToken);
```

**Migration Action:** Pass the `DataExchangeIdentifier` directly instead of destructuring it into
`ExchangeId`/`CollectionId`.

#### 3. `ElementProperties` and `AddElement(ElementProperties)` are removed

Marked `[Obsolete]` in 7.6.0-beta, these are deleted outright in 8.0.0 — the
`ElementProperties` type no longer exists in the assembly, so there is no fallback if the replacement
APIs give trouble. This sample was already migrated during the 7.6.0-beta upgrade, so no code change
was needed here.

**Migration Action:** Replace `AddElement(new ElementProperties(id, name, category, family, type))`
with `AddElement(id, name)` followed by `Classify` and `DefineType`/`SetType` as described below.

#### 4. `Autodesk.DataExchange.BaseModels.dll` is no longer a separate assembly

The `Autodesk.DataExchange.UI` package used to ship a standalone
`Autodesk.DataExchange.BaseModels.dll` alongside `Autodesk.DataExchange.UI.Bridge.dll`. In 8.0.0
that assembly is gone, and the five types it contained are declared directly inside
`Autodesk.DataExchange.UI.Bridge.dll`:

- `Autodesk.DataExchange.BaseModels.BaseExchangeModel`
- `Autodesk.DataExchange.BaseModels.BaseReadOnlyExchangeModel`
- `Autodesk.DataExchange.BaseModels.BaseReadWriteExchangeModel`
- `Autodesk.DataExchange.BaseModels.BaseWriteOnlyExchangeModel`
- `Autodesk.DataExchange.UI.Helper.ExchangeUrl`

Their namespaces are unchanged, so this is **source-compatible**: `using Autodesk.DataExchange.BaseModels;`
and `class CustomReadWriteModel : BaseReadWriteExchangeModel` still compile untouched, and no code
change was needed in this sample.

**Migration Action:** Nothing to do if you consume the SDK through `PackageReference`, as this sample
does — NuGet stops copying the removed file automatically. You do need to act if either of the
following applies:

- You reference `Autodesk.DataExchange.BaseModels.dll` through an explicit `<Reference>` with a
  `HintPath`, or list it in an installer, packaging script, or `app.config` binding redirect. Those
  references now point at a file that does not exist and must be removed.
- You are upgrading in place over an earlier build output. A stale
  `Autodesk.DataExchange.BaseModels.dll` left in `bin\` will still satisfy the loader and mask the
  change, so clean the output directory (or run `msbuild -t:Rebuild`) to be sure you are testing
  against 8.0.0 alone.

#### 5. APIs marked `[Obsolete]` in 7.6.0-beta are deleted

7.6.0-beta obsoleted a number of members and kept them functional. 8.0.0 removes them, so any
`CS0618` warning that was suppressed or ignored during the 7.6.0-beta upgrade is now a `CS0117`/
`CS1061` compile error. The removals are:

| Removed in 8.0.0 | Replacement |
|------------------|-------------|
| `IClient.RetrieveLatestExchangeDataAsync(...)` | `RetrieveLatestExchangeAsync(model, cancellationToken)` |
| `IElement.Id`, `Element.Id`, `IDesign.Id`, `IDesign.ID` | `UniqueId` (or `SourceId` where a source-scoped id is wanted) |
| `IElementDataModel.DeleteElement(string)`, `DeleteElementsById(string)` | `DeleteElementByUniqueId(element.UniqueId)` |
| `IElementDataModel.GetElementById(string)`, `GetElementsById(string)` | Query `Elements` by `UniqueId`/`SourceId` |
| `IElementDataModel.GetDesigns()`, `GetDesignsById(...)`, `GetDesignInstancesById(...)`, `InstantiateDesignById(...)`, `CreateDesignRef(...)` | `UniqueId`-based design APIs |
| `ElementDataModel.AddElement(ElementProperties, ...)` and the `AddElement(string, string, Element, ...)` overload | `AddElement(id, name)` plus `Classify`/`DefineType`/`SetType` |
| `IClient.GetCollectionAsync(string hubId, string projectId)` | Current `IClient` collection APIs |
| `IClient.GetExchangeDetailsAsync(string exchangeUrn)` — the single-argument overload only | `GetExchangeDetailsAsync(collectionId, exchangeUrn)` or `GetExchangeDetailsAsync(dataExchangeIdentifier)`, both of which remain |
| `ExchangeCreateRequestACC.ACCProjectURN` | `ProjectUrn` |

A few members that were **not** obsoleted in 7.6.0-beta were also removed outright:
`IExchange.CopyExchangeLinkAsync(ExchangeItem)` (note that `IConnectorAPI.CopyExchangeLinkAsync(string)`
is unaffected), `IExchange.GetExchangeFilterView()`, `IElementGeometry.Id`, and the ADP analytics
registration entry points (`SDKOptions.RegisterAdpAnalytics` and
`AdpAnalyticsServiceCollectionExtensions.AddAdpAnalytics`).

Three more members changed shape rather than disappearing. Source that relied on their old signatures
still needs a look, but the methods themselves are still there:

| Member | 7.6.0-beta | 8.0.0 |
|--------|------------|-------|
| `IStorage.Add` | `Add(key, value, group, bool markForUpdate = false)` | `Add(key, value, group)` — `markForUpdate` dropped; three-argument calls are unaffected |
| `IExchangeReader.OnGetLatestExchangeDataAction` | `(ExchangeItem, CancellationToken = default)` | `(ExchangeItem, IElementDataModel syncedModel = null, CancellationToken = default)` — the inserted second parameter breaks positional two-argument calls |
| `ElementDataModel.CreateFileGeometry` | `(filePath \| MemoryStream, GeometryFormat, RenderStyle, Units)` | Same overloads plus a trailing optional `string sourceId = null`; existing calls still compile |

**Migration Action:** None was needed in this sample — it moved off these APIs during the 7.6.0-beta
upgrade, which is exactly why that step matters. If you skipped it, do it before upgrading to 8.0.0,
because the compiler no longer offers an obsolete-but-working path.

### ⚠️ Element types must be defined before they can be assigned

Not an 8.0.0 change — this behaviour is identical in 7.6.0-beta — but it is the one that most
easily breaks a connector migrating off `ElementProperties`, so it is worth spelling out.

`SetType(IElement, string name, IClassification under)` is **lookup-only**. It resolves an existing
type by name and throws `NotFoundException("Unable to find type:<name>")` when there is no match, so
it cannot be used on its own to give a new element a type. Creating the type is a separate call,
`DefineType(system, name, parent, sourceId)`, which is find-or-create and returns a handle.

`ElementProperties` used to build a nested `Category` -> `Family` -> `Type` hierarchy internally, so
the replacement has to reproduce all three levels and thread each handle into the next:

```csharp
var element = dataModel.AddElement(id, name);

IClassification categoryClassification = null;
if (!string.IsNullOrEmpty(category))
{
    categoryClassification = dataModel.Classify(element, ClassificationSystem.Category, category);
}

IClassification familyClassification = categoryClassification;
if (!string.IsNullOrEmpty(family))
{
    familyClassification = dataModel.Classify(element, ClassificationSystem.Family, family, categoryClassification);
}

if (!string.IsNullOrEmpty(type))
{
    dataModel.SetType(element, dataModel.DefineType("Type", type, familyClassification));
}
```

Two things to note. Passing the parent handle to each call matters: omitting it makes `Category` and
`Family` siblings at the root instead of a hierarchy. And the `IElementType` handle overload of
`SetType` is preferred over the by-name overload, which throws on ambiguity if two types anywhere in
the exchange share a name.

**Migration Action:** Anywhere an element is given a type, call `DefineType` first and pass the
returned handle to `SetType`. Keep category names consistent across call sites — two spellings of the
same category produce two separate branches, each with its own duplicate type underneath.

### 🔧 Migration Steps

#### Step 1: Update Package References

Update the version numbers in `src/SampleConnector.csproj` and
`test/SampleConnectorUnitTests/SampleConnectorUnitTests.csproj`:

```xml
<PackageReference Include="Autodesk.DataExchange" Version="8.0.0" />
<PackageReference Include="Autodesk.DataExchange.UI" Version="8.0.0" />
```

#### Step 2: Apply the Code Fixes

1. **`CustomReadWriteModel.cs`** — pass `"LocalExchanges"` to `Storage.Save` in
   `AfterUpdateExchange`; pass `exchangeIdentifier` to `DownloadCompleteExchangeAsOBJ` in
   `DownloadExchangeGeometryAsync`.
2. **`SampleHostWindow.xaml.cs`** — in `Destroy`, move `Storage.Save("LocalExchanges")` inside the
   block that performs the matching `Storage.Add`.
3. **`CreateExchangeHelper.cs`** — in `CreateElement`, define the type with `DefineType` and pass the
   returned handle to `SetType`, threading the `Category` handle into the `Family` `Classify` call;
   align the category in `AddVariedGeometryObjects` with the other call sites (`"Generics"`).
4. **`CustomReadWriteModel.cs`** — in `UpdateExistingExchangeData`, mutate the loaded
   `currentElementDataModel` instead of building an empty one with `ElementDataModel.Create(Client)`,
   otherwise the synced model has no knowledge of the already-published elements and
   `DeleteSampleElement` silently finds nothing to delete.

#### Step 3: Restore and Rebuild

**Command Line:**
```bash
BuildSolution.bat
```

### 🎯 Summary of Changes

| Aspect | SDK 7.6.0-beta | SDK 8.0.0 |
|--------|----------------|-----------------|
| Storage persistence | `Storage.Save()` flushes everything | `Storage.Save(key)` / `Storage.Save(key, group)` |
| OBJ download | `DownloadCompleteExchangeAsOBJ(exchangeId, collectionId, path, token)` | `DownloadCompleteExchangeAsOBJ(dataExchangeIdentifier, path, token)` |
| `ElementProperties` | `[Obsolete]`, still present | Removed from the assembly |
| Base exchange models | `Autodesk.DataExchange.BaseModels.dll` | Merged into `Autodesk.DataExchange.UI.Bridge.dll` (same namespaces) |
| Element typing | `SetType(name)` alone throws; `DefineType` + `SetType(handle)` required | Unchanged |

### 🧪 Testing Your Migration

After upgrading, confirm:

- ✅ `msbuild SampleConnector.sln -p:Configuration=Debug -p:Platform=x64` builds with 0 errors
- ✅ The build still succeeds from a clean output directory (`-t:Rebuild`, or delete `bin`/`obj`
  first) — this is what proves no stale `Autodesk.DataExchange.BaseModels.dll` is propping the build
  up
- ✅ The MSTest unit test suite passes (`vstest.console.exe` against `SampleConnectorUnitTests.dll`)
- ✅ Cached exchanges still survive a connector restart (storage save/load round-trip)
- ✅ Downloading an exchange produces both the STEP file and the OBJ output
- ✅ Creating an exchange succeeds (no `Unable to find type:<name>` error)
- ✅ Updating an existing exchange preserves the previously published elements and applies the
  sample deletion

---

**Migration Checklist:**
- [x] Updated all package references to 8.0.0
- [x] Passed the storage key to `IStorage.Save`
- [x] Passed `DataExchangeIdentifier` to `DownloadCompleteExchangeAsOBJ`
- [x] Defined element types with `DefineType` before assigning them with `SetType`
- [x] Reused the loaded `ElementDataModel` when updating an existing exchange
- [x] Confirmed no reference to the removed `Autodesk.DataExchange.BaseModels.dll` remains
- [x] Restored NuGet packages and rebuilt the solution (0 errors, verified with `-t:Rebuild`)
- [x] Ran the MSTest unit test suite (4/4 passed)
- [ ] Tested create / update / download workflows end to end

### 📚 Additional Resources

- [APS DataExchange SDK Documentation](https://aps.autodesk.com/en/docs/dx-sdk/v8.0.0/developers_guide/overview/)
- [APS DataExchange Release Notes](https://aps.autodesk.com/en/docs/dx-sdk/v8.0.0/changelog/v1changelog-800/)
- [Autodesk Platform Services Developer Portal](https://aps.autodesk.com/)
- [DataExchange API Reference](https://aps.autodesk.com/en/docs/dx-sdk/v8.0.0/sdk-reference/autodesk-dataexchange/)
- [Sample Code Repository](https://github.com/autodesk-platform-services/aps-dataexchange-connector)

---

## 🔄 Migration Guide: SDK 7.6.0-beta Upgrade

This section documents the migration from SDK 7.5.0 to **Autodesk Data Exchange SDK 7.6.0-beta**.

### 📋 Overview of Changes

- **SDK Version**: Upgraded to `Autodesk.DataExchange 7.6.0-beta`
- **UI SDK Version**: Upgraded to `Autodesk.DataExchange.UI 7.6.0-beta`
- **Breaking Changes**: Yes — this is **not** a pure version bump. `IClient.GetElementDataModelAsync`
  now returns `IElementDataModel`, `ElementDataModel.Elements` yields `IElement`, and a handful of
  string-Id-based APIs were renamed/obsoleted in favor of `SourceId`/`UniqueId`-based ones.
- **Build result**: 0 errors after fixes applied (`msbuild SampleConnector.sln -p:Configuration=Debug -p:Platform=x64`)

### 🚀 Key Dependency Updates

| Package | Previous Version | New Version | Impact |
|---------|------------------|-------------|---------|
| `Autodesk.DataExchange` | `7.5.0-beta` | `7.6.0-beta` | **Minor** - breaking changes |
| `Autodesk.DataExchange.UI` | `7.5.0-beta` | `7.6.0-beta` | **Minor** - breaking changes |

### ⚠️ Breaking Changes

#### 1. `Client.GetElementDataModelAsync` now returns `IElementDataModel` instead of `ElementDataModel`

The concrete `Autodesk.DataExchange.DataModels.ElementDataModel` class still exists and still
implements `IElementDataModel`, so an explicit cast is sufficient — no data model changes required.

**Before (7.5.0):**
```csharp
this.currentElementDataModel = response.Value;
```

**After (7.6.0-beta):**
```csharp
this.currentElementDataModel = (ElementDataModel)response.Value;
```

**Migration Action:** Add an explicit cast to `ElementDataModel` wherever
`Client.GetElementDataModelAsync(...).Value` is assigned to an `ElementDataModel`-typed field or variable.

#### 2. `ElementDataModel.Elements` yields `IElement`, not `Element`

Iterating `elementDataModel.Elements` now produces `IElement` instances (even on the concrete
`ElementDataModel` class). Methods that receive a *retrieved* element (as opposed to one just
created via `AddElement`) need to accept `IElement` instead of `Element`.

**Before (7.5.0):**
```csharp
public static async Task AddUniqueStringParameter(Element element)
```

**After (7.6.0-beta):**
```csharp
public static async Task AddUniqueStringParameter(IElement element)
```

**Migration Action:** Change parameter types that receive elements read back from
`ElementDataModel.Elements` from `Element` to `IElement` — e.g.
`CreateExchangeHelper.AddUniqueStringParameter`/`AddStringParameter`.

#### 3. `IClient.RetrieveLatestExchangeDataAsync` replaced by `RetrieveLatestExchangeAsync`

The `(IElementDataModel, string, string, CancellationToken)` overload is obsolete in favor of a
`CancellationToken`-only overload; the old one allowed `fromRevision`/`toRevision` to be passed out
of order and corrupt the local cache.

**Before (7.5.0):**
```csharp
var deltaResponse = await this.Client.RetrieveLatestExchangeDataAsync(this.currentElementDataModel).ConfigureAwait(false);
```

**After (7.6.0-beta):**
```csharp
var deltaResponse = await this.Client.RetrieveLatestExchangeAsync(this.currentElementDataModel, cancellationToken).ConfigureAwait(false);
```

**Migration Action:** Replace `RetrieveLatestExchangeDataAsync(model)` with
`RetrieveLatestExchangeAsync(model, cancellationToken)`.

#### 4. `IElement.Id` and `ElementDataModel.DeleteElement(string)` obsoleted — use `SourceId`/`UniqueId`

`IElement.Id` was ambiguous (it returned the connector-supplied source identifier, not the SDK
identity), and `DeleteElement(string)` deleted by that same ambiguous, non-unique Id.

**Before (7.5.0):**
```csharp
elementDataModel.DeleteElement(existingElements[0].Id);
```

**After (7.6.0-beta):**
```csharp
elementDataModel.DeleteElementByUniqueId(existingElements[0].UniqueId);
```

**Migration Action:** Replace `element.Id` with `element.SourceId` (connector-authored id) or
`element.UniqueId` (SDK-generated unique id), and replace `DeleteElement(sourceId)` with
`DeleteElementsBySourceId(sourceId)` (deletes all matches) or `DeleteElementByUniqueId(uniqueId)`
(deletes one unambiguous element — used here since a single, specific element was being deleted).

### 📝 Newly-Obsolete APIs, migrated in this sample

SDK 7.6.0-beta also marks `ElementProperties`, `ElementDataModel.AddElement(ElementProperties)`,
and `ElementDataModel.SetElementGeometry(Element, List<ElementGeometry>)` as `[Obsolete]` in favor
of `AddElement(sourceId, name, transformation, lengthUnit, displayLengthUnit)` combined with
`Classify()`/`DefineType()`/`SetType()`, and the `IElement`/`List<IElementGeometry>` overload of
`SetElementGeometry`. `CreateExchangeHelper.cs` has been migrated to the new model: each element is
created via `AddElement(id, name)`, classified with `Classify` for `Category` and `Family`, and typed
with `DefineType` + `SetType` (see the `CreateElement` helper). Geometry lists are now typed
`List<IElementGeometry>` and passed to the `IElement`-based `SetElementGeometry` overload. No
`#pragma warning disable CS0618` is required anymore in this file.

> **Note:** `SetType(element, name)` alone is not sufficient — it only resolves an already-defined
> type and throws `NotFoundException("Unable to find type:<name>")` otherwise. The type has to be
> created with `DefineType` first, and the `Category`/`Family` handles have to be threaded through to
> preserve the hierarchy `ElementProperties` used to build. See
> [Element types must be defined before they can be assigned](#-element-types-must-be-defined-before-they-can-be-assigned)
> in the 8.0.0 section for the full pattern.

### 🔧 Migration Steps

#### Step 1: Update Package References

Update the version numbers in `src/SampleConnector.csproj` and
`test/SampleConnectorUnitTests/SampleConnectorUnitTests.csproj`:

```xml
<PackageReference Include="Autodesk.DataExchange" Version="7.6.0-beta" />
<PackageReference Include="Autodesk.DataExchange.UI" Version="7.6.0-beta" />
```

#### Step 2: Apply the Code Fixes

1. **`CustomReadWriteModel.cs`** — cast `Client.GetElementDataModelAsync(...).Value` to
   `ElementDataModel` at both call sites; swap `RetrieveLatestExchangeDataAsync` for
   `RetrieveLatestExchangeAsync(model, cancellationToken)`; swap
   `DeleteElement(existingElements[0].Id)` for `DeleteElementByUniqueId(existingElements[0].UniqueId)`.
2. **`CreateExchangeHelper.cs`** — change `AddUniqueStringParameter`/`AddStringParameter` to accept
   `IElement` instead of `Element`; replace `ElementProperties`/`AddElement(ElementProperties)`/
   `SetElementGeometry(Element, List<ElementGeometry>)` with `AddElement(id, name)` +
   `Classify()`/`DefineType()`/`SetType()` + the `IElement`/`List<IElementGeometry>` overload of
   `SetElementGeometry`.

#### Step 3: Restore and Rebuild

**Command Line:**
```bash
BuildSolution.bat
```

### 🎯 Summary of Changes

| Aspect | SDK 7.5.0 | SDK 7.6.0-beta |
|--------|-----------|-----------------|
| Element retrieval | `Client.GetElementDataModelAsync` returns `ElementDataModel` | Returns `IElementDataModel`; cast to use as `ElementDataModel` |
| `ElementDataModel.Elements` | Yields `Element` | Yields `IElement` |
| Delta sync | `RetrieveLatestExchangeDataAsync(model)` | `RetrieveLatestExchangeAsync(model, cancellationToken)` |
| Element identity | `IElement.Id` (ambiguous) | `IElement.SourceId` / `IElement.UniqueId` |
| Element deletion | `DeleteElement(sourceId)` | `DeleteElementsBySourceId(sourceId)` / `DeleteElementByUniqueId(uniqueId)` |
| Element/geometry creation | `ElementProperties` + `AddElement(ElementProperties)` + `SetElementGeometry(Element, List<ElementGeometry>)` | `AddElement(id, name)` + `Classify()`/`DefineType()`/`SetType()` + `SetElementGeometry(IElement, List<IElementGeometry>)` |

### 🧪 Testing Your Migration

After upgrading, confirm:

- ✅ `msbuild SampleConnector.sln -p:Configuration=Debug -p:Platform=x64` builds with 0 errors
- ✅ The MSTest unit test suite passes (`vstest.console.exe` against `SampleConnectorUnitTests.dll`)
- ✅ Create Exchange publishes successfully (all geometry types)
- ✅ Update Exchange adds a new revision without errors
- ✅ Downloaded exchanges preview correctly in the integrated 3D viewer

---

**Migration Checklist:**
- [x] Updated all package references to 7.6.0-beta
- [x] Cast `GetElementDataModelAsync(...).Value` to `ElementDataModel` where required
- [x] Changed `Element` → `IElement` for elements retrieved from `ElementDataModel.Elements`
- [x] Replaced `RetrieveLatestExchangeDataAsync` with `RetrieveLatestExchangeAsync`
- [x] Replaced `Id`/`DeleteElement` usage with `UniqueId`/`DeleteElementByUniqueId`
- [x] Restored NuGet packages and rebuilt the solution (0 errors)
- [x] Ran the MSTest unit test suite (4/4 passed)
- [x] Migrated `CreateExchangeHelper.cs` off the now-obsolete `ElementProperties`/`AddElement(ElementProperties)`/`SetElementGeometry(Element, ...)` APIs
- [ ] Tested create / update / download workflows end to end

### 📚 Additional Resources

- [APS DataExchange SDK Documentation](https://aps.autodesk.com/en/docs/dx-sdk/v1/developers_guide/overview/)
- [APS DataExchange Release Notes](https://aps.autodesk.com/en/docs/dx-sdk/v1/developers_guide/release_notes/)
- [Autodesk Platform Services Developer Portal](https://aps.autodesk.com/)
- [DataExchange API Reference](https://aps.autodesk.com/en/docs/dx-sdk/v1/reference/)
- [Sample Code Repository](https://github.com/autodesk-platform-services/aps-dataexchange-connector)

---

## 🔄 Migration Guide: SDK 7.5.0 Upgrade

This section documents the migration from SDK 7.2.1-beta to **Autodesk Data Exchange SDK 7.5.0**.

### 📋 Overview of Changes

- **SDK Version**: Upgraded to `Autodesk.DataExchange 7.5.0-beta`
- **UI SDK Version**: Upgraded to `Autodesk.DataExchange.UI 7.5.0-beta`
- **Breaking Changes**: Yes — see below
- **Build result**: 0 errors after fixes applied

### 🚀 Key Dependency Updates

| Package | Previous Version | New Version | Impact |
|---------|------------------|-------------|---------|
| `Autodesk.DataExchange` | `7.2.1-beta` | `7.5.0-beta` | **Minor** - breaking changes |
| `Autodesk.DataExchange.UI` | `7.2.1-beta` | `7.5.0-beta` | **Minor** - breaking changes |

### ⚠️ Breaking Changes

#### 1. `Client.GenerateViewableAsync` removed — viewable generation is now server-side

In SDK 7.5.0 viewable generation is handled **server-side**. The client-side
`Client.GenerateViewableAsync` API was removed with **no replacement** — after
`SyncExchangeDataAsync` completes, the service generates the viewable automatically.

**Before (7.2.1):**

```csharp
await this.Client.SyncExchangeDataAsync(dataExchangeIdentifier, elementDataModel);

// Explicitly request viewable generation from the client.
await this.Client.GenerateViewableAsync(exchangeItem.ExchangeID, dataExchangeIdentifier.CollectionId);
```

**After (7.5.0):**

```csharp
await this.Client.SyncExchangeDataAsync(dataExchangeIdentifier, elementDataModel);

// Viewable generation is handled server-side in SDK 7.5.0; the client-side
// Client.GenerateViewableAsync API was removed (no replacement).
```

**Migration Action:** Remove all calls to `Client.GenerateViewableAsync`.

#### 2. `RenderStyle` and `RGBA` default constructors deprecated

The parameterless constructors with property setters are marked `[Obsolete]`. Use the
parameterized constructors instead.

**Before (7.2.1):**

```csharp
private RenderStyle commonRenderStyle = new RenderStyle()
{
    Name = "Common Render Style",
    RGBA = new RGBA() { Red = 255, Green = 0, Blue = 0, Alpha = 255 },
    Transparency = 1
};
```

**After (7.5.0):**

```csharp
private RenderStyle commonRenderStyle = new RenderStyle("Common Render Style", new RGBA(255, 0, 0, 255), 1);
```

**Migration Action:** Replace every `new RenderStyle() { ... }` / `new RGBA() { ... }`
with the parameterized form `new RenderStyle(name, rgba, transparency)` /
`new RGBA(red, green, blue, alpha)`.

#### 3. Complete authentication before launching the Connector UI

The `Client` constructor calls `Initialize()` internally, and the Connector UI
requests a token as soon as it connects. Create the `Client` **off the UI thread**
and finish authentication (`GetAuthTokenAsync`) **before** wiring up and launching
the `IInteropBridge`, so the UI does not race the auth flow.

**After (7.5.0):**

```csharp
// Create the client off the UI thread so OAuth does not block message handling.
await Task.Run(() =>
{
    this.client = new Client(this.sdkOptions);
}).ConfigureAwait(true);

// Finish authentication before the Connector UI connects and requests a token.
await this.sdkOptions.AuthProvider.GetAuthTokenAsync().ConfigureAwait(true);

// Now build the bridge and launch the UI.
var bridgeOptions = InteropBridgeOptions.FromClient(this.client);
// ...
```

**Migration Action:** Move client creation onto a background thread and call
`GetAuthTokenAsync()` before creating/launching the `IInteropBridge`.

### 🔧 Migration Steps

#### Step 1: Update Package References

Update the version numbers in `src/SampleConnector.csproj`. The `PackageReference`
format is also simplified — the `IncludeAssets`/`ExcludeAssets` overrides are no
longer required (NuGet resolves transitive dependencies automatically):

**Before:**

```xml
<ItemGroup>
  <PackageReference Include="Autodesk.DataExchange" Version="7.2.1-beta">
    <IncludeAssets>all</IncludeAssets>
    <ExcludeAssets>runtime; build; native; contentfiles; analyzers</ExcludeAssets>
  </PackageReference>
  <PackageReference Include="Autodesk.DataExchange.UI" Version="7.2.1-beta" />
</ItemGroup>
```

**After:**

```xml
<ItemGroup>
  <PackageReference Include="Autodesk.DataExchange" Version="7.5.0-beta" />
  <PackageReference Include="Autodesk.DataExchange.UI" Version="7.5.0-beta" />
</ItemGroup>
```

Apply the same version updates to `test/SampleConnectorUnitTests/SampleConnectorUnitTests.csproj`.

#### Step 2: Apply the Code Fixes

1. **`CustomReadWriteModel.cs`** — remove the `Client.GenerateViewableAsync` call after `SyncExchangeDataAsync`.
2. **`CreateExchangeHelper.cs`** — replace `new RenderStyle() { ... }` / `new RGBA() { ... }` with the parameterized constructors.
3. **`SampleHostWindow.xaml.cs`** — create the `Client` off the UI thread and call `GetAuthTokenAsync()` before launching the Connector UI.

#### Step 3: Restore and Rebuild

**Visual Studio:**
- Open `src/SampleConnector.sln`
- Rebuild the solution (packages restore automatically)

**Command Line:**

```bash
BuildSolution.bat
```

### 🎯 Summary of Changes

| Aspect | SDK 7.2.1 | SDK 7.5.0 |
|--------|-----------|-----------|
| Viewable generation | Client-side via `GenerateViewableAsync` | Server-side; API removed |
| `RenderStyle` / `RGBA` | Default constructor + setters | Parameterized constructors required |
| Auth flow | Token fetched lazily | Call `GetAuthTokenAsync()` before launching the UI |
| Client construction | On UI thread | Off the UI thread (`Task.Run`) |
| `PackageReference` | `IncludeAssets`/`ExcludeAssets` overrides | Simplified — no asset overrides |

### 🧪 Testing Your Migration

After upgrading, launch the sample and confirm:

- ✅ OAuth2 sign-in completes before the Connector UI appears
- ✅ Create Exchange publishes successfully (all geometry types)
- ✅ Update Exchange adds a new revision without errors
- ✅ Downloaded exchanges preview correctly in the integrated 3D viewer

---

**Migration Checklist:**
- [x] Updated all package references to 7.5.0
- [x] Removed `Client.GenerateViewableAsync` calls
- [x] Replaced `RenderStyle`/`RGBA` with parameterized constructors
- [x] Completed auth before launching the Connector UI
- [ ] Tested create / update / download workflows end to end

### 📚 Additional Resources

- [APS DataExchange SDK Documentation](https://aps.autodesk.com/en/docs/dx-sdk/v1/developers_guide/overview/)
- [APS DataExchange Release Notes](https://aps.autodesk.com/en/docs/dx-sdk/v1/developers_guide/release_notes/)
- [Autodesk Platform Services Developer Portal](https://aps.autodesk.com/)
- [DataExchange API Reference](https://aps.autodesk.com/en/docs/dx-sdk/v1/reference/)
- [Sample Code Repository](https://github.com/autodesk-platform-services/aps-dataexchange-connector)

---

## 🔄 Migration Guide: SDK 7.2.1-beta Upgrade

This section documents the migration from SDK 7.2.0 to **Autodesk Data Exchange SDK 7.2.1-beta**.

### 📋 Overview of Changes

This patch upgrade removes the Description field from the Create Exchange form.

- **SDK Version**: Upgraded to `Autodesk.DataExchange 7.2.1-beta`
- **SDK Version**: Upgraded to `Autodesk.DataExchange.UI 7.2.1-beta`
- **Bug Fixes**: Description field removed from the Create Exchange form

### 🚀 Key Dependency Updates

| Package | Previous Version | New Version | Impact |
|---------|------------------|-------------|---------|
| `Autodesk.DataExchange` | `7.2.0` | `7.2.1-beta` | **Patch** - Bug fixes |
| `Autodesk.DataExchange.UI` | `7.2.0` | `7.2.1-beta` | **Patch** - Bug fixes |

### ⚠️ Breaking Changes

#### 1. Removal of Description field from the Create Exchange form

The `Description` field has been removed from the Create Exchange form UI.

**Before (SDK 7.2.0):**

```tsx
<FormTextField
  id="create-exchange-description"
  multiline
  minRows={1}
  maxRows={3}
  title={t("DESCRIPTION")}
  required={false}
  variant="outlined"
  placeholder={t("ADD_DESCRIPTION")}
  value={description}
  onChange={(e) => setDescription(e.target.value)}
/>
```

**After (SDK 7.2.1-beta):**

```tsx
// This component is no longer available and usage should be deleted.
//
// <FormTextField
//   id="create-exchange-description"
//   multiline
//   minRows={1}
//   maxRows={3}
//   title={t("DESCRIPTION")}
//   required={false}
//   variant="outlined"
//   placeholder={t("ADD_DESCRIPTION")}
//   value={description}
//   onChange={(e) => setDescription(e.target.value)}
// />
```

**Migration Action:** No changes required

### 🔧 Migration Steps

#### Step 1: Update Package References

Update the version numbers in your .csproj file:

**Before:**

```xml
<ItemGroup>
  <PackageReference Include="Autodesk.DataExchange" Version="7.2.0">
    <IncludeAssets>all</IncludeAssets>
    <ExcludeAssets>runtime; build; native; contentfiles; analyzers</ExcludeAssets>
  </PackageReference>
  <PackageReference Include="Autodesk.DataExchange.UI" Version="7.2.0" />
</ItemGroup>
```

**After:**

```xml
<ItemGroup>
  <PackageReference Include="Autodesk.DataExchange" Version="7.2.1-beta">
    <IncludeAssets>all</IncludeAssets>
    <ExcludeAssets>runtime; build; native; contentfiles; analyzers</ExcludeAssets>
  </PackageReference>
  <PackageReference Include="Autodesk.DataExchange.UI" Version="7.2.1-beta" />
</ItemGroup>
```

### 📚 Additional Resources

- [APS DataExchange SDK Documentation](https://aps.autodesk.com/en/docs/dx-sdk/v1/developers_guide/overview/)
- [APS DataExchange Release Notes](https://aps.autodesk.com/en/docs/dx-sdk/v1/developers_guide/release_notes/)
- [Autodesk Platform Services Developer Portal](https://aps.autodesk.com/)
- [DataExchange API Reference](https://aps.autodesk.com/en/docs/dx-sdk/v1/reference/)
- [Sample Code Repository](https://github.com/autodesk-platform-services/aps-dataexchange-connector)

For complex migration scenarios or specific technical questions, consult the official release notes and consider reaching out to Autodesk support channels.

---

*This migration guide provides guidance for the transition from version 7.2.0 to 7.2.1-beta. Always refer to the official documentation and release notes for the most accurate and up-to-date information.*
