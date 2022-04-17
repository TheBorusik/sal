using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;

namespace SAL.API
{
    public static class SalSchema
    {
        private readonly static JSchemaGenerator generator;

        static SalSchema()
        {
            generator = new JSchemaGenerator();
            generator.DefaultRequired = Newtonsoft.Json.Required.Default;
            generator.GenerationProviders.Add(new StringEnumGenerationProvider());
            generator.GenerationProviders.Add(new SalSchemaProvider());
        }

        public static JSchema Generate(Type type)
        {
            return type == null ? null : generator.Generate(type);
        }
    }
    
    
    
    internal class SalSchemaProvider : JSchemaGenerationProvider
    {
        public override JSchema GetSchema(JSchemaTypeGenerationContext context)
        {
		
            var underlyingType = Nullable.GetUnderlyingType(context.ObjectType);
            // customize the generated schema for these types to have a format
            if (context.ObjectType == typeof(JObject))
            {
                return new JSchema
                {
                    Type = JSchemaType.Object | JSchemaType.Null,
                    Format = "any",
                };
            }
            if (context.ObjectType == typeof(object))
            {
                return new JSchema
                {
                    Format = "any",
                };
            }
            if (context.ObjectType == typeof(JArray))
            {
                return new JSchema
                {
                    Type = JSchemaType.Array | JSchemaType.Null,
                    Items = { new JSchema { Format = "any" } }
                };
            }
            if (context.ObjectType == typeof(JToken))
            {
                return new JSchema
                {
                    Format = "any",
                };
            }
            if (context.ObjectType == typeof(byte[]))
            {
                return new JSchema
                {
                    Type = JSchemaType.String | JSchemaType.Null,
                    Format = "b64str",
                };
            }
            if (underlyingType == typeof(TimeSpan) || context.ObjectType == typeof(TimeSpan))
            {
                var schema = new JSchema
                {
                    Type = JSchemaType.String,
                    Format = "time-span",
                };
                if(underlyingType != null)
                {
                    schema.Type |= JSchemaType.Null;
                }
                return schema;
            }



            // use default schema generation for all other types
            return null;
        }


    }
    
}