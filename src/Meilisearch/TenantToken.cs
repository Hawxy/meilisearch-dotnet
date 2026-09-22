using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Meilisearch
{
    public class TenantToken
    {
        /// <summary>
        /// Generates a Tenant Token in a JWT string format.
        /// </summary>
        /// <returns>JWT string</returns>
        public static string GenerateToken(string apiKeyUid, TenantTokenRules searchRules, string apiKey, DateTime? expiresAt)
        {
            if (string.IsNullOrEmpty(apiKeyUid))
            {
                throw new MeilisearchTenantTokenApiKeyUidInvalid();
            }

            if (string.IsNullOrEmpty(apiKey) || apiKey.Length < 16)
            {
                throw new MeilisearchTenantTokenApiKeyInvalid();
            }

            if (expiresAt.HasValue && DateTime.Compare(DateTime.UtcNow, (DateTime)expiresAt) > 0)
            {
                throw new MeilisearchTenantTokenExpired();
            }

            var signingKey = Encoding.ASCII.GetBytes(apiKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Claims = new Dictionary<string, object>
                {
                    ["apiKeyUid"] = apiKeyUid,
                    ["searchRules"] = searchRules.ToJsonElement(),
                },
                Expires = expiresAt,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(signingKey), SecurityAlgorithms.HmacSha256)
            };

            var tokenHandler = new JsonWebTokenHandler
            {
                SetDefaultTimesOnTokenCreation = false
            };

            return tokenHandler.CreateToken(tokenDescriptor);
        }
    }
}
