using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FontPanelBehaviour : MonoBehaviour
{
    public TextMeshProUGUI fontTextMesh;

    public void Initialize(string fontName, TMP_FontAsset font)
    {
        fontTextMesh.text = fontName;
        fontTextMesh.font = font;
    }
}
