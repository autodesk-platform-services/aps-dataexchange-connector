# DataExchange Connector Migration Guide
## From 7.2.0 to 7.2.1-beta

### 🔄 Migration Guide: SDK 7.2.1-beta Upgrade

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
