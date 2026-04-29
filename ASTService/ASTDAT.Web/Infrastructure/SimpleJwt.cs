using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ASTDAT.Web.Infrastructure
{
    /// <summary>HS256 JWT (sub, exp, roles) using built-in crypto + JSON.NET only.</summary>
    public static class SimpleJwt
    {
        public const int DefaultTtlSeconds = 3600;

        public static string CreateAccessToken(
            string userId,
            string userName,
            IReadOnlyCollection<string> roleNames,
            string signingKey,
            int ttlSeconds = DefaultTtlSeconds)
        {
            if (string.IsNullOrEmpty(signingKey) || signingKey.Length < 16) throw new InvalidOperationException("JwtSigningKey in AppSettings must be at least 16 characters.");
            var exp = DateTimeOffset.UtcNow.AddSeconds(ttlSeconds).ToUnixTimeSeconds();
            var payload = new JObject
            {
                ["sub"] = userId,
                ["exp"] = exp,
            };
            if (!string.IsNullOrEmpty(userName)) payload["name"] = userName;
            if (roleNames != null && roleNames.Count > 0) payload["roles"] = new JArray(roleNames.Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray());
            return Encode(JObject.FromObject(new { alg = "HS256", typ = "JWT" }), payload, signingKey);
        }

        public static ClaimsPrincipal ValidateToPrincipal(string token, string signingKey, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrEmpty(signingKey)) { error = "missing token or key"; return null; }
            var parts = token.Split('.');
            if (parts.Length != 3) { error = "invalid format"; return null; }
            var signingInput = parts[0] + "." + parts[1];
            if (!ConstantTimeEquals(ComputeHmac(signingKey, signingInput), Base64UrlDecodeToBytes(parts[2]))) { error = "bad signature"; return null; }
            JObject p;
            try
            {
                p = JObject.Parse(Encoding.UTF8.GetString(Base64UrlDecodeToBytes(parts[1])));
            }
            catch { error = "invalid payload"; return null; }
            if (p["exp"] == null) { error = "no exp"; return null; }
            var exp = p.Value<long>("exp");
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp) { error = "expired"; return null; }
            var sub = p.Value<string>("sub");
            if (string.IsNullOrEmpty(sub)) { error = "no sub"; return null; }
            var name = p.Value<string>("name");
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, sub, ClaimValueTypes.String),
            };
            if (!string.IsNullOrEmpty(name)) claims.Add(new Claim(ClaimTypes.Name, name, ClaimValueTypes.String));
            if (p["roles"] is JArray arr) foreach (var v in arr) { var r = v?.Value<string>(); if (!string.IsNullOrEmpty(r)) claims.Add(new Claim(ClaimTypes.Role, r, ClaimValueTypes.String)); }
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        }

        static string Encode(JObject header, JObject payload, string key)
        {
            var p1 = Base64Url(Encoding.UTF8.GetBytes(header.ToString(Formatting.None)));
            var p2 = Base64Url(Encoding.UTF8.GetBytes(payload.ToString(Formatting.None)));
            var s = p1 + "." + p2;
            var h = ComputeHmac(key, s);
            return s + "." + Base64Url(h);
        }

        static byte[] ComputeHmac(string key, string data)
        {
            using (var m = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
                return m.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int r = 0;
            for (var i = 0; i < a.Length; i++) r |= a[i] ^ b[i];
            return r == 0;
        }

        static string Base64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        static byte[] Base64UrlDecodeToBytes(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
            return Convert.FromBase64String(s);
        }
    }
}
