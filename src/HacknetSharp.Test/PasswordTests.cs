using HacknetSharp.Server;
using NUnit.Framework;

namespace HacknetSharp.Test
{
    public class PasswordTests
    {
        [Test]
        public void Password_Simple_Validates()
        {
            var pw = ServerUtil.HashPassword("pass1");
            Assert.That(ServerUtil.ValidatePassword("pass2", pw), Is.False);
            Assert.That(ServerUtil.ValidatePassword("pass1", pw), Is.True);
        }
    }
}
