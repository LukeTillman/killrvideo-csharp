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
        public static string GetHashForEmailAddress(string emailAddress)
        {
            byte[] emailAddressBytes = Encoding.ASCII.GetBytes(emailAddress.Trim().ToLowerInvariant());
            using (var md5 = new MD5CryptoServiceProvider())
            {
                byte[] hashedBytes = md5.ComputeHash(emailAddressBytes);
                return BitConverter.ToString(hashedBytes).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        /// <summary>
        /// Gets the Gravatar image URL for the specified email address.
        /// </summary>
        public static string GetImageUrlForEmailAddress(string emailAddress)
        {
            var hash = GetHashForEmailAddress(emailAddress);
            return string.Format("https://secure.gravatar.com/avatar/{0}", hash);
        }
    }
}