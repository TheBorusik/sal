using System;
using Newtonsoft.Json;

namespace SAL.API
{
    public static class SalSerializer 
	{
		private static readonly SalJsonSerializerSettings sal = new SalJsonSerializerSettings();


        public static string Serialize(object obj)
		{
			return JsonConvert.SerializeObject(obj, sal.SerializerSettings);
		}

        public static string SerializeIndented(object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented, sal.SerializerSettings);
        }

		public static T Deserialize<T>(string jsonData)
		{
		    return JsonConvert.DeserializeObject<T>(jsonData, sal.SerializerSettings);
        }
        
		public static object Deserialize(string jsonData, Type dataType)
		{
		    return JsonConvert.DeserializeObject(jsonData, sal.SerializerSettings);
		}

	    public static byte[] BinarySerialize(object obj)
	    {
	        return SalEncoding.GetBytes(Serialize(obj));
	    }


	    /*public static T BinaryDeserialize<T>(ReadOnlySpan<byte> data)
	    {
	        return JsonConvert.DeserializeObject<T>(SalEncoding.GetString(data), sal.SerializerSettings);
	    }*/
	    
	    public static T BinaryDeserialize<T>(byte[] data)
		{
			return JsonConvert.DeserializeObject<T>(SalEncoding.GetString(data), sal.SerializerSettings);
		}

	    public static object BinaryDeserialize(byte[] data, Type dataType)
	    {
	        return JsonConvert.DeserializeObject(SalEncoding.GetString(data), sal.SerializerSettings);
	    }

        public static JsonSerializer Create()
        {
            return JsonSerializer.Create(sal.SerializerSettings);
        }
	}
}