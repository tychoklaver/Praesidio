using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// Saves and loads <see cref="PlayerData"/> as an encrypted, tamper-evident file (encrypt-then-MAC):
///     JSON -> AES-256-CBC encrypt -> HMAC-SHA256 over IV + ciphertext -> stored as an EncryptedSaveContainer.
/// Three files are used: the main save, a .bak (the previous good save) and a .tmp (only while writing).
///  NOTE: The secret ships inside the game, so this stops casual save editing, not a determined attacker.
/// </summary>
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
    
    // Inspector edits must invalidate the caches.
    private void OnValidate()
    {
        _mainFilePath = _backupFilePath = _tempFilePath = null;
        _encKey = _macKey = null; 
    }

    // Uses HMAC-SHA256 as a simple key-derivation function: the same secret with a different
    // purpose label gives an unrelated 32-byte key, so the encryption and signing keys are never the same.
    private byte[] DeriveKey(string pPurpose)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(encryptionSecret));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(pPurpose));
    }

    /// <returns>True if data was written successfully.</returns>
    public bool Save(PlayerData pData)
    {
        try
        {
            // No pretty-print: the payload is encrypted anyway, whitespace is just wasted size.
            byte[] cipher = Encrypt(JsonUtility.ToJson(pData), out byte[] iv);

            var container = new EncryptedSaveContainer
            {
                cipherTextBase64 = Convert.ToBase64String(cipher),
                ivBase64 = Convert.ToBase64String(iv),
                hmacSignature = Convert.ToBase64String(ComputeHmac(iv, cipher))
            };

            WriteAtomically(JsonUtility.ToJson(container));
            Debug.Log($"{LogTag} Saved: {MainFilePath}");
            return true;
        } 
        catch (Exception ex)
        {
            Debug.LogError($"{LogTag} Saving failed: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Loads the main file, falls back to the backup, and only then starts a fresh profile.
    /// </summary>
    public PlayerData Load()
    {
        // The backup counts too: a crash mid-save can leave only the .bak behind,
        // and treating that as "no save" would overwrite recoverable data.
        if (!File.Exists(MainFilePath) && !File.Exists(BackupFilePath))
        {
            Debug.Log($"{LogTag} No save file found. Creating new profile.");
            return CreateNewProfile();
        }

        if (TryLoadFile(MainFilePath, out PlayerData data)) return data;

        Debug.LogWarning($"{LogTag} Main file missing or unusable. Trying backup...");

        if (TryLoadFile(BackupFilePath, out data))
        {
            // Restore byte-for-byte instead of calling Save: Save would first copy
            // the broken main file over the good backup.
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
        // Write to a temp file first, so a crash mid-write can't leave a half-written main file.
        File.WriteAllText(TempFilePath, pContents);

        // Replace swaps the temp file in and keeps the old main as .bak in one OS operation.
        // It needs an existing target, so the very first save is a plain move.
        if (File.Exists(MainFilePath)) File.Replace(TempFilePath, MainFilePath, BackupFilePath);
        else File.Move(TempFilePath, MainFilePath);
    }

    private bool TryLoadFile(string pFilePath, out PlayerData pData)
    {
        pData = null;
        if (!File.Exists(pFilePath)) return false;

        try
        {
            var container = JsonUtility.FromJson<EncryptedSaveContainer>(File.ReadAllText(pFilePath));

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
                Debug.LogWarning($"{LogTag} Signature mismatch for {pFilePath}. File was tampered with or corrupted.");
                return false;
            }

            pData = JsonUtility.FromJson<PlayerData>(Decrypt(cipherBytes, iv));
            return pData != null;

        }
        catch (Exception ex) // Bad JSON/Base64, wrong key, damaged ciphertext, I/O errors: all mean "unusable".
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

    private byte[] Encrypt(string pPlainText, out byte[] pIv)
    {
        using var aes = CreateAes();

        // IV (Initialisation Vector): random 16 bytes that seed the first block, so the same
        // data encrypts differently each save. Not secret, but must be new for every save.
        aes.GenerateIV();
        pIv = aes.IV;

        using var encryptor = aes.CreateEncryptor();
        byte[] plainBytes = Encoding.UTF8.GetBytes(pPlainText);
        return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
    }

    private string Decrypt(byte[] pCipherBytes, byte[] pIv)
    {
        using var aes = CreateAes();
        aes.IV = pIv; // Must be the same IV that was used to encrypt.

        using var decryptor = aes.CreateDecryptor();
        return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(pCipherBytes, 0, pCipherBytes.Length));
    }
}