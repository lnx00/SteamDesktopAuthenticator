using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Newtonsoft.Json;
using SteamAuth;

namespace SteamDesktopAuthenticator;

public class Manifest
{
    [JsonProperty("encrypted")] public bool Encrypted { get; set; }

    [JsonProperty("first_run")] public bool FirstRun { get; set; } = true;

    [JsonProperty("entries")] public List<ManifestEntry> Entries { get; set; }

    [JsonProperty("periodic_checking")] public bool PeriodicChecking { get; set; }

    [JsonProperty("periodic_checking_interval")]
    public int PeriodicCheckingInterval { get; set; } = 5;

    [JsonProperty("periodic_checking_checkall")]
    public bool CheckAllAccounts { get; set; }

    [JsonProperty("auto_confirm_market_transactions")]
    public bool AutoConfirmMarketTransactions { get; set; }

    [JsonProperty("auto_confirm_trades")] public bool AutoConfirmTrades { get; set; }

    private static Manifest _manifest { get; set; }

    public static string GetExecutableDir()
    {
        return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
    }

    public static Manifest GetManifest(bool forceLoad = false)
    {
        // Check if already staticly loaded
        if (_manifest != null && !forceLoad)
        {
            return _manifest;
        }

        // Find config dir and manifest file
        var maDir = GetExecutableDir() + "/maFiles/";
        var manifestFile = maDir + "manifest.json";

        // If there's no config dir, create it
        if (!Directory.Exists(maDir))
        {
            _manifest = GenerateNewManifest();
            return _manifest;
        }

        // If there's no manifest, throw exception
        if (!File.Exists(manifestFile))
        {
            throw new ManifestParseException();
        }

        try
        {
            var manifestContents = File.ReadAllText(manifestFile);
            _manifest = JsonConvert.DeserializeObject<Manifest>(manifestContents);

            if (_manifest.Encrypted && _manifest.Entries.Count == 0)
            {
                _manifest.Encrypted = false;
                _manifest.Save();
            }

            _manifest.RecomputeExistingEntries();

            return _manifest;
        }
        catch (Exception)
        {
            throw new ManifestParseException();
        }
    }

    public static Manifest GenerateNewManifest(bool scanDir = false)
    {
        // No directory means no manifest file anyways.
        var newManifest = new Manifest();
        newManifest.Encrypted = false;
        newManifest.PeriodicCheckingInterval = 5;
        newManifest.PeriodicChecking = false;
        newManifest.AutoConfirmMarketTransactions = false;
        newManifest.AutoConfirmTrades = false;
        newManifest.Entries = new List<ManifestEntry>();
        newManifest.FirstRun = true;

        // Take a pre-manifest version and generate a manifest for it.
        if (scanDir)
        {
            var maDir = GetExecutableDir() + "/maFiles/";
            if (Directory.Exists(maDir))
            {
                var dir = new DirectoryInfo(maDir);
                var files = dir.GetFiles();

                foreach (var file in files)
                {
                    if (file.Extension != ".maFile")
                    {
                        continue;
                    }

                    var contents = File.ReadAllText(file.FullName);
                    try
                    {
                        var account = JsonConvert.DeserializeObject<SteamGuardAccount>(contents);
                        var newEntry = new ManifestEntry
                        {
                            Filename = file.Name,
                            SteamID = account.Session.SteamID
                        };
                        newManifest.Entries.Add(newEntry);
                    }
                    catch (Exception)
                    {
                        throw new MaFileEncryptedException();
                    }
                }

                if (newManifest.Entries.Count > 0)
                {
                    newManifest.Save();
                    newManifest.PromptSetupPassKey(
                        "This version of SDA has encryption. Please enter a passkey below, or hit cancel to remain unencrypted");
                }
            }
        }

        if (newManifest.Save())
        {
            return newManifest;
        }

        return null;
    }

