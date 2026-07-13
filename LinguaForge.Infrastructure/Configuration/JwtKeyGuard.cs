using System;
using System.Linq;
using System.Text;

namespace LinguaForge.Infrastructure.Configuration
{
    /// <summary>
    /// Validates the JWT signing key at startup. A missing, too-short, or placeholder key lets
    /// anyone forge tokens and bypass every <c>[Authorize]</c> endpoint, so the API must refuse
    /// to boot rather than run with a weak or committed secret. (LF-101)
    /// </summary>
    public static class JwtKeyGuard
    {
        /// <summary>Minimum key length in bytes — HS256 needs a 256-bit (32-byte) key.</summary>
        public const int MinimumKeyBytes = 32;

        // Distinctive fragments (already lower-cased and stripped of separators) that only appear
        // in sample/placeholder/default keys. Each is long enough that a real random key will not
        // contain it by chance.
        private static readonly string[] PlaceholderFragments =
        {
            "changeme",
            "changeit",
            "replaceme",
            "placeholder",
            "yoursecret",
            "localdevonlysigningkey", // the former docker-compose default
            "samplekey",
            "examplekey",
        };

        /// <summary>
        /// Throws <see cref="InvalidOperationException"/> when the key is missing, shorter than
        /// <see cref="MinimumKeyBytes"/>, or matches a known placeholder (in any hyphen/underscore/
        /// case variant). Returns normally for an acceptable key.
        /// </summary>
        public static void Validate(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException(
                    "Jwt:Key is missing. Set a long random secret via user-secrets (dev) or " +
                    "environment variables / Key Vault (prod) before starting.");
            }

            if (Encoding.UTF8.GetByteCount(key) < MinimumKeyBytes)
            {
                throw new InvalidOperationException(
                    $"Jwt:Key must be at least {MinimumKeyBytes} bytes. " +
                    "Generate one with, e.g., `openssl rand -base64 48`.");
            }

            // Normalize so "CHANGE_ME", "change-me", and "changeMe" are all caught.
            var normalized = new string(key.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            foreach (var fragment in PlaceholderFragments)
            {
                if (normalized.Contains(fragment, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Jwt:Key is still set to a known placeholder/default value. " +
                        "Set a unique long random secret before starting.");
                }
            }
        }
    }
}
