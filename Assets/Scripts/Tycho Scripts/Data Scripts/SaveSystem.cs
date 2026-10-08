using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    private const string LogTag = "[SaveSystem]";
    private const int KeyLengthBytes = 32; // AES-256 = 256-bit key = 32 bytes

    [Header("Security Settings")]
    // Symmetric key: the same secret both encrypts and decrypts.
    [SerializeField] private string encryptionSecret = "K!ysLj4isd@F8j9l^SKTPfNxL1B1jJ12";

    [Header("File Settings")]
    [SerializeField] private string fileName = "profile_stats.json";

    // Built lazily and cached: avoids per-call allocations and doesn't depend on Awake order.
    private string _mainFilePath;
    private string _backupFilePath;
    private string _tempFilePath;
    private byte[] _keyBytes;

    private string MainFilePath => _mainFilePath ??= Path.Combine(Application.persistentDataPath, fileName);
    private string BackupFilePath => _backupFilePath ??= MainFilePath + ".bak";
    private string TempFilePath => _tempFilePath ??= MainFilePath + ".tmp";
    private byte[] KeyBytes => _keyBytes ??= Encoding.UTF8.GetBytes(encryptionSecret);

    private void OnValidate()
    {
        // Inspector edits must invalidate the caches.
        _mainFilePath = _backupFilePath = _tempFilePath = null;
        _keyBytes = null;

        // Aes.Key throws at runtime unless the key is exactly 32 bytes.
        if (Encoding.UTF8.GetByteCount(encryptionSecret) != KeyLengthBytes)
            Debug.LogWarning($"{LogTag} encryptionSecret must be exactly {KeyLengthBytes} bytes (ASCII characters).");
        
    }

    /// <returns>True if data was written succesfully.</returns>
    public bool Save(PlayerData pData)
    {
        try
        {
            // No pretty-print: payload is encrypted anyway, whitespace is just wasted file size.
            string json = JsonUtility.ToJson(pData);
            string cipherText = Encrypt(json, out byte[] iv);

            // The container keeps the cipherText and its IV together; decryption needs both.
            var container = new EncryptedSaveContainer
            {
                cipherTextBase64 = cipherText,
                ivBase64 = Convert.ToBase64String(iv)
            };

            WriteAtomically(JsonUtility.ToJson(container));
            Debug.Log($"{LogTag} Data saved succesfully: {MainFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.Log($"{LogTag} Saving failed: {ex}");
            return false;
        }
    }

    public PlayerData Load()
    {
        bool hasMain = File.Exists(MainFilePath);

        // Checking the backup too: a crash mid-save can leave only the .bak behind,
        // and treating that as "no save" would overwrite recoverable data.
        if (!hasMain && !File.Exists(BackupFilePath))
        {
            Debug.Log($"{LogTag} No save file found. Creating new profile.");
            return CreateNewProfile();
        }

        if (hasMain && TryLoadFile(MainFilePath, out PlayerData data))
            return data;

        Debug.Log($"{LogTag} Main file missing or corrupted. Attempting backup recovery...");

        if (File.Exists(BackupFilePath) && TryLoadFile(BackupFilePath, out data))
        {
            // Restore byte-for-byte instead of calling Save, Save would first copy
            // the corrupted main file over the good backup.
            try { File.Copy(BackupFilePath, MainFilePath, true); }
            catch (Exception ex) { Debug.LogWarning($"{LogTag} Backup loaded, but restoring main failed: {ex}"); }

            Debug.Log($"{LogTag} Backup recovered.");
            return data;
        }

        Debug.LogError($"{LogTag} Main and backup files are unusable. Starting new profile.");
        return CreateNewProfile();
    }

    private PlayerData CreateNewProfile()
    {
        PlayerData newData = new();
        Save(newData);
        return newData;
    }

    private void WriteAtomically(string pContents)
    {
        // Write to a temp file first so a crash mid-write can't leave a half-written main file.
        File.WriteAllText(TempFilePath, pContents);

        if(File.Exists(MainFilePath))
        {
            File.Copy(MainFilePath, BackupFilePath, true);
            File.Delete(MainFilePath);
        }

        File.Move(TempFilePath, MainFilePath);
    }

    private bool TryLoadFile(string pFilePath, out PlayerData pData)
    {
        pData = null;

        try
        {
            string containerJson = File.ReadAllText(pFilePath);
            var container = JsonUtility.FromJson<EncryptedSaveContainer>(containerJson);

            if (container == null
                || string.IsNullOrEmpty(container.cipherTextBase64)
                || string.IsNullOrEmpty(container.ivBase64))
                return false;
            
            byte[] iv = Convert.FromBase64String(container.ivBase64);
            string json = Decrypt(container.cipherTextBase64, iv);

            pData = JsonUtility.FromJson<PlayerData>(json);
            return pData != null;
        }
        catch (CryptographicException)
        {
            // Thrown for a wrong key, invalid padding, or damaged ciphertext.
            Debug.LogWarning($"{LogTag} Decryption failed for {pFilePath}. File was modified or corrupted.");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"{LogTag} Failed to load {pFilePath}: {ex.Message}");
            return false;
        }
    }

    private Aes CreateAes()
    {
        var aes = Aes.Create();
        aes.Key = KeyBytes;

        // CBC (Cipher Block Chaining): each 16-byte block is mixed with the previous
        // encrypted block, so identical plaintext blocks don't produce identical output.
        aes.Mode = CipherMode.CBC;

        // AES only works on full 16-byte blocks. PKCS7 pads the last block up to size
        // and strips the padding again on decryption.
        aes.Padding = PaddingMode.PKCS7;

        // Mode and padding are set explicitly so the file format never depends on platform defaults.
        return aes;
    }

    private string Encrypt(string pPlainText, out byte[] pIv)
    {
        using var aes = CreateAes();

        // IV (Initialisation Vector): random 16 bytes that seed the first block, so the same
        // data encrypts differently each save. Not secret, but must be new for every save.
        aes.GenerateIV(); // Fresh IV per save.
        pIv = aes.IV;

        using var encryptor = aes.CreateEncryptor();
        byte[] plainBytes = Encoding.UTF8.GetBytes(pPlainText);

        // Encrypts the whole buffer in one call; Base64 turns the raw bytes into JSON-safe text.
        return Convert.ToBase64String(encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length));
    }

    private string Decrypt(string pCipherTextBase64, byte[] pIv)
    {
        using var aes = CreateAes();
        aes.IV = pIv; // Must be the same IV that was used to decrypt.

        using var decryptor = aes.CreateDecryptor();
        byte[] cipherBytes = Convert.FromBase64String(pCipherTextBase64);
        return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length));
    }
}