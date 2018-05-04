using System;

namespace Jacere.ConsoleCredentials.Windows
{
    public class EnvironmentVariablePersistentKeySource : IPersistentKeySource
    {
        private readonly string _name;

        public EnvironmentVariablePersistentKeySource(string environmentVariableName)
        {
            _name = environmentVariableName;
        }

        public bool HasKey()
        {
            return !string.IsNullOrEmpty(Get());
        }

        public string Get()
        {
            return Environment.GetEnvironmentVariable(_name);
        }
    }
}
