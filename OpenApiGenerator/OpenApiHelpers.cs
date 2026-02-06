using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Game.Modules.unity.meta.service.Modules.WebProvider
{
    /// <summary>
    /// Helper utilities for OpenAPI code generation
    /// </summary>
    public static class OpenApiHelpers
    {
        /// <summary>
        /// Common content types supported by OpenAPI
        /// </summary>
        public static class ContentTypes
        {
            public const string ApplicationJson = "application/json";
            public const string ApplicationXml = "application/xml";
            public const string ApplicationFormUrlEncoded = "application/x-www-form-urlencoded";
            public const string MultipartFormData = "multipart/form-data";
            public const string TextPlain = "text/plain";
            public const string TextHtml = "text/html";
            public const string TextXml = "text/xml";
        }
        
        /// <summary>
        /// Mapping of Swagger types to C# types
        /// </summary>
        private static readonly Dictionary<string, string> TypeMapping = new Dictionary<string, string>
        {
            { "integer", "int" },
            { "number", "float" },
            { "boolean", "bool" },
            { "string", "string" },
            { "array", "List<{0}>" },
            { "object", "object" }
        };

        /// <summary>
        /// Mapping of Swagger formats to C# types (extended with email, uri, etc.)
        /// </summary>
        private static readonly Dictionary<string, string> FormatMapping = new Dictionary<string, string>
        {
            { "int32", "int" },
            { "int64", "long" },
            { "float", "float" },
            { "double", "double" },
            { "byte", "byte" },
            { "binary", "byte[]" },
            { "date", "DateTime" },
            { "date-time", "DateTime" },
            { "password", "string" },
            { "uuid", "Guid" },
            { "email", "string" },
            { "uri", "string" },
            { "uri-reference", "string" },
            { "uri-template", "string" },
            { "hostname", "string" },
            { "ipv4", "string" },
            { "ipv6", "string" }
        };

        /// <summary>
        /// Converts Swagger/OpenAPI type to C# type
        /// </summary>
        public static string MapTypeToCSharp(string type, string format)
        {
            if (string.IsNullOrEmpty(type))
                return "object";

            // Check format mapping first (more specific)
            if (!string.IsNullOrEmpty(format) && FormatMapping.ContainsKey(format))
                return FormatMapping[format];

            // Then check type mapping
            if (TypeMapping.ContainsKey(type))
                return TypeMapping[type];

            return "object";
        }

        /// <summary>
        /// Converts text to PascalCase
        /// </summary>
        public static string ToPascalCase(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Replace hyphens and underscores with spaces for splitting
            text = text.Replace('-', ' ').Replace('_', ' ');

            // Handle special characters
            var cleanText = Regex.Replace(text, "[^a-zA-Z0-9 ]", " ");

            // Split by spaces and make each part pascal case
            var parts = cleanText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (!string.IsNullOrEmpty(parts[i]))
                {
                    parts[i] = char.ToUpper(parts[i][0]) +
                             (parts[i].Length > 1 ? parts[i].Substring(1).ToLower() : "");
                }
            }

            return string.Join("", parts);
        }

        /// <summary>
        /// Cleans and normalizes operation IDs for contract class names
        /// </summary>
        public static string CleanOperationName(string operationId)
        {
            if (string.IsNullOrEmpty(operationId))
                return string.Empty;

            // Convert to PascalCase
            string pascalCase = ToPascalCase(operationId);

            // Common HTTP method prefixes
            string[] commonPrefixes = new[] { "get", "post", "put", "delete", "patch", "head", "options" };

            // Handle method prefixes (e.g., getClientProfile -> GetClientProfile)
            foreach (var prefix in commonPrefixes)
            {
                if (pascalCase.Length > prefix.Length &&
                    pascalCase.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    char.IsUpper(pascalCase[prefix.Length]))
                {
                    string prefixProperCase = char.ToUpperInvariant(prefix[0]) + prefix.Substring(1).ToLowerInvariant();
                    string restOfName = pascalCase.Substring(prefix.Length);

                    if (char.IsUpper(restOfName[0]))
                    {
                        pascalCase = prefixProperCase + restOfName;
                    }
                }
            }

            // Remove method suffixes (e.g., ClientProfileGet -> ClientProfile)
            foreach (var suffix in commonPrefixes)
            {
                string suffixProperCase = char.ToUpperInvariant(suffix[0]) + suffix.Substring(1).ToLowerInvariant();
                if (pascalCase.EndsWith(suffixProperCase))
                {
                    string withoutSuffix = pascalCase.Substring(0, pascalCase.Length - suffixProperCase.Length);
                    if (!string.IsNullOrEmpty(withoutSuffix))
                    {
                        pascalCase = withoutSuffix;
                    }
                }
            }

            // Ensure first letter is uppercase
            if (pascalCase.Length > 0 && !char.IsUpper(pascalCase[0]))
            {
                pascalCase = char.ToUpperInvariant(pascalCase[0]) + pascalCase.Substring(1);
            }

            return pascalCase;
        }

        /// <summary>
        /// Normalizes reference strings from OpenAPI/Swagger specs
        /// Handles both Swagger 2.0 (#/definitions/) and OpenAPI 3.0 (#/components/schemas/) formats
        /// </summary>
        public static string NormalizeReference(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                return reference;

            // Swagger 2.0 format
            if (reference.StartsWith("#/definitions/"))
            {
                return reference.Substring("#/definitions/".Length);
            }

            // OpenAPI 3.0 format
            if (reference.StartsWith("#/components/schemas/"))
            {
                return reference.Substring("#/components/schemas/".Length);
            }

            // Remove any version/query string information
            int versionIndex = reference.IndexOf('?');
            if (versionIndex >= 0)
            {
                reference = reference.Substring(0, versionIndex);
            }

            return reference;
        }

        /// <summary>
        /// Maps HTTP method names to WebRequestType enum values
        /// </summary>
        public static string GetWebRequestType(string method)
        {
            switch (method.ToUpper())
            {
                case "GET":
                    return "Get";
                case "POST":
                    return "Post";
                case "PUT":
                    return "Put";
                case "DELETE":
                    return "Delete";
                case "PATCH":
                    return "Patch";
                case "HEAD":
                    return "Head";
                case "OPTIONS":
                    return "Options";
                default:
                    return "None";
            }
        }

        /// <summary>
        /// Checks if content type is supported for code generation
        /// </summary>
        public static bool IsSupportedContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            // Remove charset and other parameters
            var cleanContentType = contentType.Split(';')[0].Trim().ToLower();

            return cleanContentType == ContentTypes.ApplicationJson ||
                   cleanContentType == ContentTypes.ApplicationXml ||
                   cleanContentType == ContentTypes.MultipartFormData ||
                   cleanContentType == ContentTypes.ApplicationFormUrlEncoded;
        }

        /// <summary>
        /// Gets the preferred content type from available options
        /// Prefers application/json if available
        /// </summary>
        public static string GetPreferredContentType(IEnumerable<string> contentTypes)
        {
            var typesList = new List<string>(contentTypes);

            // Prefer application/json
            if (typesList.Contains(ContentTypes.ApplicationJson))
                return ContentTypes.ApplicationJson;

            // Then application/xml
            if (typesList.Contains(ContentTypes.ApplicationXml))
                return ContentTypes.ApplicationXml;

            // Then form data
            if (typesList.Contains(ContentTypes.ApplicationFormUrlEncoded))
                return ContentTypes.ApplicationFormUrlEncoded;

            if (typesList.Contains(ContentTypes.MultipartFormData))
                return ContentTypes.MultipartFormData;

            // Return first supported type
            foreach (var contentType in typesList)
            {
                if (IsSupportedContentType(contentType))
                    return contentType;
            }

            // Default to application/json
            return ContentTypes.ApplicationJson;
        }

        /// <summary>
        /// Formats a description string for XML documentation
        /// </summary>
        public static string FormatXmlDocumentation(string description)
        {
            if (string.IsNullOrEmpty(description))
                return "Contract for REST API endpoint";

            // Replace line breaks with spaces
            description = description.Replace("\n", " ").Replace("\r", "");

            // Remove excessive whitespace
            description = Regex.Replace(description, @"\s+", " ").Trim();

            // Escape XML special characters
            description = description
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");

            return description;
        }

        /// <summary>
        /// Parses servers array from OpenAPI 3.0 specification
        /// Returns the base URL if servers are specified
        /// </summary>
        public static string ParseServersUrl(Newtonsoft.Json.Linq.JArray servers)
        {
            if (servers == null || servers.Count == 0)
                return null;

            // Get first server URL
            var firstServer = servers[0] as Newtonsoft.Json.Linq.JObject;
            return firstServer?["url"]?.ToString();
        }

        /// <summary>
        /// Checks if a schema represents a composition type (allOf, anyOf, oneOf)
        /// </summary>
        public static bool IsCompositionSchema(Newtonsoft.Json.Linq.JObject schema)
        {
            if (schema == null)
                return false;

            return schema["allOf"] != null ||
                   schema["anyOf"] != null ||
                   schema["oneOf"] != null;
        }

        /// <summary>
        /// Gets the composition type from schema
        /// </summary>
        public static string GetCompositionType(Newtonsoft.Json.Linq.JObject schema)
        {
            if (schema == null)
                return null;

            if (schema["allOf"] != null)
                return "allOf";
            if (schema["anyOf"] != null)
                return "anyOf";
            if (schema["oneOf"] != null)
                return "oneOf";

            return null;
        }
    }
}
