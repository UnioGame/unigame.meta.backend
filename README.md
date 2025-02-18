# unity.meta.backend

Game Meta Backend service provider


# Web Provider - REST API

## Dynamic Url Path and Arguments

If you need to pass dynamic arguments to the url path, you can use the following syntax:

Demo Url: `api/store/{id}/{product}/buy`

When you just need to add into you contract field or property with the same name as the path argument.

```csharp

[Serializable]
public class DemoContract : RemoteCallContract<TInput, TOutput>
{
    public string id = "123";
    
    public string Product { get; set; } = "demo_product";
    
    ...
}

```

result url: `api/store/123/demo_product/buy`