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
            generator.GenerationProviders.Add(new JObjectSchemaProvider());
        }

        public static JSchema Generate(Type type)
        {
            return generator.Generate(type);
        }
    }
    
    
    internal class JObjectSchemaProvider : JSchemaGenerationProvider
    {
        public override JSchema GetSchema(JSchemaTypeGenerationContext context)
        {
            // customize the generated schema for these types to have a format
            if (context.ObjectType == typeof(JObject))
            {
                return new JSchema
                {
                    Type = JSchemaType.Object,
                    Format = "Any",
                };
            }
            if (context.ObjectType == typeof(object))
            {
                return new JSchema
                {
                    Type = JSchemaType.Object,
                    Format = "Any",
                };
            }
            if (context.ObjectType == typeof(JArray))
            {
                return new JSchema
                {
                    Type = JSchemaType.Array,
                    Items = { new JSchema { Format="Any" } }
                };
            }
            if (context.ObjectType == typeof(JToken))
            {
                return new JSchema
                {
                    Format = "Any",
                };
            }

            // use default schema generation for all other types
            return null;
        }


    }
}