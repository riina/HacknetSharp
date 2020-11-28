using System;

namespace HacknetSharp.Server.Common.Models
{
    public class LoginModel : Model<Guid>
    {
        public virtual WorldModel World { get; set; } = null!;
        public virtual SystemModel System { get; set; } = null!;
        public virtual PersonModel? Person { get; set; }
        public virtual string User { get; set; } = null!;
        public virtual string Pass { get; set; } = null!;
    }
}
