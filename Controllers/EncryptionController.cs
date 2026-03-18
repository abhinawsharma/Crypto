using System;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using TMCryptoCore.Model;
using TMCryptoCore.DAL;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace TMCryptoCore.Controllers
{

    [ApiController]
    public class EncryptionController : ControllerBase
    {
        // AES-256-GCM constants
        private const int NonceSizeBytes = 12; // 96-bit nonce recommended for GCM
        private const int TagSizeBytes = 16;   // 128-bit authentication tag

        private readonly byte[] _key;
        private readonly TMCryptoContext _context;

        public EncryptionController(TMCryptoContext context, IConfiguration configuration)
        {
            // Load the AES-256 key (32 bytes / 256 bits) from configuration
            string keyBase64 = configuration["Encryption:Key"]
                ?? throw new InvalidOperationException("Encryption:Key is not configured.");
            _key = Convert.FromBase64String(keyBase64);
            if (_key.Length != 32)
                throw new InvalidOperationException("Encryption:Key must be a 32-byte (256-bit) AES key.");

            _context = context;
        }

        [Route("GlobeLife/Crypto/Encrypt/{*plainString}")]
        public string Encrypt(string plainString, bool CheckSession)
        {
            try
            {
                if (CheckSession)
                {
                    //get the Auth tag from header
                    if (Request.Headers.ContainsKey("Auth"))
                    {
                        var check = CheckSessionTime(Request.Headers["Auth"]);
                        if ("SUCCESS" != check) return check;
                    }
                }

                byte[] plainBytes = Encoding.UTF8.GetBytes(plainString);
                byte[] nonce = new byte[NonceSizeBytes];
                byte[] tag = new byte[TagSizeBytes];
                byte[] ciphertext = new byte[plainBytes.Length];

                // Use a unique random nonce for every encryption operation
                RandomNumberGenerator.Fill(nonce);

                using (var aesGcm = new AesGcm(_key, TagSizeBytes))
                {
                    aesGcm.Encrypt(nonce, plainBytes, ciphertext, tag);
                }

                // Output format: nonce (12 bytes) + tag (16 bytes) + ciphertext
                byte[] result = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];
                Buffer.BlockCopy(nonce, 0, result, 0, NonceSizeBytes);
                Buffer.BlockCopy(tag, 0, result, NonceSizeBytes, TagSizeBytes);
                Buffer.BlockCopy(ciphertext, 0, result, NonceSizeBytes + TagSizeBytes, ciphertext.Length);

                return Convert.ToBase64String(result);
            }
            catch (Exception ex)
            {
                return "Exception:" + ex.Message;
            }
        }

        [Route("GlobeLife/Crypto/Decrypt/{*encryptedString}")]
        public string Decrypt(string encryptedString)
        {
            if (string.IsNullOrEmpty(encryptedString)) return encryptedString;

            try
            {
                byte[] encryptedData = Convert.FromBase64String(encryptedString);

                int ciphertextSize = encryptedData.Length - NonceSizeBytes - TagSizeBytes;
                if (ciphertextSize < 0)
                    throw new ArgumentException("Invalid encrypted data: payload is too short.");

                byte[] nonce = new byte[NonceSizeBytes];
                byte[] tag = new byte[TagSizeBytes];
                byte[] ciphertext = new byte[ciphertextSize];
                byte[] plaintext = new byte[ciphertextSize];

                // Parse nonce + tag + ciphertext from the encrypted data
                Buffer.BlockCopy(encryptedData, 0, nonce, 0, NonceSizeBytes);
                Buffer.BlockCopy(encryptedData, NonceSizeBytes, tag, 0, TagSizeBytes);
                Buffer.BlockCopy(encryptedData, NonceSizeBytes + TagSizeBytes, ciphertext, 0, ciphertextSize);

                using (var aesGcm = new AesGcm(_key, TagSizeBytes))
                {
                    aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
                }

                return Encoding.UTF8.GetString(plaintext);
            }
            catch (Exception ex)
            {
                return "Exception:" + ex.Message;
            }
        }

        [Route("GlobeLife"), HttpGet]
        public string Get()
        {
            return "Welcome to Globe Life!";
        }

        private string CheckSessionTime(string guid)
        {
            try
            {
                var asp = _context.ASPSessionStates.Where(r => r.GUID.Equals(guid)).ToList();
                if (asp.Count == 0) return "TMCrypto:UserNotFound";
                var dt = asp[0].DateTime_Added;
                if (DateTime.Now - dt > TimeSpan.FromMinutes(5.0))
                    return "TMCrypto:SessionExpired";
            }
            catch (Exception ex)
            {
                return "TMKCrypto:Exception" + ex.Message;
            }
            return "SUCCESS";
        }
    }
}
