using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Jacere.ConsoleCredentials
{
    public class CredentialStorage
    {
        private static ICredentialSource _source;
        private static IPersistentKeySource _persistentKeySource;

        private readonly string[] _groupsEncrypted;
        private readonly Dictionary<string, string> _groupDict;

        private string _secretKey;

        public static void Use(ICredentialSource source)
        {
            _source = source;
        }

        public static void Use(IPersistentKeySource persistentKeySource)
        {
            _persistentKeySource = persistentKeySource;
        }

        public static void CreatePassword()
        {
            if (_persistentKeySource != null && _persistentKeySource.HasKey())
            {
                throw new Exception("cannot create new key when persistent key is available");
            }

            var newSecretKey = AcquireNewSecretKey();
            var storage = new CredentialStorage(null);

            storage.UpdateSecretKey(newSecretKey);
        }

        private static string AcquireNewSecretKey()
        {
            return ReadHidden("New Secret Key", true);
        }

        private static string AcquireSecretKey()
        {
            return ReadHidden("Secret Key", true);
        }

        public static CredentialStorage Open()
        {
            var persistentKey = _persistentKeySource?.Get();

            var key = persistentKey ?? AcquireSecretKey();

            return new CredentialStorage(key, persistentKey != null);
        }

        private CredentialStorage(string secretKey, bool allowMatchFailure = false)
        {
            _secretKey = secretKey;

            var dictEncoded = _source.ReadEncryptedStringFromStorage();
            if (dictEncoded != null)
            {
                var dictJson = Encoding.UTF8.GetString(Convert.FromBase64String(dictEncoded));
                _groupsEncrypted = JsonConvert.DeserializeObject<string[]>(dictJson);
            }

            string groupJson = null;
            var otherGroups = new List<string>();

            if (_groupsEncrypted != null && secretKey != null)
            {
                foreach (var groupEncrypted in _groupsEncrypted)
                {
                    if (groupJson != null)
                    {
                        otherGroups.Add(groupEncrypted);
                        continue;
                    }

                    try
                    {
                        groupJson = StringCipher.Decrypt(groupEncrypted, secretKey);
                    }
                    catch (Exception)
                    {
                        // wrong password for this group
                        otherGroups.Add(groupEncrypted);
                    }
                }
            }

            if (groupJson != null)
            {
                _groupsEncrypted = otherGroups.ToArray();
                _groupDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(groupJson);
            }
            else
            {
                if (secretKey == null || allowMatchFailure)
                {
                    _groupDict = new Dictionary<string, string>();
                }
                else
                {
                    throw new Exception("failed to decrypt a group with the given key");
                }
            }
        }

        public void Destroy()
        {
            _groupDict.Clear();
            _source.Destroy();
        }

        public void Delete(string key)
        {
            _groupDict.Remove(key);
            Save();
        }

        public T Get<T>(string key)
        {
            return JsonConvert.DeserializeObject<T>(_groupDict[key]);
        }

        public IEnumerable<string> Get()
        {
            return _groupDict.Keys;
        }

        public void Set(Type type)
        {
            var cred = AcquireNewType(type);
            Set(cred.Key, cred.Value);
        }

        private void Set(string name, object value)
        {
            _groupDict[name] = JsonConvert.SerializeObject(value);
            Save();
        }

        public void UpdateSecretKey()
        {
            var newSecretKey = AcquireNewSecretKey();
            UpdateSecretKey(newSecretKey);
        }

        private void UpdateSecretKey(string newSecretKey)
        {
            _secretKey = newSecretKey;
            Save();
        }

        private void Save()
        {
            var groupEncrypted = StringCipher.Encrypt(JsonConvert.SerializeObject(_groupDict), _secretKey);

            var groupsEncrypted = _groupsEncrypted?.ToList() ?? new List<string>();
            groupsEncrypted.Add(groupEncrypted);

            var dictJson = JsonConvert.SerializeObject(groupsEncrypted);
            var dictEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(dictJson));

            _source.SaveEncryptedStringToStorage(dictEncoded);
        }

        private static KeyValuePair<string, object> AcquireNewType(Type type)
        {
            Console.Write("Name: ");
            var name = Console.ReadLine();

            var instance = Activator.CreateInstance(type);

            var props = type
                .GetProperties()
                .Where(x => x.CanWrite);

            foreach (var prop in props)
            {
                if (prop.Name == "Name")
                {
                    prop.SetValue(instance, name);
                }
                else
                {
                    var passwordAttr = prop.GetCustomAttributes(typeof(PasswordPropertyTextAttribute), true)
                        .Cast<PasswordPropertyTextAttribute>()
                        .FirstOrDefault();

                    if (passwordAttr != null && passwordAttr.Password)
                    {
                        // read with asterisk replacement
                        var value = ReadHidden(prop.Name);
                        prop.SetValue(instance, value);
                    }
                    else
                    {
                        // read simple values
                        var value = ReadSimple(prop.Name);
                        prop.SetValue(instance, value);
                    }
                }
            }

            return new KeyValuePair<string, object>(name, instance);
        }

        private static string ReadSimple(string prompt)
        {
            Console.Write($"{prompt}: ");
            var value = Console.ReadLine();

            return value;
        }

        private static string ReadHidden(string prompt, bool clearLineWhenComplete = false)
        {
            Console.Write($"{prompt}: ");

            var value = new StringBuilder();

            var top = Console.CursorTop;
            var left = Console.CursorLeft;

            while (true)
            {
                var cki = Console.ReadKey(true);

                if (cki.Key == ConsoleKey.Enter)
                {
                    break;
                }

                if (cki.Key == ConsoleKey.Escape) break;

                if (cki.Key == ConsoleKey.Backspace)
                {
                    if (value.Length > 0)
                    {
                        Console.SetCursorPosition(left + value.Length - 1, top);
                        Console.Write(' ');
                        Console.SetCursorPosition(left + value.Length - 1, top);
                        value.Remove(value.Length - 1, 1);
                    }
                }
                else
                {
                    value.Append(cki.KeyChar);
                    Console.SetCursorPosition(left + value.Length - 1, top);
                    Console.Write('*');
                }
            }

            if (clearLineWhenComplete)
            {
                var width = Console.CursorLeft;
                Console.SetCursorPosition(0, top);
                Console.Write(new string(' ', width));
                Console.SetCursorPosition(0, top);
            }
            else
            {
                Console.WriteLine();
            }

            return value.ToString();
        }
    }
}