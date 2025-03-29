# unity.meta.backend

Game Meta Backend service provider

## Configuration

**pic with settings window ^^**

You can create configuration of module with menu: "Assets/UniGame/Meta Service/Create Configuration"


## Core Components

### Base Contract concept


Interface that must be implemented by all meta contracts. Provides:
- Contract validation
- Data transformation
- Meta information handling
- Contract lifecycle management

Available base abstract implementations:
- `RemoteCallContract<TInput, TOutput>` - Generic base contract for request/response pattern
- `RemoteCallContract<TInput>` - Base contract for requests without response data
- `RemoteCallContract<TOutput>` - Base contract for requests without input data
- `RemoteCallContract` - Base contract for requests without input/output data

Simple concrete implementations:
- `SimpleMetaContract<TInput, TOutput>` - Simple implementation of full request/response contract
- `SimpleInputContract<TInput>` - Simple implementation for requests without response data (inherits from `SimpleMetaContract<TInput, string>`)
- `SimpleOutputContract<TOutput>` - Simple implementation for requests without input data (inherits from `SimpleMetaContract<string, TOutput>`)

Example of inheritance:
```csharp
// Using abstract base contract
public class UserProfileContract : RemoteCallContract<UserProfileInput, UserProfileOutput>, IRemoteMetaContract
{
    // Implementation
}

// Using simple concrete contract
public class SimpleUserProfileContract : SimpleMetaContract<UserProfileInput, UserProfileOutput>
{
    // Implementation
}

// Request-only contract using simple implementation
public class SimpleUpdateStatusContract : SimpleInputContract<UpdateStatusInput>
{
    // Implementation
}

// Response-only contract using simple implementation
public class SimpleGetConfigContract : SimpleOutputContract<ConfigOutput>
{
    // Implementation
}

```

### WebProvider - REST API

Main component for working with REST API. Allows configuring base URL, headers, and request parameters.


For details data of WebRequest your contract can implement interface `IWebRequestContract`

**pic with rest config window**

### Mock WebProvider

Allows to mock requests and responses for testing purposes.

### BackendMetaService

Core service implementation that handles all backend communication. Provides methods for:

- Authentication and authorization
- Data synchronization
- Remote data management
- Error handling and retry logic

### BackendMetaSource

Configuration source for backend service. Supports multiple configuration sources:
- Unity ScriptableObject
- StreamingAssets
- Runtime configuration

## Setup and Configuration

### Web Provider - REST API

#### Settings

**pic with settings window ^^**

### StreamingAssets Json settings support

- enable option - "useStreamingSettings"
- save settings with button "Save Settings To Streaming Asset" on the Web Provider asset

now you can edit settings in StreamingAssets folder - "web_meta_provider_settings.json". 

when you initialize web provider it will load settings from StreamingAssets folder if it's enabled.

## Data Mapping and Configuration

### RemoteMetaDataConfig

Configuration system for remote data mapping. Supports:
- Custom data type mapping
- Field name mapping
- Validation rules
- Default values

### RemoteMetaId

System for handling remote entity IDs with support for:
- ID generation
- ID validation
- ID format conversion
- ID persistence

### JsonRemoteDataConverter

Built-in JSON converter for remote data with features:
- Custom serialization rules
- Type conversion
- Null handling
- Default value handling

## Usage Examples

### Creating a Request Contract

```csharp
[Serializable]
public class UserProfileContract : RemoteCallContract<UserProfileInput, UserProfileOutput>, IRemoteMetaContract
{
    public string userId;
    public string token;

    public void Validate()
    {
        if (string.IsNullOrEmpty(userId))
            throw new ValidationException("UserId cannot be empty");
    }

    public void OnBeforeSend()
    {
        // Prepare data before sending
    }

    public void OnAfterReceive()
    {
        // Process received data
    }
}

[Serializable]
public class UserProfileInput
{
    public string name;
    public int age;
}

[Serializable]
public class UserProfileOutput
{
    public string id;
    public string name;
    public int age;
    public string email;
}
```

### Executing a Request

```csharp
var contract = new UserProfileContract 
{
    userId = "123",
    token = "auth_token"
};

var result = await webProvider.CallAsync(contract);
```

### Using BackendMetaService

```csharp
// Initialize the service
var backendService = new BackendMetaService(config);

// Authenticate
await backendService.AuthenticateAsync(credentials);

// Sync data
await backendService.SyncDataAsync();

// Get remote data
var data = await backendService.GetRemoteDataAsync<RemoteDataType>();
```

## Error Handling

When errors occur during request execution, the service returns a `RemoteCallError` object:

```csharp
try 
{
    var result = await webProvider.CallAsync(contract);
}
catch (RemoteCallException ex)
{
    Debug.LogError($"Request error: {ex.Error.Message}");
    // Error handling
}
```

## Dynamic Url Path and Arguments

If you need to pass dynamic arguments to the url path, you can use the following syntax:

Demo Url: `api/store/{id}/{number}/{product}/buy`

When you just need to add into you contract field or property with the same name as the path argument.

```csharp
[Serializable]
public class DemoContract : RemoteCallContract<TInput, TOutput>
{
    public string id = "123";
    
    public int number = 65;
    
    public string Product { get; set; } = "demo_product";
    
    ...
}
```

result url: `api/store/123/65/demo_product/buy`

## Additional Features

### Caching
The service supports response caching for performance optimization. Caching configuration is available through WebProvider parameters.

### Retry Policy
Configurable retry policy for handling request failures.

### Timeouts
Ability to configure timeouts for different types of requests.

### Data Synchronization
Built-in support for data synchronization with features:
- Incremental sync
- Conflict resolution
- Sync status tracking
- Background sync

### Authentication
Comprehensive authentication system with support for:
- Token-based auth
- Session management
- Auto-refresh tokens
- Multiple auth providers

## Requirements

- Unity 2020.3 or higher
- .NET Framework 4.7.1 or higher
- Newtonsoft.Json for serialization/deserialization

## License

This module is licensed under the terms specified in the LICENSE file.

## API Contract Generation

The module provides functionality to automatically generate C# contracts from swagger.json API definitions.

### Using the API Contract Generator

1. Open the generator window from menu: **UniGame/Meta Service/Generate API Contracts**
2. Configure the following settings:
   - **Swagger JSON Path**: Path to your swagger.json file
   - **Output Folder**: Directory where contract files will be generated
   - **API URL Template**: Template for URL paths (default: `api/{0}`)
   - **API Allowed Paths** (optional): Filter to include only specific API paths
   - **Clean Up On Generate** (optional): If enabled, existing files in output folders will be deleted before generation
3. Click **Generate Contracts** button

### Generated Contracts

The generator creates C# contract classes implementing `IWebRequestContract` interface. Contracts are automatically mapped to the appropriate base contract type:

- `SimpleMetaContract<TInput, TOutput>`: For endpoints with both request and response data
- `SimpleInputContract<TInput>`: For endpoints with only request data
- `SimpleOutputContract<TOutput>`: For endpoints with only response data
- `RemoteCallContract`: For endpoints without request or response data

### Configuration via Code

You can also generate contracts programmatically:

```csharp
var settings = new WebApiSettings
{
    apiJsonPath = "path/to/swagger.json",
    contractsOutFolder = "Assets/Generated/Contracts/",
    apiTemplate = "api/{0}",
    apiAllowedPaths = new[] { "/client/" },
    cleanUpOnGenerate = true // Optional: clean up output folders before generation
};

ApiContractGenerator.GenerateContracts(settings);
```
