using System;
using System.IO;

namespace Jacere.ConsoleCredentials.Windows
{
    public class LocalAppDataCredentialSource : ICredentialSource
    {
        private readonly string _path;

        public LocalAppDataCredentialSource(string dirName, string fileName)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _path = Path.Combine(appData, dirName, fileName);
        }

        public string ReadEncryptedStringFromStorage()
        {
            return File.Exists(_path)
                ? File.ReadAllText(_path)
                : null;
        }

        public void SaveEncryptedStringToStorage(string encrypted)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            File.WriteAllText(_path, encrypted);
        }

        public void Destroy()
        {
            File.Delete(_path);
        }
    }
}
