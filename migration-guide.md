# DataExchange Connector Migration Guide
## From 7.1.0 to 7.2.0-beta

### 🔄 Migration Guide: SDK 7.2.0-beta Upgrade

This section documents the migration from SDK 7.1.0 to **Autodesk Data Exchange SDK 7.2.0-beta**.

### 📋 Overview of Changes

This is a minor upgrade that includes bug fixes and a small number of API removals:

- **SDK Version**: Upgraded to `Autodesk.DataExchange 7.2.0-beta`
- **SDK Version**: Upgraded to `Autodesk.DataExchange.UI 7.2.0-beta`
- **Bug Fixes**: Various stability and reliability improvements
- **API Removals**: `SkippedElementType.Miscellaneous` and `SetProgressMessage` have been removed

### 🚀 Key Dependency Updates

| Package | Previous Version | New Version | Impact |
|---------|------------------|-------------|---------|
| `Autodesk.DataExchange` | `7.1.0` | `7.2.0-beta` | **Minor** - Bug fixes and API removals |
| `Autodesk.DataExchange.UI` | `7.1.0` | `7.2.0-beta` | **Minor** - Bug fixes and API removals |

### ⚠️ Breaking Changes

#### 1. Removal of SkippedElementType.Miscellaneous

The `SkippedElementType.Miscellaneous` enum value has been removed from the SDK.

**Before (SDK 7.1.0):**

```csharp
// Using SkippedElementType.Miscellaneous to categorize skipped elements
element.SkippedElementType = SkippedElementType.Miscellaneous;
```

**After (SDK 7.2.0-beta):**

```csharp
// SkippedElementType.Miscellaneous is no longer available.
// Use a more specific SkippedElementType value instead.
element.SkippedElementType = SkippedElementType.OtherSpecificType;
```

**Migration Action:** Search your codebase for any usage of `SkippedElementType.Miscellaneous` and replace it with a more specific `SkippedElementType` value that accurately describes why the element was skipped.

#### 2. Removal of SetProgressMessage

The `SetProgressMessage` method has been removed from the SDK.

**Before (SDK 7.1.0):**

```csharp
// Setting a progress message during exchange operations
SetProgressMessage("Processing element...");
```

**After (SDK 7.2.0-beta):**

```csharp
// SetProgressMessage is no longer available.
// Remove all calls to SetProgressMessage from your code.
```

**Migration Action:** Search your codebase for any calls to `SetProgressMessage` and remove them. If you need to report progress, consult the SDK documentation for alternative approaches available in 7.2.0-beta.

### 🔧 Migration Steps

#### Step 1: Update Package References

Update the version numbers in your .csproj file:

**Before:**

```xml
<ItemGroup>
  <PackageReference Include="Autodesk.DataExchange" Version="7.1.0">
    <IncludeAssets>all</IncludeAssets>
    <ExcludeAssets>runtime; build; native; contentfiles; analyzers</ExcludeAssets>
  </PackageReference>
  <PackageReference Include="Autodesk.DataExchange.UI" Version="7.1.0" />
</ItemGroup>
```

**After:**

```xml
<ItemGroup>
  <PackageReference Include="Autodesk.DataExchange" Version="7.2.0-beta">
    <IncludeAssets>all</IncludeAssets>
    <ExcludeAssets>runtime; build; native; contentfiles; analyzers</ExcludeAssets>
  </PackageReference>
  <PackageReference Include="Autodesk.DataExchange.UI" Version="7.2.0-beta" />
</ItemGroup>
```

#### Step 2: Remove SkippedElementType.Miscellaneous Usage

1. Search your codebase for `SkippedElementType.Miscellaneous`
2. Replace each occurrence with a more specific `SkippedElementType` value

#### Step 3: Remove SetProgressMessage Calls

1. Search your codebase for `SetProgressMessage`
2. Remove all calls to this method

#### Step 4: Test Your Changes

1. Build the project and resolve any compilation errors
2. Run the application and verify all functionality works correctly
3. Confirm that no references to `SkippedElementType.Miscellaneous` or `SetProgressMessage` remain

### 🎯 Improvements

#### Bug Fixes
- Various stability and reliability improvements across the SDK
- See the official release notes for a detailed list of resolved issues

### 🚨 Common Migration Issues

#### 1. **Compilation Error: SkippedElementType.Miscellaneous**
**Problem:** Compilation fails because `SkippedElementType.Miscellaneous` no longer exists
```csharp
// ❌ This will cause a compilation error in 7.2.0-beta
element.SkippedElementType = SkippedElementType.Miscellaneous;
```
**Solution:** Use a more specific `SkippedElementType` value
```csharp
// ✅ Use an appropriate specific type
element.SkippedElementType = SkippedElementType.OtherSpecificType;
```

#### 2. **Compilation Error: SetProgressMessage**
**Problem:** Compilation fails because `SetProgressMessage` has been removed
```csharp
// ❌ This will cause a compilation error in 7.2.0-beta
SetProgressMessage("Processing...");
```
**Solution:** Remove the call entirely
```csharp
// ✅ Simply remove the call
// (SetProgressMessage is no longer available)
```

### 📚 Additional Resources

- [APS DataExchange SDK Documentation](https://aps.autodesk.com/en/docs/dx-sdk/v1/developers_guide/overview/)
- [APS DataExchange Release Notes](https://aps.autodesk.com/en/docs/dx-sdk/v1/developers_guide/release_notes/)
- [Autodesk Platform Services Developer Portal](https://aps.autodesk.com/)
- [DataExchange API Reference](https://aps.autodesk.com/en/docs/dx-sdk/v1/reference/)
- [Sample Code Repository](https://github.com/autodesk-platform-services/aps-dataexchange-connector)

For complex migration scenarios or specific technical questions, consult the official release notes and consider reaching out to Autodesk support channels.

---

*This migration guide provides guidance for the transition from version 7.1.0 to 7.2.0-beta. Always refer to the official documentation and release notes for the most accurate and up-to-date information.*
