namespace RvtToNavisConverter.Services
{
    public interface IRsaCryptoService
    {
        void GenerateKeyPair(out string publicKey, out string privateKey);
        string SignData(string data, string privateKey);
        bool VerifySignature(string data, string signature, string publicKey);
        bool VerifyLicenseSignature(Models.License license);
    }
}