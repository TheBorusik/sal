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
    }
}