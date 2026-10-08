using UnityEngine;
using UnityEngine.InputSystem;

public class TestManager : MonoBehaviour
{
    [SerializeField] private SaveSystem saveSystem;
    private PlayerData currentData;

    void Start()
    {
        if (saveSystem != null)
        {
            currentData = saveSystem.Load();
        } else
        {
            Debug.LogError($"[ScoreTest] Didn't couple savesystem in inspector.");
        }
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame) AddScore();
    }

    public void AddScore()
    {
        if (currentData == null) return;

        currentData.totalScore += 1;

        saveSystem.Save(currentData);

        Debug.Log($"[ScoreTest] Current score: {currentData.totalScore}");
    }

    [ContextMenu("Test: Print decrypted JSON in console.")]
    public void TestPrintDecryptedData()
    {
        if (currentData != null)
        {
            string json = JsonUtility.ToJson(currentData, true);
            Debug.Log($"\n{json}");
        }
    }
}
