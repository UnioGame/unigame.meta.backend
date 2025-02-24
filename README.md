# unity.meta.backend

Game Meta Backend service provider


# Web Provider - REST API

## Settings

**pic with settings window ^^**

### StreamingAssets Json settings support

- enable option - "useStreamingSettings"
- save settings with button "Save Settings To Streaming Asset" on the Web Provider asset

now you can edit settings in StreamingAssets folder - "web_meta_provider_settings.json". 

when you initialize web provider it will load settings from StreamingAssets folder if it's enabled.

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

