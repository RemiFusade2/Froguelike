using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollbarKeepCursorSizeBehaviour : MonoBehaviour
{
    public float fixedSize = 0.15f;

    private Scrollbar scrollBar;
    private bool cursorCentered;

    private void Start()
    {
        scrollBar = GetComponent<Scrollbar>();
        SetCursorCentered(cursorCentered);
    }

    private void LateUpdate()
    {
        SetCursorToFixedSize();
    }

    public void SetCursorCentered(bool value)
    {
        cursorCentered = value;
        SetCursorToFixedSize();
    }

    public void SetCursorToFixedSize()
    {
        if (scrollBar != null)
        {
            scrollBar.size = fixedSize;
            if (cursorCentered)
            {
                scrollBar.value = 0.5f;
            }
        }
    }

    public void SetValue(float v)
    {
        if (scrollBar != null)
        {
            scrollBar.value = v;
        }
    }
}
