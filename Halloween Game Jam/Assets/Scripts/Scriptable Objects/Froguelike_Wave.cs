using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Wave Data", menuName = "ScriptableObjects/Froguelike/Wave Data", order = 1)]
public class Froguelike_Wave : ScriptableObject
{
    public List<float> spawnDelays;
    public List<Froguelike_SpawnPattern> spawnPatterns;
}
