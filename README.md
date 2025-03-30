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

## Usage Examples

### Creating a Request Contract

```csharp
[Serializable]
public class UserProfileContract : RemoteCallContract<UserProfileInput, UserProfileOutput>, IRemoteMetaContract
{

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

- Configurable retry policy for handling request failures.
- Ability to configure timeouts for different types of requests.

## Requirements

- Unity 2020.3 or higher
- .NET Framework 4.7.1 or higher
- Newtonsoft.Json for serialization/deserialization

## License

This module is licensed under the terms specified in the LICENSE file.

## API Contract Generation

The module provides functionality to automatically generate C# contracts from Swagger 2.0 and OpenAPI 3.0 JSON definitions.

### Using the API Contract Generator

1. Open the generator window from menu: **UniGame/Meta Service/Generate API Contracts**
2. Configure the following settings:
   - **Swagger JSON Path**: Path to your Swagger/OpenAPI JSON file
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

### OpenAPI 3.0 Support

The generator fully supports OpenAPI 3.0 format with the following features:

- Handling `requestBody` section for POST/PUT/PATCH methods
- Processing content types from `content.application/json`
- Proper reference resolution from `#/components/schemas/`
- Support for schema titles in DTO naming
- Original property names preservation with `[JsonProperty]` attributes

Example of an OpenAPI 3.0 endpoint with requestBody:

```json
{
  "paths": {
    "/client/profile/currency": {
      "patch": {
        "operationId": "patchClientProfileCurrency",
        "requestBody": {
          "description": "Currency update data",
          "required": true,
          "content": {
            "application/json": {
              "schema": {
                "$ref": "#/components/schemas/UpdateCurrencyRequestDTO"
              }
            }
          }
        },
        "responses": {
          "200": {
            "description": "Success",
            "content": {
              "application/json": {
                "schema": {
                  "$ref": "#/components/schemas/UpdateCurrencyResponseDTO"
                }
              }
            }
          }
        }
      }
    }
  }
}
```

This will generate:

```csharp
public class PatchClientProfileCurrencyContract : RestContract<UpdateCurrencyRequestDTO, UpdateCurrencyResponseDTO>
{
    // Implementation
}
```

### Schema Title Support

If a schema in the API definition includes a `title` property, it will be used as the class name:

```json
"UpdateCurrencyRequestDTO": {
  "title": "ClientCurrencyUpdateRequest",
  "properties": {
    "currency_id": {
      "type": "string"
    }
  }
}
```

This will generate a class named `ClientCurrencyUpdateRequest` instead of `UpdateCurrencyRequestDTO`.

### Configuration via Code

You can also generate contracts programmatically:

```csharp
var settings = new WebApiSettings
{
    apiJsonPath = "path/to/openapi.json",
    contractsOutFolder = "Assets/Generated/Contracts/",
    dtoOutFolder = "Assets/Generated/Contracts/DTO/",
    ContractNamespace = "Game.Generated.WebContracts",
    apiTemplate = "api/{0}",
    apiAllowedPaths = new[] { "/client/" },
    cleanUpOnGenerate = true // Optional: clean up output folders before generation
};

ApiContractGenerator.GenerateContracts(settings);
```
