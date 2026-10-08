using UnityEngine;
public class BuildMenu : MonoBehaviour
{
   [SerializeField] private BuildingGhost ghost;
   [SerializeField] private Building[] buildings;

   public void SelectBuilding(BuildingType type)
   {
      foreach (var b in buildings)
      {
         if (b.buildingType == type)
         {
            ghost.Begin(b);
            return;
         }
      }
      Debug.LogWarning($"Geen Home met {type}  govenden");
   }
}
