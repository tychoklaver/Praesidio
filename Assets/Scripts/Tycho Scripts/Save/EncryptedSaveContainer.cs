using System;

/// <summary>
/// What is actually written to disk: the encrypted PlayerData plus everything needed to verify
/// and decrypt it (all Base64, so it is JSON-safe). Loading needs all three fields.
/// </summary>
[Serializable]
public class EncryptedSaveContainer
{
    public string cipherTextBase64;
    public string ivBase64;
    /// <summary>Signature over IV + ciphertext; proves the file was not modified.</summary>
    public string hmacSignature;
}
