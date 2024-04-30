using System.Security.Cryptography;
using System.Text;

namespace Anecrypt.Core;

public static class Encryption
{
    /// <summary>
    /// Encrypts a file and stores it in a new location
    /// </summary>
    /// <param name="rawFilePath">Path to unencrypted file</param>
    /// <param name="saveFolder">Path to folder where encrypted file will be stored</param>
    /// <param name="password">Password to secure encrypted file</param>
    /// <param name="cleanupRawFile">Delete raw file after encryption succeeds</param>
    /// <returns></returns>
    public static async Task Encrypt(string rawFilePath, string saveFolder, string password, bool cleanupRawFile = false)
    {
        FileInfo rawFileInfo = new FileInfo(rawFilePath);

        string encryptedFilePath = Path.Combine(saveFolder, rawFileInfo.Name) + ".anecrypt";

        using (FileStream encryptedFile = new FileStream(encryptedFilePath, FileMode.Create))
        {
            using (FileStream fileStream = new FileStream(Path.Combine(rawFilePath), FileMode.Open))
            {
                using (Aes aes = Aes.Create())
                {
                    byte[] salt = Salt();

                    // Figure out how to generate key based on user input
                    aes.Key = Hash(password, salt);

                    encryptedFile.Write(aes.IV, 0, aes.IV.Length);
                    encryptedFile.Write(salt, 0, salt.Length);

                    using (CryptoStream cryptoStream = new(encryptedFile, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        await fileStream.CopyToAsync(cryptoStream);
                    }
                }
            }
        }

        if (cleanupRawFile)
            File.Delete(rawFilePath);
    }

    /// <summary>
    /// Decrypts a file that was previously encrypted
    /// </summary>
    /// <param name="encryptedFilePath">Path to encrypted file</param>
    /// <param name="saveFolder">Path to folder where decrypted file will be stored</param>
    /// <param name="password">Password to unlock encrypted file</param>
    /// <param name="cleanupEncryptedFile">Delete encrypted file after decryption</param>
    public static async Task Decrypt(string encryptedFilePath, string saveFolder, string password, bool cleanupEncryptedFile = false)
    {
        string decryptedFilePath = Path.Combine(saveFolder, new FileInfo(encryptedFilePath).Name);

        using (FileStream encryptedFile = new FileStream(encryptedFilePath, FileMode.Open))
        {
            using (FileStream fileStream = new FileStream(decryptedFilePath.Remove(decryptedFilePath.IndexOf(".anecrypt")), FileMode.Create))
            {
                using (Aes aes = Aes.Create())
                {
                    byte[] iv = new byte[aes.IV.Length];
                    byte[] salt = new byte[32];

                    int ivBytesRead = encryptedFile.Read(iv, 0, aes.IV.Length);
                    int saltBytesRead = encryptedFile.Read(salt, 0, salt.Length);

                    // If IV or salt is not read, then we can't decrypt the file    
                    if (ivBytesRead != aes.IV.Length || saltBytesRead != salt.Length)
                        return;

                    byte[] key = Hash(password, salt);

                    using (CryptoStream cryptoStream = new(fileStream, aes.CreateDecryptor(key, iv), CryptoStreamMode.Write))
                    {
                        await encryptedFile.CopyToAsync(cryptoStream);
                    }
                }
            }
        }

        if (cleanupEncryptedFile)
            File.Delete(encryptedFilePath);
    }

    public static bool IsEncrypted(string path) => !string.IsNullOrEmpty(path) && path.EndsWith(".anecrypt");

    private static byte[] Hash(string password, byte[] salt) => SHA256.HashData(Encoding.UTF8.GetBytes(password).Concat(salt).ToArray());

    private static byte[] Salt() {
        // We are using 32 bytes for salt since we are using SHA256
        byte[] salt = new byte[32];

        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        return salt;
    }
}
