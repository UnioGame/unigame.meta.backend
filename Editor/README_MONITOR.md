# Meta Backend Contract Monitor

Editor for monitoring and debugging meta-backend contract execution in Unity.

## Opening the Editor Window

Menu: `UniGame > Meta Backend > Meta Editor Window`

## Features

### 1. Configuration Display

**Left Panel** shows the loaded contract configuration:
- List of all registered contracts
- Contract method
- Contract ID
- Input/output data types
- Provider (if overridden)
- Status (enabled/disabled)

Contracts with a green left border are active, red border indicates disabled.

### 2. Contract Execution History

**Right Upper Panel** displays the history of executed contracts:
- History record number
- Execution time
- Meta ID of the contract
- Status (Success/Failed)
- Result type
- Result data or error text
- Result hash

**Search**: Use the text field to filter history by:
- Meta ID
- Result type
- Error text

### 3. Real-time Stream

**Right Lower Panel** shows the event stream in real-time:
- Automatic updates when new contracts execute
- Last 50 records
- Connection status indicator (green = connected, red = disconnected)

Each record contains:
- Meta ID
- Precise time with milliseconds
- Execution status
- Result preview (up to 200 characters)

## Controls

### Header (top panel)

- **Auto Refresh**: Automatic data refresh every 500ms (enabled by default)
- **Refresh**: Manual refresh of all data
- **Clear Stream**: Clear real-time stream history
- **Export History**: Export history to JSON file

## Runtime Usage

The editor automatically connects to `BackendMetaService.EditorInstance` when entering Play Mode.

### Requirements:
1. An instance of `BackendMetaService` must be created in Play Mode
2. This instance must be assigned to `BackendMetaService.EditorInstance`

Example:
```csharp
var service = new BackendMetaService(...);
#if UNITY_EDITOR
BackendMetaService.EditorInstance = service;
#endif
```

The editor subscribes to:
- `BackendMetaService.ContractHistory` - for displaying full history
- `BackendMetaService.DataStream` - for real-time updates

## Technical Details

### Refresh Rate
- Auto Refresh: 500 milliseconds
- Updates only occur when Auto Refresh is enabled
- Real-time stream updates instantly via Observable subscription

### Limitations
- Contract history: displays last 100 records
- Real-time stream: stores last 50 records
- Configuration: displays first 100 contracts

### Data Formatting
- Results are serialized to JSON with indentation for better readability
- Long results are truncated (500 characters for history, 200 for stream)
- Time is displayed in local timezone

## UI Toolkit

The editor is built on UI Toolkit using:
- **UXML**: `MetaBackendEditorWindow.uxml` - UI structure
- **USS**: `MetaBackendEditorWindow.uss` - styles
- **C#**: `MetaBackendEditorWindow.cs` - logic

### Style Customization

You can modify the editor's appearance by editing the `MetaBackendEditorWindow.uss` file:
- Colors for success/error states
- Font sizes
- Padding and border radius
- Panel background colors

## Troubleshooting

### "Service not available"
Start Play Mode to activate BackendMetaService.

### "No configuration loaded"
Ensure there is a ScriptableObject with `BackendMetaConfiguration` in the project.

### "Failed to load MetaBackendEditorWindow.uxml"
The UXML file should be located in the same folder as the editor script.

### Stream not updating
1. Check that Auto Refresh is enabled
2. Verify that contracts are actually being executed
3. Check the console for DataStream subscription errors
