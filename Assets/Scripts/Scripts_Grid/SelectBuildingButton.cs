using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BuildButton : MonoBehaviour
{
    [SerializeField] private BuildMenu menu;
    [SerializeField] private BuildingType type;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => menu.SelectBuilding(type));
    }
}