using System;
using System.IO;
using System.Security.Cryptography;

namespace FileSignatureChecker.Classes
{
    public class UtilityFunctions
    {
        public static string CalculateSHA256(string filename)
        {
            using (var sha256 = SHA256.Create())
            {
                using (FileStream stream = File.OpenRead(filename))
                {
                    byte[] hash = sha256.ComputeHash(stream);
                    stream.Close();
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

    }
}