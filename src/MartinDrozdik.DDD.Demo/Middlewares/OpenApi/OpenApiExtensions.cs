using Microsoft.AspNetCore.OpenApi;

namespace MartinDrozdik.DDD.Demo.Middlewares.OpenApi;

public static class OpenApiExtensions
{
    /// <summary>
    /// Customizes schema IDs using the provided transformer function.
    /// </summary>
    /// <remarks>Inspiration: https://azuregems.io/net9-openapi-fulltypenames/</remarks>
    /// <param name="config">The OpenApi options to transform.</param>
    /// <param name="typeSchemaTransformer">The name transformer.</param>
    /// <returns>Updated <see cref="OpenApiOptions"/>.</returns>
    public static OpenApiOptions CustomSchemaIds(this OpenApiOptions config,
        Func<Type, string?> typeSchemaTransformer)
    {
        return config.AddSchemaTransformer((schema, context, _) =>
        {
            // Skip value types and strings
            if (context.JsonTypeInfo.Type.IsValueType || context.JsonTypeInfo.Type == typeof(string))
            {
                return Task.CompletedTask;
            }

            // Skip if the schema ID is not already set because we don't want to decorate the schema multiple times
            if (schema.Metadata == null || !schema.Metadata.TryGetValue("x-schema-id", out var _))
            {
                return Task.CompletedTask;
            }

            // Transform the typename based on the provided delegate
            var transformedTypeName = typeSchemaTransformer(context.JsonTypeInfo.Type);
            if (string.IsNullOrEmpty(transformedTypeName))
            {
                return Task.CompletedTask;
            }

            // Scalar - decorate the models section
            schema.Metadata["x-schema-id"] = transformedTypeName;

            // Swagger and Scalar specific:
            // for Scalar - decorate the endpoint section
            // for Swagger - decorate the endpoint and model sections
            schema.Title = transformedTypeName;

            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Customizes schema IDs to use full type names and inherit nested type names recursively.
    /// </summary>
    /// <param name="config">The OpenApi options to transform.</param>
    /// <returns>Updated <see cref="OpenApiOptions"/>.</returns>
    public static OpenApiOptions ParentDeclarationSchemaIds(this OpenApiOptions config)
    {
        // Recursively gather declaring parent types
        static string GatherDeclaringTypes(Type type)
        {
            if (type.DeclaringType == null)
            {
                return type.Name;
            }

            return $"{GatherDeclaringTypes(type.DeclaringType)}{type.Name}";
        }

        return config.CustomSchemaIds(GatherDeclaringTypes);
    }
}
