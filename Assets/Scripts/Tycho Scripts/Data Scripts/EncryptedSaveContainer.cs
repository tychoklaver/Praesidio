using System;
using UnityEngine;

[Serializable]
public class EncryptedSaveContainer
{
    public string cipherTextBase64;
    public string ivBase64;
    public string hmacSignature;
}
