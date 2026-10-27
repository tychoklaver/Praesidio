using UnityEngine;

// Alles wat torens kunnen aanvallen. Laat je bestaande target-klassen dit interface implementeren
// en registreren bij TargetRegistry (OnEnable/OnDisable), dan werken de torens er direct mee.
public interface ITargetable
{
    Transform TargetTransform { get; }
    int Priority { get; }   // hoger = belangrijker; torens vallen dit eerst aan
    bool IsAlive { get; }
    void TakeDamage(float amount);
}