namespace Jacere.ConsoleCredentials
{
    public interface IPersistentKeySource
    {
        bool HasKey();
        string Get();
    }
}