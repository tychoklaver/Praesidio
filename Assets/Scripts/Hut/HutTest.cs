using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Praesidio.HutTest
{
    public class HutTestPanel : MonoBehaviour
    {
        [Header("Building")]
        [SerializeField] string buildingName = "Lumber Jack Hut";
        [SerializeField] Sprite buildingSprite;
        [SerializeField] int maxWorkers = 5;
        [SerializeField] int assignedWorkers = 3;

        [Header("Villagers")]
        [SerializeField] Sprite[] villagerPortraits;
        [SerializeField] Sprite filledSlot;
        [SerializeField] Sprite emptySlot;

        [Header("UI")]
        [SerializeField] Image buildingImage;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text counterText;
        [SerializeField] Button minusButton;
        [SerializeField] Button plusButton;
        [SerializeField] Transform slotParent;

        void Start()
        {
            plusButton.onClick.AddListener(AddWorker);
            minusButton.onClick.AddListener(RemoveWorker);

            UpdateUI();
        }

        void AddWorker()
        {
            if (assignedWorkers < maxWorkers)
            {
                assignedWorkers++;
                UpdateUI();
            }
        }

        void RemoveWorker()
        {
            if (assignedWorkers > 0)
            {
                assignedWorkers--;
                UpdateUI();
            }
        }

        void UpdateUI()
        {
            nameText.text = buildingName;
            counterText.text = $"{assignedWorkers}/{maxWorkers}";
            buildingImage.sprite = buildingSprite;

            for (int i = slotParent.childCount - 1; i >= 0; i--)
                Destroy(slotParent.GetChild(i).gameObject);

            for (int i = 0; i < maxWorkers; i++)
            {
                GameObject slot = new GameObject("Slot", typeof(RectTransform), typeof(Image));
                slot.transform.SetParent(slotParent, false);

                RectTransform slotRect = slot.GetComponent<RectTransform>();
                slotRect.sizeDelta = new Vector2(96, 102);

                float angle = Mathf.Lerp(180f, 0f, i / (float)(maxWorkers - 1)) * Mathf.Deg2Rad;

                slotRect.anchoredPosition = new Vector2(
                    Mathf.Cos(angle) * 250f,
                    Mathf.Sin(angle) * 220f
                );

                Image slotImage = slot.GetComponent<Image>();
                slotImage.sprite = i < assignedWorkers ? filledSlot : emptySlot;

                if (i < assignedWorkers && villagerPortraits.Length > 0)
                {
                    GameObject portrait = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                    portrait.transform.SetParent(slot.transform, false);

                    Image portraitImage = portrait.GetComponent<Image>();
                    portraitImage.sprite = villagerPortraits[i % villagerPortraits.Length];
                    portraitImage.preserveAspect = true;

                    RectTransform portraitRect = portrait.GetComponent<RectTransform>();
                    portraitRect.sizeDelta = new Vector2(64, 64);
                    portraitRect.anchoredPosition = new Vector2(0, 4);
                }
            }

            minusButton.interactable = assignedWorkers > 0;
            plusButton.interactable = assignedWorkers < maxWorkers;
        }
    }
}