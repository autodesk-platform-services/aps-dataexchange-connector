# DataExchange Connector Migration Guide
## From 6.3.0 to 7.1.0

### 🔄 Migration Guide: SDK 7.1.0 Upgrade

This section documents the migration from SDK 6.3.0 to **Autodesk Data Exchange SDK 7.1.0**.

### 📋 Overview of Changes

This is a major upgrade that includes significant API improvements, project modernization, and enhanced configuration requirements:

- **SDK Version**: Upgraded to `Autodesk.DataExchange 7.1.0`
- **SDK Version**: Upgraded to `Autodesk.DataExchange.UI 7.1.0`
- **Project Modernization**: Migration from `packages.config` to PackageReference format
- **Enhanced API Methods**: Streamlined geometry creation method signatures
- **Configuration Requirements**: Several configuration parameters are now mandatory
- **Authentication Improvements**: Simplified PKCE authentication flow

### 🚀 Key Dependency Updates

| Package | Previous Version | New Version | Impact |
|---------|------------------|-------------|---------|
| `Autodesk.DataExchange` | `6.3.0-beta` | `7.1.0` | **Major** - Core SDK upgrade with API streamlining |
| `Autodesk.DataExchange.UI` | `6.3.0-beta` | `7.1.0` | **Major** - UI SDK upgrade |

### ⚠️ Breaking Changes

#### 1. ElementDataModel Geometry Creation API Simplification

The geometry creation methods have been simplified by removing the `GeometryProperties` wrapper class.

**Before (SDK 6.3.0):**

```csharp
// File geometry with GeometryProperties wrapper
ElementDataModel.CreateFileGeometry(new GeometryProperties(filePath, renderStyle));

// Mesh geometry with GeometryProperties wrapper  
ElementDataModel.CreateMeshGeometry(new GeometryProperties(meshObject, "MeshName"));

// Primitive geometry with GeometryProperties wrapper
ElementDataModel.CreatePrimitiveGeometry(new GeometryProperties(geometryContainer, renderStyle));
```

**After (SDK 7.1.0):**

```csharp
// File geometry with explicit format parameter
ElementDataModel.CreateFileGeometry(filePath, GeometryFormat.Step, renderStyle);
ElementDataModel.CreateFileGeometry(filePath, GeometryFormat.Ifc, renderStyle);
ElementDataModel.CreateFileGeometry(filePath, GeometryFormat.Obj, renderStyle);

// Mesh geometry with direct parameters
ElementDataModel.CreateMeshGeometry(meshObject, "MeshName");

// Primitive geometry with direct parameters  
ElementDataModel.CreatePrimitiveGeometry(geometryContainer, renderStyle);
```

**Migration Action:** Remove all `GeometryProperties` wrapper usage and pass parameters directly to the geometry creation methods. For file geometry, explicitly specify the `GeometryFormat` enum value.

#### 2. Required Configuration Parameters

Several configuration parameters that were previously optional are now **mandatory**.

**Before (SDK 6.3.0):**

```xml
<!-- These were optional -->
<add key="ConnectorName" value="My Connector" />           <!-- ❌ Optional -->
<add key="ConnectorVersion" value="1.0.0" />               <!-- ❌ Optional -->  
<add key="HostApplicationName" value="My Host App" />       <!-- ❌ Optional -->
<add key="HostApplicationVersion" value="2.0.0" />         <!-- ❌ Optional -->
```

**After (SDK 7.1.0):**

```xml
<!-- These are now required -->
<add key="ConnectorName" value="My Connector" />           <!-- ✅ Required -->
<add key="ConnectorVersion" value="1.0.0" />               <!-- ✅ Required -->
<add key="HostApplicationName" value="My Host App" />       <!-- ✅ Required -->
<add key="HostApplicationVersion" value="2.0.0" />         <!-- ✅ Required -->
```

**Migration Action:** Ensure all four configuration parameters (`ConnectorName`, `ConnectorVersion`, `HostApplicationName`, `HostApplicationVersion`) are defined in your App.config files. The application will throw configuration errors if these are missing.

#### 3. Project System Modernization

The project has been modernized from `packages.config` to PackageReference format.

