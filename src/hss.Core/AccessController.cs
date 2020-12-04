using System.Threading.Tasks;
using HacknetSharp;
using HacknetSharp.Server;
using HacknetSharp.Server.Models;

namespace hss.Core
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
            return ServerUtil.ValidatePassword(pass, userModel.Hash, userModel.Salt) ? userModel : null;
        }

        public async Task<UserModel?> RegisterAsync(string user, string pass, string registrationToken)
        {
            var userModel = await _db.GetAsync<string, UserModel>(user).Caf();
            if (userModel != null) return null;
            var token = await _db.GetAsync<string, RegistrationToken>(registrationToken).Caf();
            if (token == null) return null;
            _db.Delete(token);
            var (hash, salt) = CommonUtil.HashPassword(pass);
            userModel = new UserModel {Key = user, Hash = hash, Salt = salt};
            _db.Add(userModel);
            await _db.SyncAsync().Caf();
            return userModel;
        }

        public async Task<bool> ChangePasswordAsync(UserModel userModel, string newPass)
        {
            (userModel.Hash, userModel.Salt) = CommonUtil.HashPassword(newPass);
            _db.Edit(userModel);
            await _db.SyncAsync().Caf();
            return true;
        }

        public async Task<bool> AdminChangePasswordAsync(UserModel userModel, string user, string newPass)
        {
            if (!userModel.Admin) return false;
            var targetUserModel = await _db.GetAsync<string, UserModel>(user).Caf();
            if (targetUserModel == null) return false;
            (targetUserModel.Hash, targetUserModel.Salt) = CommonUtil.HashPassword(newPass);
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
    }
}
