using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Froguelike_UIManager : MonoBehaviour
{
    public static Froguelike_UIManager instance;

    public Slider xpSlider;

    public Text levelText;

    private void Awake()
    {
        instance = this;
    }

    public void UpdateXPSlider(int xp, int maxXp)
    {
        xpSlider.maxValue = maxXp;
        xpSlider.value = xp;
    }

    public void UpdateLevel(int level)
    {
        levelText.text = "LVL " + level.ToString();
    }
}
