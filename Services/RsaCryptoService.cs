using System;
using System.Security.Cryptography;
using System.Text;
using RvtToNavisConverter.Models;

namespace RvtToNavisConverter.Services
{
    public class RsaCryptoService : IRsaCryptoService
    {
        // Public key that will be embedded in the application
        // This is safe to distribute with the application
        private const string EmbeddedPublicKey = @"<RSAKeyValue><Modulus>uQA7kHMs3VcMk/mCpl9dS7x+/chKQohJ6UzRGb7pIEZRdoG85q0b8RImYw0CuLSDPP3qiuMBQ45dyH/T8xGtz7dcUzM20vKQ9ctDGcrMP4PfGJp8z6rlRUtSNmfyBUzCnaaoAtU5r18MlcLdbPry7S1wXnO0F0oJQAABY02JIZ1IOYecJ7/5qhcZ+0U+lPhlQIpbp3DvnOMthET3bIXMpeWNI63rEswaXZ+zWuwTD1Oes4EnMb2jVz3bksex8oZemSGxeMlR/bJcz7SkkbzDTuykmVk9Esi7Z/IJS/YUs1KAASFTM5B7yQzvDfE5zj4szn4FX3/cWT7jXUV60buysQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        public void GenerateKeyPair(out string publicKey, out string privateKey)
        {
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                publicKey = rsa.ToXmlString(false); // Export public key only
                privateKey = rsa.ToXmlString(true); // Export private key with public key
            }
        }

        public string SignData(string data, string privateKey)
        {
            try
            {
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(privateKey);
                    
                    var dataBytes = Encoding.UTF8.GetBytes(data);
                    var signatureBytes = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    
                    return Convert.ToBase64String(signatureBytes);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to sign data", ex);
            }
        }

        public bool VerifySignature(string data, string signature, string publicKey)
        {
            try
            {
                using (var rsa = new RSACryptoServiceProvider())
                {
                    // Use embedded public key if none provided
                    rsa.FromXmlString(string.IsNullOrEmpty(publicKey) ? EmbeddedPublicKey : publicKey);
                    
                    var dataBytes = Encoding.UTF8.GetBytes(data);
                    var signatureBytes = Convert.FromBase64String(signature);
                    
                    return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
            }
            catch
            {
                return false;
            }
        }

        public bool VerifyLicenseSignature(License license)
        {
            // Create the same data string that was signed
            var dataToVerify = $"{license.HardwareId}|{license.TrialStartDate:yyyy-MM-dd}|{license.TrialDays}|{license.LicenseType}|{license.CustomerName}|{license.CustomerEmail}";
            
            return VerifySignature(dataToVerify, license.Signature, null);
        }
    }
}