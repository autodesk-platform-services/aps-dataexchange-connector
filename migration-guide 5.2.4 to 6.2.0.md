# DataExchange Connector Migration Guide
## From Version 5.2.4 to 6.2.0

### Table of Contents
1. [Introduction](#introduction)
2. [Prerequisites](#prerequisites)
3. [Key Changes](#key-changes)
4. [Migration Steps](#migration-steps)
5. [Code Examples](#code-examples)
6. [Common Pitfalls](#common-pitfalls)
7. [Conclusion](#conclusion)

---

## Introduction

This migration guide assists developers in updating their DataExchange Connector codebase from version 5.2.4 to 6.2.0. The primary change involves transitioning from direct `ExchangeData` usage to using `ElementDataModel` as the primary interface for all data exchange operations.

**Important:** The `ExchangeData` class is now reserved for internal use only in version 6.2.0. All external interactions should use `ElementDataModel` which provides a more robust and feature-rich API.

For comprehensive details beyond this migration guide, please refer to:
- [APS DataExchange SDK Documentation](https://aps.autodesk.com/en/docs/dx-sdk/v1/developers_guide/overview/)
- [APS DataExchange Release Notes](https://aps.autodesk.com/en/docs/dx-sdk/v1/developers_guide/release_notes/)
- [Autodesk Platform Services Developer Portal](https://aps.autodesk.com/)
- [DataExchange API Reference](https://aps.autodesk.com/en/docs/dx-sdk/v1/reference/)

---

## Prerequisites

Before starting the migration, ensure you have:

1. **Access to both codebases:**
   - Current implementation (version 5.2.4)
   - Target implementation (version 6.2.0)

2. **Updated packages:**
   - Autodesk.DataExchange version 6.2.0
   - Autodesk.DataExchange.UI version 6.2.0
   - All dependent packages updated to compatible versions

3. **Development environment:**
   - Visual Studio with .NET Framework 4.8
   - Valid Autodesk Platform Services credentials

4. **Testing capabilities:**
   - Test data exchange files
   - Access to Autodesk Construction Cloud (ACC)

---

## Key Changes

### Primary Change: ExchangeData → ElementDataModel

The core change is simple: **stop using `ExchangeData` directly and use `ElementDataModel` for everything**.

| Version | Usage Pattern |
|---------|---------------|
| **5.2.4** | Mixed: `ExchangeData` + `ElementDataModel` wrapper |
| **6.2.0** | Single: `ElementDataModel` only |

**What this means:**
- `ExchangeData` becomes internal-only (you can't access it directly)
- `ElementDataModel` handles all data operations
- Simpler, cleaner API with better functionality

### Additional Breaking Changes

1. **Namespace Changes:** Some classes may have moved namespaces
2. **Method Signatures:** Updated method parameters and return types
3. **Event Handling:** Modified event subscription patterns
4. **Error Handling:** Enhanced exception types and error messages

---

## Migration Steps

### Step 1: Update Package References

Update your `packages.config` file:

```xml
<!-- OLD (5.2.4) -->
<package id="Autodesk.DataExchange" version="5.2.4-beta" targetFramework="net48" />
<package id="Autodesk.DataExchange.UI" version="5.2.4-beta" targetFramework="net48" />

<!-- NEW (6.2.0) -->
<package id="Autodesk.DataExchange" version="6.2.0" targetFramework="net48" />
<package id="Autodesk.DataExchange.UI" version="6.2.0" targetFramework="net48" />
```

### Step 2: Identify ExchangeData Usage

Search your codebase for the following patterns:
- `ExchangeData` variable declarations
- `Client.GetExchangeDataAsync()` calls
- Direct `ExchangeData` property access
- `ExchangeData` parameter passing

### Step 3: Replace ExchangeData Variables

**Pattern to find:**
```csharp
private ExchangeData currentExchangeData;
```

**Replace with:**
```csharp
private ElementDataModel currentElementDataModel;
```

### Step 4: Update Data Retrieval Methods

**OLD Method (5.2.4):**
```csharp
// Get ExchangeData directly
currentExchangeData = await Client.GetExchangeDataAsync(exchangeIdentifier);

// Create wrapper
var data = ElementDataModel.Create(Client, currentExchangeData);
```

**NEW Method (6.2.0):**
```csharp
// Get ElementDataModel directly
currentElementDataModel = await Client.GetElementDataModelAsync(exchangeIdentifier);
// OR create new ElementDataModel
currentElementDataModel = ElementDataModel.Create(Client);
```

### Step 5: Update Property Access

**OLD Property Access (5.2.4):**
```csharp
// Access through ExchangeData
var exchangeId = currentExchangeData.ExchangeID;
var identifier = data.ExchangeData.ExchangeIdentifier;
```

**NEW Property Access (6.2.0):**
```csharp
// Access through ElementDataModel
var exchangeId = currentElementDataModel.ExchangeID;
var identifier = currentElementDataModel.ExchangeIdentifier;
```

### Step 6: Update Method Parameters

Replace methods that accept `ExchangeData` parameters:

**OLD Signature (5.2.4):**
```csharp
private async Task ProcessExchange(ExchangeData exchangeData)
{
    var elementModel = ElementDataModel.Create(Client, exchangeData);
    // Process data...
}
```

**NEW Signature (6.2.0):**
```csharp
private async Task ProcessExchange(ElementDataModel elementDataModel)
{
    // Process data directly...
}
```

---

## Code Examples

### Example 1: Data Retrieval and Processing

#### Before (5.2.4):
```csharp
public async Task GetLatestExchangeDataAsync(ExchangeItem exchangeItem)
{
    var exchangeIdentifier = new DataExchangeIdentifier
    {
        CollectionId = exchangeItem.ContainerID,
        ExchangeId = exchangeItem.ExchangeID,
        HubId = exchangeItem.HubId,
    };

    // Get Exchange data
    currentExchangeData = await Client.GetExchangeDataAsync(exchangeIdentifier);
    
    // Use ElementDataModel Wrapper
    var data = ElementDataModel.Create(Client, currentExchangeData);
    
    // Get elements
    var wallElements = data.Elements.Where(element => element.Category == "Walls").ToList();
    
    // Access exchange properties through wrapper
    var wholeGeometryPath = Client.DownloadCompleteExchangeAsSTEP(data.ExchangeData.ExchangeIdentifier);
}
```

#### After (6.2.0):
```csharp
public async Task GetLatestExchangeDataAsync(ExchangeItem exchangeItem)
{
    var exchangeIdentifier = new DataExchangeIdentifier
    {
        CollectionId = exchangeItem.ContainerID,
        ExchangeId = exchangeItem.ExchangeID,
        HubId = exchangeItem.HubId,
    };

    // Get ElementDataModel directly
    currentElementDataModel = await Client.GetElementDataModelAsync(exchangeIdentifier);
    
    // Get elements directly
    var wallElements = currentElementDataModel.Elements.Where(element => element.Category == "Walls").ToList();
    
    // Access exchange properties directly
    var wholeGeometryPath = Client.DownloadCompleteExchangeAsSTEP(currentElementDataModel.ExchangeIdentifier);
}
```

### Example 2: Creating New Exchange

#### Before (5.2.4):
```csharp
public async Task CreateNewExchange()
{
    // Create a new ElementDataModel wrapper
    var currentElementDataModel = ElementDataModel.Create(Client);

    // Set Unit info on Root Asset through ExchangeData
    (currentElementDataModel.ExchangeData.RootAsset as DesignAsset).LengthUnit = UnitFactory.Feet;
    (currentElementDataModel.ExchangeData.RootAsset as DesignAsset).DisplayLengthUnit = UnitFactory.Feet;

    // Sync using ExchangeData
    await Client.SyncExchangeDataAsync(exchangeIdentifier, currentElementDataModel.ExchangeData);
}
```

#### After (6.2.0):
```csharp
public async Task CreateNewExchange()
{
    // Create a new ElementDataModel
    var currentElementDataModel = ElementDataModel.Create(Client);

    // Set Unit info directly through ElementDataModel
    currentElementDataModel.SetLengthUnit(UnitFactory.Feet);
    currentElementDataModel.SetDisplayLengthUnit(UnitFactory.Feet);

    // Sync using ElementDataModel
    await Client.SyncElementDataModelAsync(exchangeIdentifier, currentElementDataModel);
}
```

### Example 3: Delta Updates

#### Before (5.2.4):
```csharp
public async Task UpdateExchange()
{
    // Update Data Exchange data with Delta
    var newRevision = await Client.RetrieveLatestExchangeDataAsync(currentExchangeData);
    
    // Create wrapper on existing ExchangeData
    var data = ElementDataModel.Create(Client, currentExchangeData);
    
    // Process updates...
}
```

#### After (6.2.0):
```csharp
public async Task UpdateExchange()
{
    // Update ElementDataModel with Delta
    var newRevision = await Client.RetrieveLatestElementDataModelAsync(currentElementDataModel);
    
    // Process updates directly on ElementDataModel...
}
```

---

## Common Pitfalls

### 1. **Direct ExchangeData Access**
**Problem:** Attempting to access `ExchangeData` properties directly
```csharp
// ❌ This will fail in 6.2.0
var exchangeId = someExchangeData.ExchangeID;
```
**Solution:** Use `ElementDataModel` properties
```csharp
// ✅ Correct approach
var exchangeId = elementDataModel.ExchangeID;
```

### 2. **Mixing Old and New Patterns**
**Problem:** Using both `ExchangeData` and `ElementDataModel` in the same workflow
```csharp
// ❌ Mixed pattern - avoid this
ExchangeData exchangeData = await Client.GetExchangeDataAsync(identifier);
ElementDataModel model = ElementDataModel.Create(Client, exchangeData);
```
**Solution:** Use `ElementDataModel` consistently
```csharp
// ✅ Consistent pattern
ElementDataModel model = await Client.GetElementDataModelAsync(identifier);
```

### 3. **Deprecated Method Usage**
**Problem:** Using deprecated methods that return `ExchangeData`
```csharp
// ❌ May be deprecated
var data = Client.GetExchangeDataSync(identifier);
```
**Solution:** Use new async methods that return `ElementDataModel`
```csharp
// ✅ Use new methods
var data = await Client.GetElementDataModelAsync(identifier);
```

### 4. **Event Handler Updates**
**Problem:** Event handlers expecting `ExchangeData` parameters
```csharp
// ❌ Old event signature
public void OnExchangeUpdated(ExchangeData data) { ... }
```
**Solution:** Update to expect `ElementDataModel`
```csharp
// ✅ New event signature
public void OnExchangeUpdated(ElementDataModel model) { ... }
```

### 5. **Null Reference Errors**
**Problem:** Assuming `ExchangeData` properties are available
```csharp
// ❌ May cause null reference
var id = elementDataModel.ExchangeData.ExchangeID;
```
**Solution:** Use direct `ElementDataModel` properties
```csharp
// ✅ Direct access
var id = elementDataModel.ExchangeID;
```

---

## Conclusion

The migration from DataExchange Connector 5.2.4 to 6.2.0 primarily involves transitioning from `ExchangeData` to `ElementDataModel` as the primary interface. This change provides:

- **Simplified API:** Fewer classes to manage
- **Better Encapsulation:** Internal details hidden from developers
- **Enhanced Functionality:** More robust methods and properties
- **Future-Proofing:** Aligned with long-term API evolution

### Next Steps

1. **Complete the Migration:** Follow all steps in this guide systematically
2. **Test Thoroughly:** Verify all functionality works as expected
3. **Review Documentation:** Consult the official API documentation for additional features
4. **Update Dependencies:** Ensure all related packages are compatible with 6.2.0
5. **Monitor for Updates:** Stay informed about future releases and deprecation notices

### Additional Resources

- [APS DataExchange SDK Documentation](https://aps.autodesk.com/en/docs/dx-sdk-beta/v1/developers_guide/overview/)
- [Autodesk Platform Services Developer Portal](https://aps.autodesk.com/)
- [Sample Code Repository](https://github.com/autodesk-platform-services/aps-dataexchange-connector)

For complex migration scenarios or specific technical questions, consult the official release notes and consider reaching out to Autodesk support channels.

---

*This migration guide provides general guidance for the transition from version 5.2.4 to 6.2.0. Always refer to the official documentation and release notes for the most accurate and up-to-date information.* 