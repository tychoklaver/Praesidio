using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SkillBranch
{
    Core,
    Economy,
    Workers,
    Defense,
    Walls
}

public class SkillTreeUI : MonoBehaviour
{
    [System.Serializable]
    public class Skill
    {
        public string name;
        public Sprite icon;
        public SkillBranch branch;
        public int q;
        public int r;
        public string description;
        public int cost = 1;
        public string requires;
        public bool learned;
    }

    [Header("Skills")]
    [SerializeField] Skill[] skills;
    [SerializeField] int skillPoints;
    [SerializeField] GameObject skillTemplate;
    [SerializeField] Transform skillContent;
    [SerializeField] float hexSize = 68;

    [Header("Branch colors")]
    [SerializeField] Color coreColor;
    [SerializeField] Color economyColor;
    [SerializeField] Color workersColor;
    [SerializeField] Color defenseColor;
    [SerializeField] Color wallsColor;

    [Header("State colors")]
    [SerializeField] Color availableBorder;
    [SerializeField] Color availableFill;
    [SerializeField] Color lockedBorder;
    [SerializeField] Color lockedFill;
    [SerializeField] Color learnedIcon;
    [SerializeField] Color availableIcon;
    [SerializeField] Color lockedIcon;
    [SerializeField] Color normalText;
    [SerializeField] Color lockedText;

    [Header("UI")]
    [SerializeField] GameObject skillTreePanel;
    [SerializeField] CanvasGroup treeGroup;
    [SerializeField] CanvasGroup infoGroup;
    [SerializeField] RectTransform infoPanelRect;
    [SerializeField] TMP_Text skillPointsText;
    [SerializeField] TMP_Text infoNameText;
    [SerializeField] TMP_Text infoStatusText;
    [SerializeField] TMP_Text infoDescriptionText;
    [SerializeField] TMP_Text infoCostText;
    [SerializeField] Button learnButton;
    [SerializeField] Button resetButton;
    [SerializeField] Button closeButton;
    [SerializeField] string backScene = "MainMenu";

    GameObject[] hexes;
    int selected = -1;
    bool closing;
    float pointsScale = 1;

    void Start()
    {
        hexes = new GameObject[skills.Length];
        for (int i = 0; i < skills.Length; i++)
        {
            hexes[i] = AddHex(i);
        }

        learnButton.onClick.AddListener(LearnSelected);
        resetButton.onClick.AddListener(ResetSkills);
        closeButton.onClick.AddListener(Close);

        treeGroup.alpha = 0;
        infoGroup.alpha = 0;
        infoGroup.blocksRaycasts = false;
        Refresh();
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        if (closing)
        {
            treeGroup.alpha = Mathf.MoveTowards(treeGroup.alpha, 0, dt * 5);
            if (treeGroup.alpha == 0)
            {
                skillTreePanel.SetActive(false);
            }
        }
        else
        {
            treeGroup.alpha = Mathf.MoveTowards(treeGroup.alpha, 1, dt * 4);
        }

        float infoTarget = 0;
        if (selected >= 0)
        {
            infoTarget = 1;
        }
        infoGroup.alpha = Mathf.Lerp(infoGroup.alpha, infoTarget, dt * 12);
        infoGroup.blocksRaycasts = selected >= 0;
        infoPanelRect.anchoredPosition = new Vector2(Mathf.Lerp(-16, -40, infoGroup.alpha), infoPanelRect.anchoredPosition.y);

        pointsScale = Mathf.Lerp(pointsScale, 1, dt * 10);
        skillPointsText.transform.localScale = Vector3.one * pointsScale;
    }

    GameObject AddHex(int index)
    {
        Skill skill = skills[index];
        GameObject hex = Instantiate(skillTemplate, skillContent);
        hex.name = skill.name;
        hex.SetActive(true);

        float x = hexSize * Mathf.Sqrt(3) * (skill.q + skill.r / 2f);
        float y = -hexSize * 1.5f * skill.r;
        hex.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);

        hex.transform.Find("Icon").GetComponent<Image>().sprite = skill.icon;
        hex.transform.Find("Label").GetComponent<TMP_Text>().text = skill.name;

