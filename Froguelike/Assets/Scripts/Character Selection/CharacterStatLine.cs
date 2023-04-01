using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterStatLine : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameTextMesh;
    public TextMeshProUGUI totalValueTextMesh;
    public TextMeshProUGUI frogValueTextMesh;
    public TextMeshProUGUI shopValueTextMesh;

    private string GetStringForValue(float value, bool usePercent)
    {
        string result = "-";
        if (value != 0)
        {
            string sign = (value >= 0) ? "+" : "";
            if (usePercent)
            {
                result = $"{sign}{value.ToString("P0").Replace(" ٪", "%")}";
            }
            else
            {
                result = $"{sign}{value.ToString("0.#")}";
            }
        }
        return result;
    }

    public void Initialize(CharacterStat stat, float totalValue, float frogValue, float shopValue)
    {
        if (DataManager.instance.TryGetStatData(stat, out string shortName, out string longName, out string unit, out bool usePercent, out Sprite icon))
        {
            // Icon and Name
            iconImage.sprite = icon;
            iconImage.color = (icon == null) ? new Color(0, 0, 0, 0) : Color.white;
            nameTextMesh.SetText(longName);

            // Values
            string totalValueSign = (totalValue >= 0) ? "+" : "";

            string frogValueStr = GetStringForValue(frogValue, usePercent);
            string shopValueStr = GetStringForValue(shopValue, usePercent);

            if (usePercent)
            {
                totalValueTextMesh.SetText($"{totalValueSign}{totalValue.ToString("P0").Replace(" ٪", "%")}{unit}");
            }
            else
            {
                totalValueTextMesh.SetText($"{totalValue.ToString()}{unit}");
            }
            frogValueTextMesh.SetText(frogValueStr);
            shopValueTextMesh.SetText(shopValueStr);
        }
    }
}