    public string PromptForPassKey()
    {
        if (!Encrypted)
        {
            throw new ManifestNotEncryptedException();
        }

        var passKeyValid = false;
        string passKey = null;
        while (!passKeyValid)
        {
            var passKeyForm = new InputForm("Please enter your encryption passkey.", true);
            passKeyForm.ShowDialog();
            if (!passKeyForm.Canceled)
            {
                passKey = passKeyForm.txtBox.Text;
                passKeyValid = VerifyPasskey(passKey);
                if (!passKeyValid)
                {
                    MessageBox.Show("That passkey is invalid.");
                }
            }
            else
            {
                return null;
            }
        }

        return passKey;
    }

    public string PromptSetupPassKey(string initialPrompt = "Enter passkey, or hit cancel to remain unencrypted.")
    {
        var newPassKeyForm = new InputForm(initialPrompt);
        newPassKeyForm.ShowDialog();
        if (newPassKeyForm.Canceled || newPassKeyForm.txtBox.Text.Length == 0)
        {
            MessageBox.Show(
                "WARNING: You chose to not encrypt your files. Doing so imposes a security risk for yourself. If an attacker were to gain access to your computer, they could completely lock you out of your account and steal all your items.");
            return null;
        }

        var newPassKeyForm2 = new InputForm("Confirm new passkey.");
        newPassKeyForm2.ShowDialog();
        if (newPassKeyForm2.Canceled)
        {
            MessageBox.Show(
                "WARNING: You chose to not encrypt your files. Doing so imposes a security risk for yourself. If an attacker were to gain access to your computer, they could completely lock you out of your account and steal all your items.");
            return null;
        }

        var newPassKey = newPassKeyForm.txtBox.Text;
        var confirmPassKey = newPassKeyForm2.txtBox.Text;

        if (newPassKey != confirmPassKey)
        {
            MessageBox.Show("Passkeys do not match.");
            return null;
        }

        if (!ChangeEncryptionKey(null, newPassKey))
        {
            MessageBox.Show("Unable to set passkey.");
            return null;
        }

        MessageBox.Show("Passkey successfully set.");

        return newPassKey;
    }

    public SteamGuardAccount[] GetAllAccounts(string passKey = null, int limit = -1)
    {
        if (passKey == null && Encrypted)
        {
            return new SteamGuardAccount[0];
        }

        var maDir = GetExecutableDir() + "/maFiles/";

        var accounts = new List<SteamGuardAccount>();
        foreach (var entry in Entries)
        {
            var fileText = File.ReadAllText(maDir + entry.Filename);
            if (Encrypted)
            {
                var decryptedText = FileEncryptor.DecryptData(passKey, entry.Salt, entry.IV, fileText);
                if (decryptedText == null)
                {
                    return new SteamGuardAccount[0];
                }

                fileText = decryptedText;
            }

            var account = JsonConvert.DeserializeObject<SteamGuardAccount>(fileText);
            if (account == null)
            {
                continue;
            }

            accounts.Add(account);

            if (limit != -1 && limit >= accounts.Count)
            {
                break;
            }
        }

        return accounts.ToArray();
    }

    public bool ChangeEncryptionKey(string oldKey, string newKey)
    {
        if (Encrypted)
        {
            if (!VerifyPasskey(oldKey))
            {
                return false;
            }
        }

        var toEncrypt = newKey != null;

        var maDir = GetExecutableDir() + "/maFiles/";
        for (var i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            var filename = maDir + entry.Filename;
            if (!File.Exists(filename))
            {
                continue;
            }

            var fileContents = File.ReadAllText(filename);
            if (Encrypted)
            {
                fileContents = FileEncryptor.DecryptData(oldKey, entry.Salt, entry.IV, fileContents);
            }

            string newSalt = null;
            string newIV = null;
            var toWriteFileContents = fileContents;

            if (toEncrypt)
            {
                newSalt = FileEncryptor.GetRandomSalt();
                newIV = FileEncryptor.GetInitializationVector();
                toWriteFileContents = FileEncryptor.EncryptData(newKey, newSalt, newIV, fileContents);
            }

            File.WriteAllText(filename, toWriteFileContents);
            entry.IV = newIV;
            entry.Salt = newSalt;
        }

        Encrypted = toEncrypt;

        Save();
        return true;
    }

