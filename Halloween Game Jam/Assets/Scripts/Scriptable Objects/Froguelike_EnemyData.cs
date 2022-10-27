using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum Froguelike_EnemyMovePattern
{
    NO_MOVEMENT,
    STRAIGHTLINE,
    TARGETPLAYER
}

[System.Serializable]
[CreateAssetMenu(fileName = "Enemy Data", menuName = "ScriptableObjects/Froguelike/Enemy Data", order = 1)]
public class Froguelike_EnemyData : ScriptableObject
{
    public string ID;

    public Froguelike_EnemyMovePattern movePattern;

    public int maxHP;
    public int xPBonus;
    public float moveSpeed;
    public float damage;

    public GameObject prefab;
}