**Before (SDK 6.3.0):**

```xml
<!-- packages.config file -->
<packages>
  <package id="Autodesk.DataExchange" version="6.3.0-beta" targetFramework="net48" />
  <package id="Autodesk.DataExchange.UI" version="6.3.0-beta" targetFramework="net48" />
  <!-- Many explicit package references... -->
</packages>

<!-- .csproj with explicit assembly references -->
<ItemGroup>
  <Reference Include="Autodesk.DataExchange">
    <HintPath>packages\Autodesk.DataExchange.6.3.0-beta\lib\net48\Autodesk.DataExchange.dll</HintPath>
  </Reference>
  <!-- Many explicit assembly references... -->
</ItemGroup>
```

**After (SDK 7.1.0):**

```xml
<!-- No packages.config file needed -->

<!-- .csproj with PackageReference -->
<ItemGroup>
  <PackageReference Include="Autodesk.DataExchange" Version="7.1.0">
    <IncludeAssets>all</IncludeAssets>
    <ExcludeAssets>runtime; build; native; contentfiles; analyzers</ExcludeAssets>
  </PackageReference>
  <PackageReference Include="Autodesk.DataExchange.UI" Version="7.1.0" />
  <!-- Dependencies resolved automatically -->
</ItemGroup>
```

**Migration Action:** Delete `packages.config` files and convert explicit assembly references to PackageReference format in your .csproj files.

#### 4. Authentication and Initialization Changes

The client initialization and authentication flow has been simplified, particularly for PKCE authentication.

**Before (SDK 6.3.0):**

```csharp
// Complex initialization with potential client secret
this.sdkOptions = new SDKOptionsDefaultSetup()
{
    CallBack = authCallback,
    ClientId = authClientId,
    ClientSecret = authClientSecret,  // May have been used
    ConnectorName = connectorName,
    ConnectorVersion = connectorVersion,
    HostApplicationName = hostApplicationName,
    HostApplicationVersion = hostApplicationVersion,
};

// Basic configuration validation
var client = new Autodesk.DataExchange.Client(this.sdkOptions);
```

**After (SDK 7.1.0):**

```csharp
// Enhanced validation for required parameters
if (string.IsNullOrEmpty(authClientId))
{
    throw new ConfigurationErrorsException("AuthClientId is missing from App.config.");
}

if (string.IsNullOrEmpty(authCallback))
{
    throw new ConfigurationErrorsException("AuthCallback is missing from App.config.");
}

if (!authCallback.EndsWith("/"))
{
    throw new ConfigurationErrorsException("AuthCallback URL must end with a trailing slash '/'.");
}

if (string.IsNullOrEmpty(connectorName) || string.IsNullOrEmpty(connectorVersion) ||
    string.IsNullOrEmpty(hostApplicationName) || string.IsNullOrEmpty(hostApplicationVersion))
{
    throw new ConfigurationErrorsException("ConnectorName, ConnectorVersion, HostApplicationName, and HostApplicationVersion are required in App.config.");
}

// PKCE flow - no client secret needed
this.sdkOptions = new SDKOptionsDefaultSetup()
{
    CallBack = authCallback,
    ClientId = authClientId,
    // ClientSecret removed for PKCE flow
    ConnectorName = connectorName,
    ConnectorVersion = connectorVersion,
    HostApplicationName = hostApplicationName,
    HostApplicationVersion = hostApplicationVersion,
};

this.client = new Client(this.sdkOptions);
```

**Migration Action:** Remove `ClientSecret` usage for PKCE authentication and add comprehensive configuration validation as shown above.

### 🔧 Migration Steps

#### Step 1: Update Package References

**For existing projects using packages.config:**

1. Delete the `packages.config` file
2. Remove all explicit assembly references from your .csproj file
3. Add PackageReference entries:

```xml
<ItemGroup>
  <PackageReference Include="Autodesk.DataExchange" Version="7.1.0">
    <IncludeAssets>all</IncludeAssets>
    <ExcludeAssets>runtime; build; native; contentfiles; analyzers</ExcludeAssets>
  </PackageReference>
  <PackageReference Include="Autodesk.DataExchange.UI" Version="7.1.0" />
</ItemGroup>
```

