namespace RvtToNavisConverter.Services
{
    public interface ICryptoService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
        string ComputeChecksum(string data);
        bool VerifyChecksum(string data, string checksum);
    }
}