namespace Jacere.ConsoleCredentials
{
    public interface ICredentialSource
    {
        string ReadEncryptedStringFromStorage();
        void SaveEncryptedStringToStorage(string encrypted);
        void Destroy();
    }
}