#### Step 2: Update Required Configuration

Ensure your App.config files contain all required settings:

```xml
<configuration>
  <appSettings>
    <add key="AuthClientId" value="YOUR_CLIENT_ID" />
    <add key="AuthCallback" value="http://127.0.0.1:63212/" />
    
    <!-- These are now REQUIRED -->
    <add key="ConnectorName" value="Your Connector Name" />
    <add key="ConnectorVersion" value="1.0.0" />
    <add key="HostApplicationName" value="Your Host Application" />
    <add key="HostApplicationVersion" value="2.0.0" />
    
    <!-- Optional -->
    <add key="LogLevel" value="Info" />
  </appSettings>
</configuration>
```

#### Step 3: Update Geometry Creation Calls

Replace all `GeometryProperties` wrapper usage:

```csharp
// OLD: Using GeometryProperties wrapper
ElementDataModel.CreateFileGeometry(new GeometryProperties(filePath, CommonRenderStyle))
ElementDataModel.CreateMeshGeometry(new GeometryProperties(meshObject, "MeshName"))
ElementDataModel.CreatePrimitiveGeometry(new GeometryProperties(geomContainer, commonRenderStyle))

// NEW: Direct parameter passing
ElementDataModel.CreateFileGeometry(filePath, GeometryFormat.Step, CommonRenderStyle)
ElementDataModel.CreateMeshGeometry(meshObject, "MeshName")
ElementDataModel.CreatePrimitiveGeometry(geomContainer, commonRenderStyle)
```

**File format mapping:**
- `.stp` or `.step` files → `GeometryFormat.Step`
- `.ifc` files → `GeometryFormat.Ifc`  
- `.obj` files → `GeometryFormat.Obj`

#### Step 4: Update Client Initialization

Add comprehensive configuration validation:

```csharp
private void InitializeConnector()
{
    // Read configuration
    var authClientId = ConfigurationManager.AppSettings["AuthClientId"];
    var authCallback = ConfigurationManager.AppSettings["AuthCallback"];
    var connectorName = ConfigurationManager.AppSettings["ConnectorName"];
    var connectorVersion = ConfigurationManager.AppSettings["ConnectorVersion"];
    var hostApplicationName = ConfigurationManager.AppSettings["HostApplicationName"];
    var hostApplicationVersion = ConfigurationManager.AppSettings["HostApplicationVersion"];

    // Validate required configuration
    if (string.IsNullOrEmpty(authClientId))
    {
        throw new ConfigurationErrorsException("AuthClientId is missing from App.config. Please ensure the config file is properly configured.");
    }

    if (string.IsNullOrEmpty(authCallback))
    {
        throw new ConfigurationErrorsException("AuthCallback is missing from App.config. Please ensure the config file is properly configured.");
    }

    if (!authCallback.EndsWith("/"))
    {
        throw new ConfigurationErrorsException("AuthCallback URL must end with a trailing slash '/'. Example: http://127.0.0.1:63212/");
    }

    if (string.IsNullOrEmpty(connectorName) || string.IsNullOrEmpty(connectorVersion) ||
        string.IsNullOrEmpty(hostApplicationName) || string.IsNullOrEmpty(hostApplicationVersion))
    {
        throw new ConfigurationErrorsException("ConnectorName, ConnectorVersion, HostApplicationName, and HostApplicationVersion are required in App.config.");
    }

    // Create SDK options (PKCE flow - no client secret)
    this.sdkOptions = new SDKOptionsDefaultSetup()
    {
        CallBack = authCallback,
        ClientId = authClientId,
        ConnectorName = connectorName,
        ConnectorVersion = connectorVersion,
        HostApplicationName = hostApplicationName,
        HostApplicationVersion = hostApplicationVersion,
    };

    // Create the Client
    this.client = new Client(this.sdkOptions);
    
    // Rest of initialization...
}
```

#### Step 5: Test Your Changes

1. Build the project and resolve any compilation errors
2. Run the application and verify authentication works correctly
3. Test geometry creation operations to ensure they work with the new API
4. Verify that all required configuration parameters are being validated properly

### 🎯 New Features & Improvements

