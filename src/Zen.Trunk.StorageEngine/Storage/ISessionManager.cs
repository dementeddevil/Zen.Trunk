namespace Zen.Trunk.Storage
{
    public interface ISessionManager
    {
        /// <summary>
        /// Creates a new session.
        /// </summary>
        /// <returns></returns>
        ISession CreateSession();
    }
}