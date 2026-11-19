using System;
using System.Collections.Generic;

/// <summary>Saved state of the skill tree, stored inside of <see cref="PlayerData"/>.</summary>
[Serializable]
public class SkillTreeData
{
    /// <summary>False until the tree was saved once; until then every skill keeps its editor default.</summary>
    public bool hasSaved;

    /// <summary>Unspent skill points (also raised by <see cref="SkillPoints.Award"/> from other scenes).</summary>
    public int skillPoints;

    /// <summary>Names of the learned skills.</summary>
    public List<string> learned = new();
    
}
