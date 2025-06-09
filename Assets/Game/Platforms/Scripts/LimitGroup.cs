using System.Collections.Generic;
using Game.Platforms.Scripts;
using UnityEngine;

[System.Serializable]
public class LimitGroup
{
    [Tooltip("Limit applies to the sum of all these types")]
    public List<PlatformType> types = new List<PlatformType>();

    [Tooltip("Maximum number of active instances for this group")]
    public int maxActive = 1;
}