using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAchievement", menuName = "Praesidio/Achievement")]
public class AchievementDefinition : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string title;
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private bool isSecret;

    [Header("Condition: unlocks when stat >= target")]
    [SerializeField] private string statKey;
    [SerializeField] private long target = 1;

    public string Id => id;
    public string Title => title;
    public string Description => description;
    public Sprite Icon => icon;
    public bool IsSecret => isSecret;
    public string StatKey => statKey;
    public long Target => target;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id)) id = name;
    }
}
