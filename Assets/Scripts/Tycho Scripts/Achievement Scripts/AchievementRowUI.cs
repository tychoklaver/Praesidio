using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
 
/// <summary>One row in the achievements menu. Look and layout are built in the editor (prefab).</summary>
public class AchievementRowUI : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
 
    [Header("Optional")]
    [SerializeField] private Image background;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text unlockedAtText;
 
    [Header("Progress (only shown for locked achievements)")]
    [SerializeField] private GameObject progressRoot;
    [Tooltip("The gold bar inside the dark bar. Must stretch over its parent (anchors 0,0 to 1,1, offsets 0).")]
    [SerializeField] private RectTransform progressFill;
    [SerializeField] private TMP_Text progressText;
 
    [Header("Colors")]
    [SerializeField] private Color unlockedTitleColor = new(0.89f, 0.71f, 0.32f, 1f);
    [SerializeField] private Color lockedTitleColor = new(0.6f, 0.55f, 0.48f, 1f);
    [SerializeField] private Color unlockedBackground = new(0.26f, 0.19f, 0.12f, 1f);
    [SerializeField] private Color lockedBackground = new(0.16f, 0.11f, 0.08f, 1f);
    [SerializeField] private Color lockedIconColor = new(0.3f, 0.3f, 0.3f, 1f);
 
    public AchievementDefinition Definition { get; private set; }
 
    public void Bind(AchievementDefinition pDef, AchievementManager pManager)
    {
        Definition = pDef;
 
        bool unlocked = pManager.IsUnlocked(pDef);
        bool hidden = pDef.IsSecret && !unlocked; // Secret: nothing is revealed until unlocked.
 
        titleText.text = hidden ? "???" : pDef.Title;
        descriptionText.text = hidden ? "Secret achievement" : pDef.Description;
        titleText.color = unlocked ? unlockedTitleColor : lockedTitleColor;
 
        if (background != null)
            background.color = unlocked ? unlockedBackground : lockedBackground;
 
        if (iconImage != null)
        {
            iconImage.sprite = pDef.Icon;
            iconImage.enabled = pDef.Icon != null;
            iconImage.color = unlocked ? Color.white : lockedIconColor;
        }
 
        if (unlockedAtText != null)
        {
            DateTime unlockTime = default;
            bool hasDate = unlocked && pManager.TryGetUnlockedAt(pDef, out unlockTime);
 
            unlockedAtText.gameObject.SetActive(hasDate);
            if (hasDate) unlockedAtText.text = $"Unlocked\n{unlockTime:dd-MM-yyyy HH:mm}";
        }
 
        // A progress bar only makes sense for achievements with a real range (not "reach 1").
        bool showProgress = !unlocked && !hidden && pDef.Target > 1;
 
        if (progressRoot != null) progressRoot.SetActive(showProgress);
        if (!showProgress) return;
 
        long current = Math.Min(pManager.GetCurrentValue(pDef), pDef.Target);
 
        if (progressFill == null)
        {
            Debug.LogWarning("[AchievementRowUI] 'Progress Fill' is not assigned on the row prefab, so the bar can't change.", this);
        }
        else
        {
            // Force a stretch rect that only differs in its right edge, whatever the prefab had set.
            float ratio = Mathf.Clamp01((float)current / pDef.Target);
            progressFill.anchorMin = Vector2.zero;
            progressFill.anchorMax = new Vector2(ratio, 1f);
            progressFill.offsetMin = Vector2.zero;
            progressFill.offsetMax = Vector2.zero;
        }
 
        if (progressText != null)
            progressText.text = $"{current} / {pDef.Target}";
    }
}