        hex.transform.Find("Border").GetComponent<Image>().alphaHitTestMinimumThreshold = 0.5f;
        int distance = (Mathf.Abs(skill.q) + Mathf.Abs(skill.r) + Mathf.Abs(skill.q + skill.r)) / 2;
        hex.GetComponent<SkillHex>().appearDelay = 0.2f + distance * 0.12f;

        hex.GetComponent<Button>().onClick.AddListener(() => Select(index));
        return hex;
    }

    void Refresh()
    {
        skillPointsText.text = skillPoints.ToString();

        for (int i = 0; i < skills.Length; i++)
        {
            UpdateHex(i);
        }

        if (selected >= 0)
        {
            ShowInfo(selected);
        }
    }

    void UpdateHex(int index)
    {
        Skill skill = skills[index];
        Transform hex = hexes[index].transform;
        Color branchColor = GetBranchColor(skill.branch);

        Color border;
        Color fill;
        Color icon;
        Color label;
        bool locked = false;

        if (skill.learned)
        {
            border = branchColor;
            fill = Color.Lerp(availableFill, branchColor, 0.38f);
            icon = learnedIcon;
            label = normalText;
        }
        else if (IsAvailable(skill))
        {
            border = availableBorder;
            fill = availableFill;
            icon = availableIcon;
            label = normalText;
        }
        else
        {
            border = lockedBorder;
            fill = lockedFill;
            icon = lockedIcon;
            label = lockedText;
            locked = true;
        }

        if (skill.branch == SkillBranch.Core)
        {
            icon = branchColor;
            label = branchColor;
        }

        SkillHex skillHex = hex.GetComponent<SkillHex>();
        skillHex.borderColor = border;
        skillHex.fillColor = fill;
        skillHex.iconColor = icon;
        skillHex.labelColor = label;
        skillHex.glowColor = branchColor;
        skillHex.learned = skill.learned;
        skillHex.selected = index == selected;
        skillHex.locked = locked;
    }

    bool IsAvailable(Skill skill)
    {
        if (skill.requires == "")
        {
            return true;
        }

        foreach (Skill other in skills)
        {
            if (other.name == skill.requires && other.learned)
            {
                return true;
            }
        }
        return false;
    }

    bool CanLearn(Skill skill)
    {
        if (skill.learned) return false;
        if (skillPoints < skill.cost) return false;
        return IsAvailable(skill);
    }

    Color GetBranchColor(SkillBranch branch)
    {
        if (branch == SkillBranch.Economy) return economyColor;
        if (branch == SkillBranch.Workers) return workersColor;
        if (branch == SkillBranch.Defense) return defenseColor;
        if (branch == SkillBranch.Walls) return wallsColor;
        return coreColor;
    }

    void Select(int index)
    {
        selected = index;
        hexes[index].GetComponent<SkillHex>().Press();
        Refresh();
    }

    void ShowInfo(int index)
    {
        Skill skill = skills[index];
        infoNameText.text = skill.name;
        infoDescriptionText.text = skill.description;
        infoCostText.text = skill.cost + " Skill Point";

        if (skill.learned)
        {
            infoStatusText.text = "Learned";
        }
        else
        {
            infoStatusText.text = "Not learned";
        }

        learnButton.interactable = CanLearn(skill);
    }

    void LearnSelected()
    {
        if (selected < 0) return;

        Skill skill = skills[selected];
        if (CanLearn(skill))
        {
            skillPoints -= skill.cost;
            skill.learned = true;
            hexes[selected].GetComponent<SkillHex>().LearnEffect();
            pointsScale = 1.4f;
            Refresh();
        }
    }

    void ResetSkills()
    {
        foreach (Skill skill in skills)
        {
            if (skill.learned && skill.branch != SkillBranch.Core)
            {
                skill.learned = false;
                skillPoints += skill.cost;
            }
        }
        pointsScale = 1.4f;
        Refresh();
    }

    void Close()
    {
        SceneTransition.Load(backScene);
    }
}
