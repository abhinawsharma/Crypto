using System;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.IO;
using TMCryptoCore.Model;
using TMCryptoCore.DAL;
using System.Linq;

namespace TMCryptoCore.Controllers
{

    [ApiController]
    public class EncryptionController : ControllerBase
    {
        TripleDES tripleDES = null;
        private readonly TMCryptoContext _context;
        public EncryptionController(TMCryptoContext context)
        {
            tripleDES = TripleDES.Create();
            tripleDES.Key = new byte[] { 123, 32, 213, 242, 212, 32, 24, 67, 84, 224, 55, 212, 123, 121, 31, 32 };
            tripleDES.IV = new byte[] { 122, 211, 32, 103, 200, 102, 87, 103 };

            _context = context;
        }
        [Route("GlobeLife/Crypto/Encrypt/{*plainString}")]
        public string Encrypt(string plainString,bool CheckSession)
        {
            try
            {
                if (CheckSession)
                {
                    //get the Auth tag from header
                    if (Request.Headers.ContainsKey("Auth"))
                    {
                        // return "TMCrypto:Please provide a valid [Auth] value in the header";
                        var check = CheckSessionTime(Request.Headers["Auth"]);//var check = CheckSessionTime("83D822D1-68A4-6BAE-065A-AD6F419D5FCE");
                        if ("SUCCESS" != check) return check;
                    }
                }
                using (var encryptor = tripleDES.CreateEncryptor())
                {
                    byte[] encryptedData = null;
                    using (var dataStream = new MemoryStream())
                    {
                        using (var encryptedStream = new CryptoStream(dataStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (var theWriter = new StreamWriter(encryptedStream))
                            {
                                //Write the string to the memory stream
                                theWriter.Write(plainString);
                                //End the writing
                                theWriter.Flush();
                                encryptedStream.FlushFinalBlock();
                                //Position back at start
                                dataStream.Position = 0;
                                encryptedData = new byte[dataStream.Length + 1]; 
                                //Read data from memory
                                dataStream.Read(encryptedData, 0, (int)dataStream.Length  + 1);
                                //Convert to String
                                return Convert.ToBase64String(encryptedData);
                            }
                        }
                    }
                }
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
                using (var dataStream = new MemoryStream())
                {
                    using (var decryptor = tripleDES.CreateDecryptor())
                    {
                        using (var encryptedStream = new CryptoStream(dataStream, decryptor, CryptoStreamMode.Write))
                        {
                            byte[] encryptedData = Convert.FromBase64String(encryptedString);

                            //Write the decrypted data to the memory stream
                            encryptedStream.Write(encryptedData, 0, encryptedData.Length  - 1); //to make compatible with VB COM+ code 
                            encryptedStream.FlushFinalBlock();
                            //Position back at start
                            dataStream.Position = 0;
                            long strLen = dataStream.Length;
                            encryptedData = new byte[strLen];
                            //Read decrypted data to byte()
                            dataStream.Read(encryptedData, 0, (int)strLen);
                            //Construct string from byte()
                            return System.Text.Encoding.UTF8.GetString(encryptedData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return "Exception:" + ex.Message;
            }
        }


        [Route("GlobeLife"),HttpGet]
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
