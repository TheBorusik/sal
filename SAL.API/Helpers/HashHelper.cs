using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public static class HashHelper
    {
        
        public static string MD5(this string value, bool hexString = true)
        {
            var alg = HashAlgorithm.Create("MD5");
            return MakeHashFromString(alg, value, hexString);
        }
        public static string MD5(this JValue value, bool hexString = true)
        {
            return MD5(value.Value != null ? value.Value.ToString() : "", hexString);
        }
        public static string MD5(this object value, bool hexString = true)
        {
            var alg = HashAlgorithm.Create("MD5");
            return MakeHashFromObject(alg, value, hexString);
        }
        
        public static string SHA1(this string value, bool hexString = true)
        {
            var alg = HashAlgorithm.Create("SHA1");
            return MakeHashFromString(alg, value, hexString);
        }
        public static string SHA1(this object value, bool hexString = true)
        {
            var alg = HashAlgorithm.Create("SHA1");
            return MakeHashFromObject(alg, value, hexString);
        }
        public static string SHA1(this JValue value, bool hexString = true)
        {
            return SHA1(value.Value != null ? value.Value.ToString() : "", hexString);
        }
        public static string SHA256(this string value, bool hexString = true)
        {
            var alg = HashAlgorithm.Create("SHA256");
            return MakeHashFromString(alg, value, hexString);
        }
        public static string SHA256(this object value, bool hexString = true)
        {
            var alg = HashAlgorithm.Create("SHA256");
            return MakeHashFromObject(alg, value, hexString);
        }
        
        public static string SHA256(this JValue value, bool hexString = true)
        {
            return SHA256(value.Value != null ? value.Value.ToString() : "", hexString);
        }
        public static string SHA384(this string value, bool hexString = true)
        {
            var alg = HashAlgorithm.Create("SHA256");
            return MakeHashFromString(alg, value, hexString);
        }
        public static string SHA384(this object value, bool hexString = true)
        {
            var alg = HashAlgorithm.Create("SHA256");
            return MakeHashFromObject(alg, value, hexString);
        }
        
        public static string SHA384(this JValue value, bool hexString = true)
        {
            return SHA384(value.Value != null ? value.Value.ToString() : "", hexString);
        }
        public static string SHA512(this string value, bool hexString = true)
        {
            var alg = HashAlgorithm.Create("SHA256");
            return MakeHashFromString(alg, value, hexString);
        }
        public static string SHA512(this object value, bool hexString = true)
        {
            var alg = HashAlgorithm.Create("SHA256");
            return MakeHashFromObject(alg, value, hexString);
        }
        
        public static string SHA512(this JValue value, bool hexString = true)
        {
            return SHA512(value.Value != null ? value.Value.ToString() : "", hexString);
        }
        
        private static string MakeHashFromObject(HashAlgorithm alg, object obj, bool hexString)
        {
            return MakeHash(alg, SalSerializer.BinarySerialize(obj), hexString);
        }
        private static string MakeHashFromString(HashAlgorithm alg, string str, bool hexString)
        {
            return MakeHash(alg, Encoding.UTF8.GetBytes(str), hexString);
        }
        private static string MakeHash(HashAlgorithm alg, byte[] data, bool hexString)
        {
            var hash = alg.ComputeHash(data);
            return hexString ? Convert.ToHexString(hash) : Convert.ToBase64String(hash);
        }
        
    }
}