    public bool VerifyPasskey(string passkey)
    {
        if (!Encrypted || Entries.Count == 0)
        {
            return true;
        }

        var accounts = GetAllAccounts(passkey, 1);
        return accounts != null && accounts.Length == 1;
    }

    public bool RemoveAccount(SteamGuardAccount account, bool deleteMaFile = true)
    {
        var entry = (from e in Entries where e.SteamID == account.Session.SteamID select e).FirstOrDefault();
        if (entry == null)
        {
            return true; // If something never existed, did you do what they asked?
        }

        var maDir = GetExecutableDir() + "/maFiles/";
        var filename = maDir + entry.Filename;
        Entries.Remove(entry);

        if (Entries.Count == 0)
        {
            Encrypted = false;
        }

        if (Save() && deleteMaFile)
        {
            try
            {
                File.Delete(filename);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        return false;
    }

    public bool SaveAccount(SteamGuardAccount account, bool encrypt, string passKey = null)
    {
        if (encrypt && string.IsNullOrEmpty(passKey))
        {
            return false;
        }

        if (!encrypt && Encrypted)
        {
            return false;
        }

        string salt = null;
        string iV = null;
        var jsonAccount = JsonConvert.SerializeObject(account);

        if (encrypt)
        {
            salt = FileEncryptor.GetRandomSalt();
            iV = FileEncryptor.GetInitializationVector();
            var encrypted = FileEncryptor.EncryptData(passKey, salt, iV, jsonAccount);
            if (encrypted == null)
            {
                return false;
            }

            jsonAccount = encrypted;
        }

        var maDir = GetExecutableDir() + "/maFiles/";
        var filename = account.Session.SteamID + ".maFile";

        var newEntry = new ManifestEntry
        {
            SteamID = account.Session.SteamID,
            IV = iV,
            Salt = salt,
            Filename = filename
        };

        var foundExistingEntry = false;
        for (var i = 0; i < Entries.Count; i++)
        {
            if (Entries[i].SteamID == account.Session.SteamID)
            {
                Entries[i] = newEntry;
                foundExistingEntry = true;
                break;
            }
        }

        if (!foundExistingEntry)
        {
            Entries.Add(newEntry);
        }

        var wasEncrypted = Encrypted;
        Encrypted = encrypt || Encrypted;

        if (!Save())
        {
            Encrypted = wasEncrypted;
            return false;
        }

        try
        {
            File.WriteAllText(maDir + filename, jsonAccount);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool Save()
    {
        var maDir = GetExecutableDir() + "/maFiles/";
        var filename = maDir + "manifest.json";
        if (!Directory.Exists(maDir))
        {
            try
            {
                Directory.CreateDirectory(maDir);
            }
            catch (Exception)
            {
                return false;
            }
        }

        try
        {
            var contents = JsonConvert.SerializeObject(this);
            File.WriteAllText(filename, contents);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void RecomputeExistingEntries()
    {
        var newEntries = new List<ManifestEntry>();
        var maDir = GetExecutableDir() + "/maFiles/";

        foreach (var entry in Entries)
        {
            var filename = maDir + entry.Filename;
            if (File.Exists(filename))
            {
                newEntries.Add(entry);
            }
        }

        Entries = newEntries;

        if (Entries.Count == 0)
        {
            Encrypted = false;
        }
    }

    public void MoveEntry(int from, int to)
    {
        if (from < 0 || to < 0 || from > Entries.Count || to > Entries.Count - 1)
        {
            return;
        }

        var sel = Entries[from];
        Entries.RemoveAt(from);
        Entries.Insert(to, sel);
        Save();
    }

    public class IncorrectPassKeyException : Exception
    {
    }

    public class ManifestNotEncryptedException : Exception
    {
    }

    public class ManifestEntry
    {
        [JsonProperty("encryption_iv")] public string IV { get; set; }

        [JsonProperty("encryption_salt")] public string Salt { get; set; }

        [JsonProperty("filename")] public string Filename { get; set; }

        [JsonProperty("steamid")] public ulong SteamID { get; set; }
    }
}