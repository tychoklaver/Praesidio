using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SkillTreeData
{
    public bool hasSaved;

    public int skillPoints;

    public List<string> learned = new();
    
}
