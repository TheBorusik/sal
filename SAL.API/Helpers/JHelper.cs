using System;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public static class JHelper
    {
        // base
        public static T ConvertValue<T>(this JToken jToken)
        {
            return (T) jToken.ConvertValue(typeof(T));
        }

        public static object ConvertValue(this JToken jToken, Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type);

            if (jToken == null || jToken.Type is JTokenType.Null or JTokenType.None)
            {
                if (underlyingType != null || type.IsClass)
                    return null;
                else
                    throw new InvalidCastException("null value to not Nullable type");
            }

            if (jToken is JProperty jProperty)
            {
                return jProperty.ConvertValue(type);
            }

            if (type == typeof(object))
            {
                return jToken;
            }


            if (jToken is JValue jVal)
            {
                if (jVal.GetType() == type)
                {
                    return jVal.Value;
                }
                else
                {
/*                    if (type.IsEnum)
                    {
                        return jVal.ToObject(type);
                    }

                    if (jVal.Value is IConvertible)
                    {
                        
                        return Convert.ChangeType(jVal.Value, underlyingType ?? type);
                    }*/
                    return jVal.ToObject(type);
                    // throw new InvalidCastException($"Type: \"{jVal.GetType().Name}\" can't convert to type \"{type.Name}\".");
                }
            }

            if (type == typeof(string))
            {
                return jToken.ToJson();
            }

            if (jToken is JObject jObj)
            {
                if (!type.IsClass)
                    throw new InvalidCastException($"Object can't convert to type \"{type.Name}\".");

                return jObj.ToObject(type);
            }


            if (jToken is JArray jArr)
            {
                if (!type.IsArray)
                    throw new InvalidCastException($"Array can't convert to type \"{type.Name}\".");

                return jArr.ToObject(type);
            }


            throw new InvalidCastException($"Unknown type can't convert to type \"{type.Name}\".");
        }

        public static JToken GetValueIC(this JToken jToken, string propertyName)
        {
            if (jToken is JObject jObj)
                foreach(var property in jObj.Properties())
                    if (string.Equals(property.Name, propertyName, StringComparison.InvariantCultureIgnoreCase))
                        return property.Value;
            
            return jToken?.SelectToken(propertyName);
        }


        public static string ToJson(this object jToken)
        {
            return SalSerializer.Serialize(jToken);
        }

        public static string ToIndentedJson(this object jToken)
        {
            return SalSerializer.SerializeIndented(jToken);
        }

        // string

        public static T ConvertValue<T>(this string str)
        {
            return (T) str.ConvertValue(typeof(T));
        }

        public static object ConvertValue(this string str, Type type)
        {
            var jToken = JToken.Parse(str);
            return jToken.ConvertValue(type);
        }

        // JObject

        public static JObject AddOrUpdate(this JObject jObj, string propertyName, object value)
        {
            var find = false;
            foreach(var property in jObj.Properties())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.InvariantCultureIgnoreCase))
                {
                    property.Value = value != null ? JToken.FromObject(value) : JValue.CreateNull();
                    find = true;
                    break;
                }
            }

            if (find) return jObj;

            try
            {
                if (value != null)
                    jObj.Add(propertyName, JToken.FromObject(value));
            }
            catch (Exception)
            {
                // ignored
            }

            return jObj;
        }
        
        
        public static bool TryGetValue(this JObject jObj, string propertyName, Type type, out object value)
        {
            try
            {
                value = jObj.GetValueIC(propertyName).ConvertValue(type);
                return value != null;
            }
            catch
            {
                value = null;
            }

            return false;
        }

        public static bool TryGetValue<T>(this JObject jObj, string propertyName, out T value)
        {
            try
            {
                value = jObj.GetValueIC(propertyName).ConvertValue<T>();
                return value != null;
            }
            catch
            {
                value = default(T);
            }

            return false;
        }

        public static object GetValue(this JObject jObj, string propertyName, Type type)
        {
            return jObj.GetValueIC(propertyName).ConvertValue(type);
        }

        public static T GetValue<T>(this JObject jObj, string propertyName)
        {
            return jObj.GetValueIC(propertyName).ConvertValue<T>();
        }

        public static object GetSafeValue(this JObject jObj, string propertyName, Type type, object safeValue)
        {
            try
            {
                return jObj.GetValueIC(propertyName).ConvertValue(type);
            }
            catch
            {
                return safeValue;
            }
        }

        public static T GetSafeValue<T>(this JObject jObj, string propertyName, T safeValue)
        {
            try
            {
                var token = jObj.GetValueIC(propertyName);
                if (token == null)
                    return safeValue;
                return token.ConvertValue<T>();
            }
            catch
            {
                return safeValue;
            }
        }

        public static bool ContainsKey(this JObject jObj, string propertyName)
        {
            return jObj.GetValueIC(propertyName) != null;
        }
        
        public static JToken RemoveEmptyChildren(this JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                var copy = new JObject();
                foreach (var prop in token.Children<JProperty>())
                {
                    var child = prop.Value;
                    if (child.HasValues)
                    {
                        child = RemoveEmptyChildren(child);
                    }
                    if (!child.IsEmpty())
                    {
                        copy.Add(prop.Name, child);
                    }
                }
                return copy;
            }

            if (token.Type == JTokenType.Array)
            {
                var copy = new JArray();
                foreach (var item in token.Children())
                {
                    var child = item;
                    if (child.HasValues)
                    {
                        child = RemoveEmptyChildren(child);
                    }
                    if (!child.IsEmpty())
                    {
                        copy.Add(child);
                    }
                }
                return copy;
            }
            
            return token;
        }
        
        public static JObject RemoveEmptyChildren(this JObject obj)
        {
            return (JObject) RemoveEmptyChildren((JToken)obj);
        }

        public static bool IsEmpty(this JToken token)
        {
            return (token.Type == JTokenType.Null) ||
                   (token.Type == JTokenType.Object && !token.HasValues);
        }

        public static bool IsNull(this JToken token)
        {
            if (token == null)
                return true;
            if (token.Type == JTokenType.Null)
                return true;
            if (token.Type == JTokenType.None)
                return true;
            if (token.Type == JTokenType.String)
            {
                if (string.Equals(token.Value<string>(), "null"))
                    return true;
            }
            return false;
        }
        
    
        
        
        //JProperty

        public static T ConvertValue<T>(this JProperty jProperty)
        {
            return (T) jProperty.ConvertValue(typeof(T));
        }

        public static object ConvertValue(this JProperty jProperty, Type type)
        {
            return jProperty.Value.ConvertValue(type);
        }


        // object

        public static T ConvertValue<T>(this object obj)
        {
            return (T) obj.ConvertValue(typeof(T));
        }

        public static object ConvertValue(this object obj, Type type)
        {
            var jToken = obj as JToken ?? JToken.FromObject(obj);
            return jToken.ConvertValue(type);
        }

        public static bool TryGetValue(this object obj, string propertyName, Type type, out object value)
        {
            var jObj = obj as JObject ?? JObject.FromObject(obj);
            return jObj.TryGetValue(propertyName, type, out value);
        }

        public static bool TryGetValue<T>(this object obj, string propertyName, out T value)
        {
            var jObj = obj as JObject ?? JObject.FromObject(obj);
            return jObj.TryGetValue(propertyName, out value);
        }

        public static object GetValue(this object obj, string propertyName, Type type)
        {
            var jObj = obj as JObject ?? JObject.FromObject(obj);
            return jObj.GetValueIC(propertyName).ConvertValue(type);
        }

        public static T GetValue<T>(this object obj, string propertyName)
        {
            var jObj = obj as JObject ?? JObject.FromObject(obj);
            return jObj.GetValueIC(propertyName).ConvertValue<T>();
        }

        public static object GetSafeValue(this object obj, string propertyName, Type type, object safeValue)
        {
            try
            {
                var jObj = obj as JObject ?? JObject.FromObject(obj);
                var prop = jObj.GetValueIC(propertyName);
                if (prop == null)
                    return safeValue;
                return prop.ConvertValue(type);
            }
            catch
            {
                return safeValue;
            }
        }

        public static T Clone<T>(this T obj) where T : class, new()
        {
            if (obj == null)
                return null;

            return JObject.FromObject(obj).ConvertValue<T>();
        }

        public static T Convert<T>(this object obj) where T : class, new()
        {
            if (obj == null)
                return null;

            return JObject.FromObject(obj).ConvertValue<T>();
        }

        public static T GetSafeValue<T>(this object obj, string propertyName, T safeValue)
        {
            try
            {
                var jObj = obj as JObject ?? JObject.FromObject(obj);

                var prop = jObj.GetValueIC(propertyName);
                if (prop == null)
                    return safeValue;
                return prop.ConvertValue<T>();
            }
            catch
            {
                return safeValue;
            }
        }

        public static bool ContainsKey(this object obj, string propertyName)
        {
            var jObj = obj as JObject ?? JObject.FromObject(obj);
            return jObj.SelectToken(propertyName) != null;
        }


        //foreach

        public static JToken ForEach(this JToken src, Action<JToken> action)
        {
            if (src == null)
                return null;
            if (action == null)
                return src;

            var objectToken = src.First;

            while (objectToken != null)
            {
                action(objectToken);
                objectToken = objectToken.Next;
            }

            return src;
        }

        public static TResult ToObject<TResult>(this string json) where TResult : class, new()
        {
            return string.IsNullOrWhiteSpace(json) ? null : JToken.Parse(json).ToObject<TResult>();
        }

        public static JObject ToJObjectSafe(this string json)
        {
            if(string.IsNullOrWhiteSpace(json))
                return  new JObject();

            try
            {
                var jt = JToken.Parse(json);
                if (jt.Type is JTokenType.Object)
                    return jt as JObject;
                return new JObject();

            }
            catch (Exception)
            {
                return new JObject();
            }

        }

        public static JObject ToJObjectSafe(this object obj)
        {
            if (obj == null)
                return new JObject();
            if (obj.GetType() == typeof(JObject))
                return (JObject) obj;
            return JObject.FromObject(obj);
        }

        //clone
        public static JObject Clone(this JObject obj)
        {
            return (JObject) obj?.DeepClone();
        }
    }
}