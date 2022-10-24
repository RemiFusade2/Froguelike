using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Froguelike_GameManager : MonoBehaviour
{
    public static Froguelike_GameManager instance;

    public Froguelike_CharacterController player;

    public Transform fliesParent;

    public int xp;
    public int nextLevelXp = 5;
    public int level;

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        level = 1;
        xp = 0;
        Froguelike_UIManager.instance.UpdateXPSlider(0, nextLevelXp);
    }

    public void EatFly(int experiencePoints)
    {
        xp += experiencePoints;

        while (xp >= nextLevelXp)
        {
            LevelUP();
            xp -= nextLevelXp;
            nextLevelXp *= 2;
        }

        Froguelike_UIManager.instance.UpdateXPSlider(xp, nextLevelXp);
    }

    public void LevelUP()
    {
        level++;
        player.LevelUP();
        Froguelike_UIManager.instance.UpdateLevel(level);
    }
}
