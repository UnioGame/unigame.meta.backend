using System.Collections.Generic;

namespace Game.Modules.unity.meta.service.Modules.WebProvider
{
    /// <summary>
    /// Represents the entire Swagger API definition
    /// </summary>
    public class SwaggerApiDefinition
    {
        public string Title { get; set; }
        public string Version { get; set; }
        public string BasePath { get; set; }
        public Dictionary<string, SwaggerPathItem> Paths { get; set; } = new Dictionary<string, SwaggerPathItem>();
        public Dictionary<string, SwaggerDefinition> Definitions { get; set; } = new Dictionary<string, SwaggerDefinition>();
    }

    /// <summary>
    /// Represents a path in the Swagger API
    /// </summary>
    public class SwaggerPathItem
    {
        public Dictionary<string, SwaggerOperation> Methods { get; set; } = new Dictionary<string, SwaggerOperation>();
    }

    /// <summary>
    /// Represents an operation (HTTP method) in a path
    /// </summary>
    public class SwaggerOperation
    {
        public string OperationId { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public List<SwaggerParameter> Parameters { get; set; } = new List<SwaggerParameter>();
        public Dictionary<string, SwaggerResponse> Responses { get; set; } = new Dictionary<string, SwaggerResponse>();
    }

    /// <summary>
    /// Represents a parameter in an operation
    /// </summary>
    public class SwaggerParameter
    {
        public string Name { get; set; }
        public string In { get; set; } // path, query, header, body, formData
        public string Description { get; set; }
        public bool Required { get; set; }
        public string Type { get; set; }
        public string Format { get; set; }
        public SwaggerSchema Schema { get; set; }
    }

    /// <summary>
    /// Represents a response in an operation
    /// </summary>
    public class SwaggerResponse
    {
        public string Description { get; set; }
        public SwaggerSchema Schema { get; set; }
    }

    /// <summary>
    /// Represents a schema (data model)
    /// </summary>
    public class SwaggerSchema
    {
        public string Type { get; set; }
        public string Format { get; set; }
        public string Reference { get; set; }
        public SwaggerSchema Items { get; set; }
        public Dictionary<string, SwaggerSchema> Properties { get; set; }
    }

    /// <summary>
    /// Represents a definition (model) in the Swagger API
    /// </summary>
    public class SwaggerDefinition
    {
        public string Type { get; set; }
        public Dictionary<string, SwaggerProperty> Properties { get; set; } = new Dictionary<string, SwaggerProperty>();
        public List<string> Required { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a property in a definition
    /// </summary>
    public class SwaggerProperty
    {
        public string Type { get; set; }
        public string Format { get; set; }
        public string Description { get; set; }
        public string Reference { get; set; }
        public SwaggerProperty Items { get; set; }
    }
} 