using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using HacknetSharp.Server.Common;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace HacknetSharp.Server
{
    public class AccessController
    {
        public Common.Server Server { get; set; } = null!;

        private ServerDatabase? _db;

        public async Task<bool> AuthenticateAsync(string user, string pass)
        {
            _db ??= Server.Database;
            var userObj = await _db.GetAsync<string, UserModel>(user);
            if (userObj == null) return false;
            var (_, hash) = Base64Password(pass, Convert.FromBase64String(userObj.Base64Salt));
            return hash == userObj.Base64Password;
        }

        public async Task<bool> RegisterAsync(string user, string pass, string registrationToken)
        {
            _db ??= Server.Database;
            var token = await _db.GetAsync<string, RegistrationToken>(registrationToken);
            if (token == null) return false;
            _db.Delete(token);
            var (salt, hash) = Base64Password(pass);
            _db.Add(new UserModel {Key = user, Base64Salt = salt, Base64Password = hash});
            await _db.SyncAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(string user, string pass, string newPass)
        {
            _db ??= Server.Database;
            var userObj = await _db.GetAsync<string, UserModel>(user);
            if (userObj == null) return false;
            var (_, hash) = Base64Password(pass, Convert.FromBase64String(userObj.Base64Salt));
            if (hash != userObj.Base64Password) return false;
            (userObj.Base64Salt, userObj.Base64Password) = Base64Password(newPass);
            _db.Edit(userObj);
            await _db.SyncAsync();
            return true;
        }

        public async Task<bool> AdminChangePasswordAsync(string user, string newPass)
        {
            _db ??= Server.Database;
            var userObj = await _db.GetAsync<string, UserModel>(user);
            if (userObj == null) return false;
            (userObj.Base64Salt, userObj.Base64Password) = Base64Password(newPass);
            _db.Edit(userObj);
            await _db.SyncAsync();
            return true;
        }

        public async Task<bool> DeregisterAsync(string user, string pass, bool purge)
        {
            _db ??= Server.Database;
            var userObj = await _db.GetAsync<string, UserModel>(user);
            if (userObj == null) return false;
            var (_, hash) = Base64Password(pass, Convert.FromBase64String(userObj.Base64Salt));
            if (hash != userObj.Base64Password) return false;
            _db.Delete(userObj);
            // TODO implement purge
            await _db.SyncAsync();
            return true;
        }

        /// <summary>
        /// Generate hash for password
        /// </summary>
        /// <param name="password">Password to hash</param>
        /// <param name="iterations">Number of iterations</param>
        /// <param name="hashLength">Length of hash</param>
        /// <param name="salt">Existing salt (optional)</param>
        /// <param name="saltLength">Salt length (ignored if salt provided)</param>
        /// <returns>Salt and hashed password</returns>
        private static (byte[] salt, byte[] hash) HashPassword(string password, int iterations = 10000,
            int hashLength = 256 / 8, byte[]? salt = null, int saltLength = 128 / 8)
        {
            if (salt == null)
            {
                salt = new byte[saltLength];
                using var r = RandomNumberGenerator.Create();
                r.GetBytes(salt);
            }

            byte[] hash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, iterations, hashLength);

            return (salt, hash);
        }

        public static (string salt, string hash) Base64Password(string password, byte[]? salt = null)
        {
            byte[] hash;
            (salt, hash) = HashPassword(password, salt: salt);
            return (Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }
    }
}