#### Simplified Geometry API
- **Direct Parameter Passing**: Removed the need for `GeometryProperties` wrapper class
- **Explicit Format Specification**: Clear specification of file geometry formats
- **Reduced Boilerplate**: Less code required for geometry creation operations
- **Better Type Safety**: Compile-time validation of geometry format parameters

#### Enhanced Configuration Management
- **Mandatory Parameters**: Important connector metadata is now required
- **Better Validation**: Comprehensive validation with helpful error messages
- **PKCE Authentication**: Simplified OAuth2 flow without client secrets
- **Improved Error Handling**: Clear configuration error messages

#### Modern Project System
- **PackageReference Format**: Modern NuGet package management
- **Automatic Dependency Resolution**: No need to manually manage transitive dependencies
- **Simplified Project Files**: Cleaner, more maintainable project structure
- **Better Tooling Support**: Enhanced Visual Studio and build system integration

#### Performance & Reliability
- **Streamlined API**: Reduced object allocation in geometry operations
- **Enhanced Authentication Flow**: More robust PKCE implementation
- **Better Error Messages**: More descriptive error messages for troubleshooting
- **Improved Validation**: Proactive validation prevents runtime issues

### 🚨 Common Migration Issues

#### 1. **Configuration Errors on Startup**
**Problem:** Application fails to start with configuration errors
```csharp
// ❌ This will cause startup failure
ConfigurationErrorsException: "ConnectorName, ConnectorVersion, HostApplicationName, and HostApplicationVersion are required in App.config."
```
**Solution:** Add all required configuration parameters to your App.config
```xml
<!-- ✅ Add these required settings -->
<add key="ConnectorName" value="Your Connector Name" />
<add key="ConnectorVersion" value="1.0.0" />
<add key="HostApplicationName" value="Your Host Application" />
<add key="HostApplicationVersion" value="2.0.0" />
```

#### 2. **GeometryProperties Compilation Errors**
**Problem:** Compilation errors due to removed `GeometryProperties` class
```csharp
// ❌ This will cause compilation error in 7.1.0
ElementDataModel.CreateFileGeometry(new GeometryProperties(filePath, style));
```
**Solution:** Use direct parameter passing
```csharp
// ✅ Correct approach
ElementDataModel.CreateFileGeometry(filePath, GeometryFormat.Step, style);
```

#### 3. **PackageReference Migration Issues**
**Problem:** Build errors due to conflicting package management approaches
```xml
<!-- ❌ Don't mix packages.config with PackageReference -->
<packages>
  <package id="Autodesk.DataExchange" version="6.3.0-beta" />
</packages>
<!-- AND -->
<PackageReference Include="Autodesk.DataExchange" Version="7.1.0" />
```
**Solution:** Use only PackageReference approach
```xml
<!-- ✅ Use only PackageReference -->
<PackageReference Include="Autodesk.DataExchange" Version="7.1.0" />
```

#### 4. **Authentication Callback URL Format**
**Problem:** Authentication fails due to incorrect callback URL format
```csharp
// ❌ Missing trailing slash
<add key="AuthCallback" value="http://127.0.0.1:63212" />
```
**Solution:** Ensure callback URL ends with trailing slash
```csharp
// ✅ Correct format with trailing slash
<add key="AuthCallback" value="http://127.0.0.1:63212/" />
```

### 📚 Additional Resources

- [APS DataExchange SDK Documentation](https://aps.autodesk.com/en/docs/dx-sdk/v1/developers_guide/overview/)
- [APS DataExchange Release Notes](https://aps.autodesk.com/en/docs/dx-sdk/v1/developers_guide/release_notes/)
- [Autodesk Platform Services Developer Portal](https://aps.autodesk.com/)
- [DataExchange API Reference](https://aps.autodesk.com/en/docs/dx-sdk/v1/reference/)
- [Sample Code Repository](https://github.com/autodesk-platform-services/aps-dataexchange-connector)

For complex migration scenarios or specific technical questions, consult the official release notes and consider reaching out to Autodesk support channels.

---

*This migration guide provides comprehensive guidance for the transition from version 6.3.0 to 7.1.0. Always refer to the official documentation and release notes for the most accurate and up-to-date information.*