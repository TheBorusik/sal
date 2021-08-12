using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace SAL.API
{
    public static class JSchemaHelper
    {
        public static JSchema Clone(this JSchema schema)
        {
            if (schema == null)
                return null;
            return (JSchema) ((JToken) schema).DeepClone();
        }
        public static bool Validate(this JObject obj, JSchema schema)
        {
            return obj.IsValid(schema);
        }
        public static bool Validate(this object obj, JSchema schema)
        {
            return Validate(JObject.FromObject(obj), schema);
        }

        public static void ThrowIsInvalid(this JObject obj, JSchema schema)
        {
            IList<string> messages;
            if(!obj.IsValid(schema, out messages))
            {
                throw SalError.CreateValidation(messages.Select(s => new FieldError
                {
                    Description = s
                }).ToArray());
            }
        }
        
        public static void ThrowIsInvalid(this object obj, JSchema schema)
        {
            ThrowIsInvalid(JObject.FromObject(obj), schema);
        }

        public static JToken Restore(this JSchema schema)
        {
            var restorer = new SchemaObjectRestorer();
            return restorer.ResoreFromJSchema(schema);
        }
    }
}