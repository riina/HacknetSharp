namespace HacknetSharp.Server.Lua
{
    internal interface IProxyConversion<out T> where T : class
    {
        T Generate();
    }
}
