using System.Threading.Tasks;
using HacknetSharp;
using HacknetSharp.Server;
using HacknetSharp.Server.EF;
using HacknetSharp.Server.Models;

namespace hss
{
    public class AccessController
    {
        private readonly ServerDatabase _db;
        private readonly Spawn _spawn;

        public AccessController(Server server)
        {
            _db = server.EfDatabase;
            _spawn = new Spawn(_db);
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
            var (hash, salt) = ServerUtil.HashPassword(pass);
            userModel = _spawn.User(user, hash, salt, false);
            _db.Add(userModel);
            await _db.SyncAsync().Caf();
            return userModel;
        }

        public async Task<bool> ChangePasswordAsync(UserModel userModel, string newPass)
        {
            (userModel.Hash, userModel.Salt) = ServerUtil.HashPassword(newPass);
            _db.Update(userModel);
            await _db.SyncAsync().Caf();
            return true;
        }

        public async Task<bool> AdminChangePasswordAsync(UserModel userModel, string user, string newPass)
        {
            if (!userModel.Admin) return false;
            var targetUserModel = await _db.GetAsync<string, UserModel>(user).Caf();
            if (targetUserModel == null) return false;
            (targetUserModel.Hash, targetUserModel.Salt) = ServerUtil.HashPassword(newPass);
            _db.Update(targetUserModel);
            await _db.SyncAsync().Caf();
            return true;
        }

        public void Deregister(UserModel userModel, bool purge)
        {
            _spawn.RemoveUser(userModel);
        }
    }
}
