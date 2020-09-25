using System.Security.Cryptography;
using System.Text;

namespace Xpo.Smart.Efficiency.Shared.Extensions
{
    public static class HashUtils
    {
        public static string Hash(params object[] components)
        {
            var sb = new StringBuilder();
            foreach (var component in components)
            {
                sb.Append(component);
            }

            using (SHA256 hasher = SHA256.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = hasher.ComputeHash(Encoding.Unicode.GetBytes(sb.ToString()));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                StringBuilder sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("X2"));
                }

                // Return the hexadecimal string.
                return sBuilder.ToString();
            }
        }
    }
}
