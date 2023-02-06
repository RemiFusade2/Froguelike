using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum EnemyMovePattern
{
    NO_MOVEMENT,
    STRAIGHTLINE,
    TARGETPLAYER
}

[System.Serializable]
[CreateAssetMenu(fileName = "Enemy Data", menuName = "ScriptableObjects/Froguelike/Enemy Data", order = 1)]
public class EnemyData : ScriptableObject
{
    public string ID;

    public EnemyMovePattern movePattern;

    public float maxHP;
    public int xPBonus;
    public float moveSpeed;
    public float damage;

    public bool instantlyEndChapter;

    public GameObject prefab;
}
