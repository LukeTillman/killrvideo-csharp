using System;
using System.Security.Cryptography;
using System.Text;

namespace KillrVideo.Utils
{
    public static class GravatarHasher
    {
        /// <summary>
        /// Gets a Gravatar compatible hash string for the specified email address.
        /// </summary>
        /// <param name="emailAddress"></param>
        /// <returns></returns>
        public static string GetHashForEmailAddress(string emailAddress)
        {
            byte[] emailAddressBytes = Encoding.ASCII.GetBytes(emailAddress.Trim().ToLowerInvariant());
            using (var md5 = new MD5CryptoServiceProvider())
            {
                byte[] hashedBytes = md5.ComputeHash(emailAddressBytes);
                return BitConverter.ToString(hashedBytes).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }
}