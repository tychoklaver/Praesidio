using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// Fills in the "Cost" text of the build cards from the real building prefabs, so the UI can never
/// disagree with what a building actually costs. It runs in Awake, before InGameUI builds its cards
/// in Start. Nothing in other people's scripts is changed; their private lists are read by reflection.
/// </summary>
public class BuildCostLabels : MonoBehaviour
{
    [SerializeField] InGameUI hud;
    [SerializeField] BuildMenu menu;

    static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    void Awake()
    {
        if (hud == null || menu == null)
        {
            Debug.LogError("[BuildCostLabels] Assign the InGameUI and the BuildMenu in the inspector.", this);
            return;
        }

        InGameUI.Building[] cards = ReadField<InGameUI.Building[]>(hud, "buildings");
        Building[] prefabs = ReadField<Building[]>(menu, "buildings");
        if (cards == null || prefabs == null)
        {
            Debug.LogWarning("[BuildCostLabels] The 'buildings' lists were not found. Costs not filled in.", this);
            return;
        }

        // A card and a prefab belong together when they share the same BuildingType,
        // which is also how BuildMenu picks the prefab to place.
        foreach (InGameUI.Building card in cards)
        {
            foreach (Building prefab in prefabs)
            {
                if (prefab == null || prefab.buildingType != card.type) continue;
                card.cost = Format(prefab.cost);
                break;
            }
        }
    }

    // "10 Wood, 20 Stone", or "Free" when the building costs nothing.
    static string Format(ResourceCost[] pCost)
    {
        if (pCost == null || pCost.Length == 0) return "Free";

        StringBuilder text = new();
        foreach (ResourceCost c in pCost)
        {
            if (text.Length > 0) text.Append(", ");
            text.Append(c.amount).Append(' ').Append(c.type);
        }
        return text.ToString();
    }

    static T ReadField<T>(object pOwner, string pName) where T : class =>
        pOwner.GetType().GetField(pName, PrivateInstance)?.GetValue(pOwner) as T;
}