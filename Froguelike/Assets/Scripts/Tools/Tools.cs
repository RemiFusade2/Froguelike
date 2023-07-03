using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Tools
{
    public static string FormatCurrency(long num, string currencySymbol)
    {
        string result = "";

        // Ensure number has max 3 significant digits (no rounding up can happen)
        long i = (long)Math.Pow(10, (int)Math.Max(0, Math.Log10(num) - 2));
        num = num / i * i;

        // Symbols for powers of 10:
        // K M B T QD QN SX SP O N DE UD DD TDD QDD QND SXD SPD OCD NVD VGN UVG DVG TVG QTV QNV SEV SPG OVG NVG

        if (num >= 1000000000000000000)
            result = (num / 1000000000000000000D).ToString("0.##") + "QN";
        else if (num >= 1000000000000000)
            result = (num / 1000000000000000D).ToString("0.##") + "QD";
        else if (num >= 1000000000000)
            result = (num / 1000000000000D).ToString("0.##") + "T";
        else if (num >= 1000000000)
            result = (num / 1000000000D).ToString("0.##") + "B";
        else if (num >= 1000000)
            result = (num / 1000000D).ToString("0.##") + "M";
        else if (num >= 1000)
            result = (num / 1000D).ToString("0.##") + "K";
        else
            result = num.ToString("#,0");

        return result + currencySymbol;
    }

    public static Vector2 Rotate(Vector2 v, float angle)
    {
        return new Vector2(
           v.x * Mathf.Cos(angle) - v.y * Mathf.Sin(angle),
           v.x * Mathf.Sin(angle) + v.y * Mathf.Cos(angle)
       );
    }
}
