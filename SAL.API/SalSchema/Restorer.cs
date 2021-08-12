using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace SAL.API
{
    public class SchemaObjectRestorer
    {
        public JToken ResoreFromJSchema(JSchema schema)
        {
            if (schema == null)
                return new JObject();

            return MakeItem(schema, 0, false);
		
        }
	
        private JToken MakeItem(JSchema schema, int level, bool required)
        {
            if(!schema.Type.HasValue)
                return new JObject();

		
            if(schema.Type.Value.HasFlag(JSchemaType.Object))
                return MakeObject(schema, level);



            if (schema.Type.Value.HasFlag(JSchemaType.Array))
                return MakeArray(schema, level);

            if (schema.Type.Value.HasFlag(JSchemaType.Boolean))
                return MakeBoolean(schema,required);
            if (schema.Type.Value.HasFlag(JSchemaType.String))
                return MakeString(schema, required);

            if (schema.Type.Value.HasFlag(JSchemaType.Integer))
                return MakeInteger(schema,required);
            if (schema.Type.Value.HasFlag(JSchemaType.Number))
                return MakeNumber(schema,required);


            return null;
        }

        private JToken MakeObject(JSchema schema, int level)
        {
            var result = new JObject();
		
            if(level > 3)
                return result;
		
            foreach(var kv in schema.Properties)
            {
                var required = schema.Required.Contains(kv.Key);
			
                var value =  MakeItem(kv.Value, level + 1, required);
                if(value != null)
                    result.Add(kv.Key,value);
            }
		
            if(schema.AdditionalProperties != null)
            {
                return new JValue("object as Dictionary");
            }
	
            return result;
        }

        private JArray MakeArray(JSchema schema,int level)
        {
            var itemSchema = schema.Items.FirstOrDefault();
            if (itemSchema != null)
            {
                var item = MakeItem(itemSchema, level + 1, false);
                return new JArray(item);
            }
		
            return new JArray();
        }

        private JValue MakeString(JSchema schema, bool required)
        {
		
            var nullable = schema.Type.Value.HasFlag(JSchemaType.Null);
		
            var strValue = "string";
            var format = schema.Format;
            if (format == "date-time")
            {
                if(required)
                    strValue = DateTime.UtcNow.ToString("O");
                else
                    strValue = new DateTime().ToString("O");
            }
            else if (format == "time-span")
            {
                if (required)
                    strValue = TimeSpan.FromTicks(863990000000).ToString();
                else
                    strValue = TimeSpan.Zero.ToString();
            }
            else
            {
                if (format == "b64str")
                    strValue = "b64str";
				
                if(required)
                    strValue = "!" + strValue;
            }
			

		
            return new JValue(strValue);
        }
        private JValue MakeBoolean(JSchema schema, bool required)
        {	
            return new JValue(required);
        }
        private JValue MakeInteger(JSchema schema, bool required)
        {
            if(required)
                return new JValue(555);
            return new JValue(0);
        }
        private JValue MakeNumber(JSchema schema, bool required)
        {
            if (required)
                return new JValue(555.555);
            return new JValue(0.0);
        }
    }
}