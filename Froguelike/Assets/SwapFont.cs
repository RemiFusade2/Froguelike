using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SwapFont : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font1;
    [SerializeField] private TMP_FontAsset font2;
    [SerializeField] private TMP_FontAsset font3;

    Object[] TMPUGUIList = new Object[] { };
    TMP_FontAsset currentFont;

    // Start is called before the first frame update
    void Start()
    {
        TMPUGUIList = Resources.FindObjectsOfTypeAll(typeof(TextMeshProUGUI));
        currentFont = font1;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Swap();
        }
    }

    public void Swap()
    {
        if (currentFont == font1)
        {
            currentFont = font2;
        }
        else if (currentFont == font2)
        {
            currentFont = font3;
        }
        else if (currentFont == font3)
        {
            currentFont = font1;
        }

        foreach (TextMeshProUGUI text in TMPUGUIList)
        {
            text.font = currentFont;
            // text.enableAutoSizing = currentFont == font1 ? false : true;
            // text.fontSizeMin = 0;
        }
    }
}
