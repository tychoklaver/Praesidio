using UnityEngine;

/// <summary>
/// Designer-facing asset describing one achievement.
/// It unlocks as soon as the stat <see cref="StatKey"/> reaches <see cref="Target"/>
/// </summary>
[CreateAssetMenu(fileName = "NewAchievement", menuName = "Praesidio/Achievement")]
public class AchievementDefinition : ScriptableObject
{
    [Tooltip("Key in the save file. Keep it stable: changing it makes players lose this achievement.")]
    [SerializeField] private string id;
    [SerializeField] private string title;
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;
    [Tooltip("Secret achievements show '???' in the menu until they are unlocked.")]
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

    // Defaults the ID to the asset name once; afterwards it stays fixed even if the asset is renamed.
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id)) id = name;
    }
}
