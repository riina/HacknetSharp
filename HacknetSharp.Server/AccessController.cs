using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using HacknetSharp.Server.Common.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace HacknetSharp.Server
{
    public class AccessController
    {
        private readonly ServerDatabase _db;

        public AccessController(Server server)
        {
            _db = server.Database;
        }

        public async Task<UserModel?> AuthenticateAsync(string user, string pass)
        {
            var userModel = await _db.GetAsync<string, UserModel>(user).Caf();
            if (userModel == null) return null;
            var (_, hash) = Base64Password(pass, Convert.FromBase64String(userModel.Base64Salt));
            return hash == userModel.Base64Password ? userModel : null;
        }

        public async Task<UserModel?> RegisterAsync(string user, string pass, string registrationToken)
        {
            var userModel = await _db.GetAsync<string, UserModel>(user).Caf();
            if (userModel != null) return null;
            var token = await _db.GetAsync<string, RegistrationToken>(registrationToken).Caf();
            if (token == null) return null;
            _db.Delete(token);
            var (salt, hash) = Base64Password(pass);
            userModel = new UserModel {Key = user, Base64Salt = salt, Base64Password = hash};
            _db.Add(userModel);
            await _db.SyncAsync().Caf();
            return userModel;
        }

        public async Task<bool> ChangePasswordAsync(UserModel userModel, string newPass)
        {
            (userModel.Base64Salt, userModel.Base64Password) = Base64Password(newPass);
            _db.Edit(userModel);
            await _db.SyncAsync().Caf();
            return true;
        }

        public async Task<bool> AdminChangePasswordAsync(UserModel userModel, string user, string newPass)
        {
            if (!userModel.Admin) return false;
            var targetUserModel = await _db.GetAsync<string, UserModel>(user).Caf();
            if (targetUserModel == null) return false;
            (targetUserModel.Base64Salt, targetUserModel.Base64Password) = Base64Password(newPass);
            _db.Edit(targetUserModel);
            await _db.SyncAsync().Caf();
            return true;
        }

        public async Task DeregisterNonSyncAsync(UserModel userModel, bool purge)
        {
            // this can only be executed during sync step of world update, must queue
            //var ctx = _db.Context;
            if (purge)
            {
                var player = await _db.GetAsync<string, PlayerModel>(userModel.Key).Caf();
                if (player != null)
                {
                    foreach (var person in player.Identities)
                    {
                        foreach (var system in person.Systems) _db.DeleteBulk(system.Files);
                        _db.DeleteBulk(person.Systems);
                    }

                    _db.DeleteBulk(player.Identities);
                    _db.Delete(player);
                }
            }

            _db.Delete(userModel);
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
