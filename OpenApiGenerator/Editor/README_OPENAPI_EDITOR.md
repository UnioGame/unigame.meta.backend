# OpenAPI Contract Generator - Editor Window

## Overview

The OpenAPI Contract Generator Editor Window provides a user-friendly interface for generating C# contracts and DTOs from Swagger/OpenAPI JSON specifications.

## Opening the Window

**Menu:** `UniGame > Meta Backend > OpenApi Generator`

## Features

### 1. **Settings Asset Management**
- Create new settings assets
- Select existing settings assets
- Settings are automatically saved near ContractsConfigurationAsset if available
- Default location: `Assets/UniGame.Generated/OpenApi/`

### 2. **API Specification Configuration**
- **Swagger/OpenAPI JSON:** Path to your API specification file
- **API URL Template:** Template for generating API paths (e.g., `api/{0}`)
- **Allowed Paths:** Filter which endpoints to generate (comma-separated list)

### 3. **Output Configuration**
- **Contracts Output Folder:** Where to generate contract classes
- **DTO Output Folder:** Where to generate Data Transfer Objects
- **Namespace:** Namespace for all generated classes

### 4. **Generation Options**
- **Clean Output Folders:** Delete existing files before generation
- **Use Response Data Container:** Enable if API wraps responses in a data field
- **Response Data Field Name:** Name of the data container field (default: "data")

## Usage Workflow

1. **Open the Editor Window**
   - Navigate to `UniGame > Meta Backend > OpenApi Generator`

2. **Configure or Load Settings**
   - Click "Create New Settings" to create a new configuration
   - Or click "Select Existing" to load an existing OpenApiSettingsAsset

3. **Configure API Source**
   - Click "Browse" next to Swagger/OpenAPI JSON to select your API spec file
   - Optionally configure allowed paths to filter endpoints

4. **Configure Output**
   - Set output folders for contracts and DTOs
   - Configure the namespace for generated code

5. **Set Generation Options**
   - Enable "Clean Output Folders" if you want fresh generation
   - Enable "Use Response Data Container" if your API wraps responses

6. **Save and Generate**
   - Click "Save Settings" to persist your configuration
   - Click "Generate Contracts" to create C# files

## Settings Persistence

Settings are saved in an `OpenApiSettingsAsset` ScriptableObject. By default, the editor tries to locate this asset near your `ContractsConfigurationAsset` for better organization.

## Generated Files

### Contracts
- Located in the Contracts Output Folder
- Named as `{OperationId}Contract.cs`
- Inherit from `RestContract<TInput, TOutput>` or `RestContract<TInput, TOutput, TError>`

### DTOs
- Located in the DTO Output Folder
- Named based on schema definitions or operation names
- Include JSON serialization attributes
- Support Unity serialization with `[field: SerializeField]`

## Tips

- Use **Allowed Paths** to generate only specific endpoints (e.g., "profile", "currency")
- Enable **Clean Output Folders** when making major API changes
- The **Response Data Container** option is useful for APIs that return:
  ```json
  {
    "data": { /* actual response */ }
  }
  ```

## Troubleshooting

### "API JSON file not found"
- Ensure the path to your Swagger/OpenAPI JSON file is correct
- Use the Browse button to select the file

### "No settings asset"
- Click "Create New Settings" to create a configuration
- Settings will be saved automatically

### Generated files have errors
- Check that your OpenAPI specification is valid
- Ensure namespace configuration is correct
- Verify output folders exist and are writable

## Integration with Meta Backend

The generated contracts integrate seamlessly with the Meta Backend system:
- Contracts can be registered in `ContractsConfigurationAsset`
- Use `UpdateRemoteMetaData()` to refresh contract list
- Monitor execution in the Meta Backend Editor Window
