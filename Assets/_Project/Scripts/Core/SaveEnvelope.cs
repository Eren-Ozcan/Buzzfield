using System;
using System.Security.Cryptography;
using System.Text;

namespace Buzzfield.Core
{
    /// <summary>
    /// Wraps the save JSON with a SHA-256 checksum so a truncated or damaged file is
    /// detected and the backup is loaded instead. This is corruption detection, not
    /// anti-cheat: there is no secret key.
    /// Format: "BZ1|&lt;base64 checksum&gt;|&lt;payload&gt;".
    /// </summary>
    public static class SaveEnvelope
    {
        const string Version = "BZ1";
        const char Separator = '|';

        public static string Wrap(string payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            return Version + Separator + Checksum(payload) + Separator + payload;
        }

        /// <summary>False when the text is malformed or the checksum does not match.</summary>
        public static bool TryUnwrap(string text, out string payload)
        {
            payload = null;
            if (string.IsNullOrEmpty(text))
                return false;

            int first = text.IndexOf(Separator);
            int second = first < 0 ? -1 : text.IndexOf(Separator, first + 1);
            if (second < 0 || text.Substring(0, first) != Version)
                return false;

            string checksum = text.Substring(first + 1, second - first - 1);
            string body = text.Substring(second + 1);
            if (checksum != Checksum(body))
                return false;

            payload = body;
            return true;
        }

        static string Checksum(string payload)
        {
            using (var sha = SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }
    }
}
