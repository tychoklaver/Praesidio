using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class SaveSystem : MonoBehaviour
{
    private const string LogTag = "[SaveSystem]";

    [Header("Security Settings")]
    // One secret; separate encryption and signing keys are derived from it.
    [SerializeField] private string encryptionSecret = "K!ysLj4isd@F8j9l^SKTPfNxL1B1jJ12";

    [Header("File Settings")]
    [SerializeField] private string fileName = "profile_stats.json";

    // Built lazily and cached: avoids per-call allocations and doesn't depend on Awake order.
    private string _mainFilePath;
    private string _backupFilePath;
    private string _tempFilePath;
    private byte[] _encKey, _macKey;

    private string MainFilePath => _mainFilePath ??= Path.Combine(Application.persistentDataPath, fileName);
    private string BackupFilePath => _backupFilePath ??= MainFilePath + ".bak";
    private string TempFilePath => _tempFilePath ??= MainFilePath + ".tmp";
    private byte[] EncKey => _encKey ??= DeriveKey("enc");
    private byte[] MacKey => _macKey ??= DeriveKey("mac");
    
    private void OnValidate()
    {
        // Inspector edits must invalidate the caches.
        _mainFilePath = _backupFilePath = _tempFilePath = null;
        _encKey = _macKey = null; 
    }

    // Derives two independent 32-byte keys from one secret (HMAC-SHA256 as key derivation),
    // so the encryption key and the signing key are never the same.
    private byte[] DeriveKey(string pPurpose)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(encryptionSecret));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(pPurpose));
    }

    /// <returns>True if data was written succesfully.</returns>
    public bool Save(PlayerData pData)
    {
        try
        {
            // No pretty-print: payload is encrypted anyway, whitespace is just wasted file size.
            string json = JsonUtility.ToJson(pData);
            string cipherText = Encrypt(json, out byte[] iv);

            // The container keeps cipherText, IV and signature together; loading needs all three.
            var container = new EncryptedSaveContainer
            {
                cipherTextBase64 = cipherText,
                ivBase64 = Convert.ToBase64String(iv),
                hmacSignature = Convert.ToBase64String(ComputeHmac(iv, Convert.FromBase64String(cipherText)))
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
                || string.IsNullOrEmpty(container.ivBase64)
                || string.IsNullOrEmpty(container.hmacSignature))
                return false;
            
            byte[] iv = Convert.FromBase64String(container.ivBase64);
            byte[] cipherBytes = Convert.FromBase64String(container.cipherTextBase64);
            byte[] storedHmac = Convert.FromBase64String(container.hmacSignature);

            // Verify before decrypting: tampered data never reaches the decryptor.
            // FixedTimeEquals compares in constant time; a normal == leaks timing information.
            if (!CryptographicOperations.FixedTimeEquals(ComputeHmac(iv, cipherBytes), storedHmac))
            {
                Debug.LogWarning($"{LogTag} Signature mismatch for {pFilePath}. File was tampered with.");
                return false;
            }

            string json = Decrypt(cipherBytes, iv);

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

    // HMAC: keyed hash over IV + ciphertext. Changing any byte changes the signature,
    // and without the key a valid signature can't be produced.
    private byte[] ComputeHmac(byte[] pIv, byte[] pCipherBytes)
    {
        using var hmac = new HMACSHA256(MacKey);
        hmac.TransformBlock(pIv, 0, pIv.Length, null, 0);
        hmac.TransformFinalBlock(pCipherBytes, 0, pCipherBytes.Length);
        return hmac.Hash;
    }

    private Aes CreateAes()
    {
        var aes = Aes.Create();
        aes.Key = EncKey; // AES-256 = 256-bit key = 32 bytes.

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

    private string Decrypt(byte[] pCipherBytes, byte[] pIv)
    {
        using var aes = CreateAes();
        aes.IV = pIv; // Must be the same IV that was used to decrypt.

        using var decryptor = aes.CreateDecryptor();
        return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(pCipherBytes, 0, pCipherBytes.Length));
    }
}