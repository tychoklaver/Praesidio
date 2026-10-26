using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum BuildTab
{
    Defense,
    Production
}

public class InGameUI : MonoBehaviour
{
    [System.Serializable]
    public class Stat
    {
        public string label;
        public string value;
    }

    [System.Serializable]
    public class Building
    {
        public string name;
        public Sprite icon;
        public BuildTab tab;
        public string cost;
        public Stat[] stats;
    }

    [Header("Buildings")]
    [SerializeField] Building[] buildings;
    [SerializeField] GameObject cardTemplate;
    [SerializeField] Transform defenseContent;
    [SerializeField] Transform productionContent;

    [Header("Resources")]
    [SerializeField] int wood;
    [SerializeField] int stone;
    [SerializeField] int iron;
    [SerializeField] int gold;
    [SerializeField] int workers;
    [SerializeField] int maxWorkers;
    [SerializeField] int lowAmount;
    [SerializeField] Color normalColor = new Color32(240, 226, 200, 255);
    [SerializeField] Color lowColor = new Color32(214, 72, 58, 255);
    [SerializeField] TMP_Text woodText;
    [SerializeField] TMP_Text stoneText;
    [SerializeField] TMP_Text ironText;
    [SerializeField] TMP_Text goldText;
    [SerializeField] TMP_Text workerText;

    [Header("Level and wave")]
    [SerializeField] int level = 1;
    [SerializeField] int xp;
    [SerializeField] int xpNeeded;
    [SerializeField] int wave;
    [SerializeField] int maxWave;
    [SerializeField] int enemies;
    [SerializeField] TMP_Text levelText;
    [SerializeField] TMP_Text xpText;
    [SerializeField] Image xpFill;
    [SerializeField] TMP_Text waveText;
    [SerializeField] TMP_Text enemiesText;

    [Header("Tabs")]
    [SerializeField] Button[] tabButtons;
    [SerializeField] GameObject[] tabPages;
    [SerializeField] Sprite tabSprite;
    [SerializeField] Sprite tabActiveSprite;

    [Header("Cards")]
    [SerializeField] Sprite cardSprite;
    [SerializeField] Sprite cardSelectedSprite;

    [Header("Inspector")]
    [SerializeField] GameObject overviewPanel;
    [SerializeField] GameObject infoPanel;
    [SerializeField] TMP_Text infoNameText;
    [SerializeField] TMP_Text infoStatsText;

    [Header("Speed")]
    [SerializeField] Button[] speedButtons;
    [SerializeField] Sprite buttonSprite;
    [SerializeField] Sprite buttonSelectedSprite;

    float[] speeds = { 0f, 1f, 2f };
    List<Button> cards = new List<Button>();

    void Start()
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int index = i;
            tabButtons[i].onClick.AddListener(() => SelectTab(index));
        }
        foreach (Building building in buildings)
        {
            AddCard(building);
        }
        for (int i = 0; i < speedButtons.Length; i++)
        {
            int index = i;
            speedButtons[i].onClick.AddListener(() => SetSpeed(index));
        }

        SelectTab(0);
        SetSpeed(1);
        ShowOverview();
        Refresh();
    }

    void Update()
    {
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            ShowOverview();
        }

        Refresh();
    }

    void Refresh()
    {
        SetResource(woodText, wood);
        SetResource(stoneText, stone);
        SetResource(ironText, iron);
        SetResource(goldText, gold);
        workerText.text = workers + " / " + maxWorkers;

        levelText.text = "Level " + level;
        xpText.text = xp + " / " + xpNeeded + " XP";
        if (xpNeeded > 0)
        {
            xpFill.fillAmount = Mathf.Clamp01((float)xp / xpNeeded);
        }
        else
        {
            xpFill.fillAmount = 0;
        }

        waveText.text = "Wave " + wave + " / " + maxWave;
        enemiesText.text = enemies + " enemies";
    }

    void SetResource(TMP_Text label, int amount)
    {
        label.text = amount.ToString();
        if (amount < lowAmount)
        {
            label.color = lowColor;
        }
        else
        {
            label.color = normalColor;
        }
    }

    void SelectTab(int index)
    {
        for (int i = 0; i < tabPages.Length; i++)
        {
            tabPages[i].SetActive(i == index);
            if (i == index)
            {
                tabButtons[i].image.sprite = tabActiveSprite;
            }
            else
            {
                tabButtons[i].image.sprite = tabSprite;
            }
        }
    }

    void AddCard(Building building)
    {
        Transform content = productionContent;
        if (building.tab == BuildTab.Defense)
        {
            content = defenseContent;
        }

        GameObject card = Instantiate(cardTemplate, content);
        card.name = building.name;
        card.SetActive(true);

        card.transform.Find("Name").GetComponent<TMP_Text>().text = building.name;
        card.transform.Find("Cost").GetComponent<TMP_Text>().text = building.cost;
        card.transform.Find("Info").GetComponent<TMP_Text>().text = "";
        card.transform.Find("Slot/Icon").GetComponent<Image>().sprite = building.icon;

        Button button = card.GetComponent<Button>();
        cards.Add(button);
        button.onClick.AddListener(() => SelectCard(button, building));
    }

    void SelectCard(Button selected, Building building)
    {
        foreach (Button card in cards)
        {
            if (card == selected)
            {
                card.image.sprite = cardSelectedSprite;
            }
            else
            {
                card.image.sprite = cardSprite;
            }
        }

        infoNameText.text = building.name;

        string text = StatLine("Cost", building.cost);
        foreach (Stat stat in building.stats)
        {
            text += "\n" + StatLine(stat.label, stat.value);
        }
        infoStatsText.text = text;

        overviewPanel.SetActive(false);
        infoPanel.SetActive(true);
    }

    string StatLine(string label, string value)
    {
        if (value == "")
        {
            value = "-";
        }
        return label + ": " + value;
    }

    void ShowOverview()
    {
        foreach (Button card in cards)
        {
            card.image.sprite = cardSprite;
        }

        overviewPanel.SetActive(true);
        infoPanel.SetActive(false);
    }

    void SetSpeed(int index)
    {
        Time.timeScale = speeds[index];
        for (int i = 0; i < speedButtons.Length; i++)
        {
            if (i == index)
            {
                speedButtons[i].image.sprite = buttonSelectedSprite;
            }
            else
            {
                speedButtons[i].image.sprite = buttonSprite;
            }
        }
    }
}
