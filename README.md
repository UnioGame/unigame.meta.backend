  # unity.meta.backend

Customizable Constract-based backend transport for Unity, supporting REST API, Mocking, Unity JavaScript bridge for WebGL

# Installation

You can install this module via Unity Package Manager by adding the following git URL to your `manifest.json` file:

```json
{
  "dependencies": {
    "com.unigame.metaservice": "https://github.com/UnioGame/unity.meta.backend.git"
}
```

![backend service](https://i.ibb.co/TMVcx1Cy/backend-service.png)

# Configuration

You can create configuration of module with menu: "Assets/UniGame/Meta Service/Create Configuration"

![backend command](https://i.ibb.co/SDdWttMK/create-menu-backend.png)

### StreamingAssets Json settings support

- enable option - "useStreamingSettings"
- save settings with button "Save Settings To Streaming Asset" on the Web Provider asset

now you can edit settings in StreamingAssets folder - "web_meta_provider_settings.json". 

when you initialize web provider it will load settings from StreamingAssets folder if it's enabled.

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

//or just
var result = await contract.ExecuteAsync();;
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

### WebProvider - REST API

Main component for working with REST API. Allows configuring base URL, headers, and request parameters.


For details data of WebRequest your contract can implement interface `IWebRequestContract`

![rest api settings](https://i.ibb.co/vCCTW1Mh/restapi-settings.png)

![rest api contracts](https://i.ibb.co/VW8Y5ZY5/restapi-settings2.png)


### Mock WebProvider

Allows to mock requests and responses for testing purposes.

![mock data](https://i.ibb.co/zVkFRJ2j/mock-settings.png)

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

### Response Data Container Support

Some APIs wrap all response data in a container object with a specific field (commonly `data`). The generator supports this pattern:

```json
{
  "data": {
    "id": 5,
    "name": "John",
    "email": "john@example.com"
  }
}
```

To enable support for this format:

1. Set `useResponseDataContainer = true` in the WebApiSettings
2. Specify the field name containing the actual data (default is "data")

```csharp
var settings = new WebApiSettings
{
    // Other settings...
    useResponseDataContainer = true,
    responseDataField = "data" // The field name in the response container
};
```

When enabled, the generator will:
1. Create a generic `ResponseDataDTO<T>` wrapper class
2. Use the wrapper directly in contract signatures
3. Add appropriate `[JsonProperty]` attributes to map the wrapper properties

The resulting contract will look like:

```csharp
// Contract includes the ResponseDataDTO wrapper directly in its signature
public class GetUserProfileContract : RestContract<GetUserProfileInput, ResponseDataDTO<UserProfileDTO>>
{
    /// <summary>
    /// The API path for this request
    /// </summary>
    public override string Path => "api/user/profile";

    /// <summary>
    /// The type of request
    /// </summary>
    public override WebRequestType RequestType => WebRequestType.Get;
}
```

This approach ensures that the generated contracts match exactly the structure of the API responses, making it clear to developers that the data is wrapped in a container.

### Error Type Support

The generator now supports error types in contracts. If an API operation defines error responses (status codes 4XX or 5XX) with a schema, the generator will include this error type as a third type parameter in the contract:

```csharp
// Contract with error type
public class GetUserProfileContract : RestContract<GetUserProfileInput, UserProfileDTO, ErrorResponseDTO>
{
    /// <summary>
    /// The API path for this request
    /// </summary>
    public override string Path => "api/user/profile";

    /// <summary>
    /// The type of request
    /// </summary>
    public override WebRequestType RequestType => WebRequestType.Get;
}
```

The generator determines the error type by:
1. Looking for response status codes starting with 4 or 5 (client and server errors)
2. Checking if these responses have a schema defined
3. Using the schema reference as the error type, or generating a custom error DTO if needed

This feature allows for more comprehensive error handling in API client code, as the contract explicitly defines the expected error type.
