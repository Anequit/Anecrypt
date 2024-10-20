using System.Security.Cryptography;
using System.Text;

namespace Anecrypt.Core;

public static class Encryption
{
    public const int SaltLength = 32;
    public const int FileExtensionLength = 3;

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

        string encryptedFilePath = Path.Combine(saveFolder, Path.GetFileNameWithoutExtension(rawFileInfo.Name)) + ".anecrypt";

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

                    byte[] fileExtensionLength = GetFileExtensionLengthBytes(rawFileInfo.Name);

                    encryptedFile.Write(fileExtensionLength, 0, fileExtensionLength.Length);

                    byte[] fileExtension = Encoding.ASCII.GetBytes(Path.GetExtension(rawFileInfo.Name));

                    encryptedFile.Write(fileExtension, 0, fileExtension.Length);

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
        using(FileStream encryptedFile = new FileStream(encryptedFilePath, FileMode.Open))
        {
            using(Aes aes = Aes.Create())
            {
                byte[] iv = new byte[aes.IV.Length];
                byte[] salt = new byte[SaltLength];

                int ivBytesRead = encryptedFile.Read(iv, 0, aes.IV.Length);
                int saltBytesRead = encryptedFile.Read(salt, 0, salt.Length);

                byte[] fileExtensionLength = new byte[FileExtensionLength];

                encryptedFile.Read(fileExtensionLength, 0, FileExtensionLength);

                byte[] fileExtension = new byte[Convert.ToInt32(Encoding.ASCII.GetString(fileExtensionLength))];

                encryptedFile.Read(fileExtension, 0, fileExtension.Length);

                // If IV or salt is not read, then we can't decrypt the file    
                if(ivBytesRead != aes.IV.Length || saltBytesRead != salt.Length)
                    return;

                byte[] key = Hash(password, salt);

                string fileName = Path.Combine(saveFolder, Path.GetFileNameWithoutExtension(encryptedFilePath) + Encoding.ASCII.GetString(fileExtension));

                using(FileStream fileStream = new FileStream(fileName, FileMode.Create))
                {
                    using(CryptoStream cryptoStream = new(fileStream, aes.CreateDecryptor(key, iv), CryptoStreamMode.Write))
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

    private static byte[] GetFileExtensionLengthBytes(string path)
    {
        byte[] padded = new byte[FileExtensionLength];

        byte[] temp = Encoding.ASCII.GetBytes(Path.GetExtension(path).Length.ToString());

        Array.Copy(temp, padded, temp.Length);

        return padded;
    }

    private static byte[] Salt() 
    {
        byte[] salt = new byte[SaltLength];

        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        return salt;
    }
}
