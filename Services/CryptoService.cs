using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RvtToNavisConverter.Services
{
    public class CryptoService : ICryptoService
    {
        // Internal key for encryption - in production, this should be more secure
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public CryptoService()
        {
            // Generate a consistent key based on application-specific data
            var keyString = "RvtN@visC0nv3rt3r2025BTankut!@#$";
            using (var sha256 = SHA256.Create())
            {
                _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
            }
            
            // Generate IV from a portion of the key
            _iv = new byte[16];
            Array.Copy(_key, _iv, 16);
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = _iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var encryptor = aes.CreateEncryptor())
                    using (var memoryStream = new MemoryStream())
                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        var plainBytes = Encoding.UTF8.GetBytes(plainText);
                        cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                        cryptoStream.FlushFinalBlock();
                        
                        return Convert.ToBase64String(memoryStream.ToArray());
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = _iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    var cipherBytes = Convert.FromBase64String(cipherText);

                    using (var decryptor = aes.CreateDecryptor())
                    using (var memoryStream = new MemoryStream(cipherBytes))
                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    using (var reader = new StreamReader(cryptoStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        public string ComputeChecksum(string data)
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;

            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data + "BTankut2025"));
                return Convert.ToBase64String(bytes);
            }
        }

        public bool VerifyChecksum(string data, string checksum)
        {
            if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(checksum))
                return false;

            var computedChecksum = ComputeChecksum(data);
            return computedChecksum == checksum;
        }
    }
}