using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HatBehaviour : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    private HatInfo hatInfo;

    public void Initialize(HatInfo _hatInfo)
    {
        hatInfo = _hatInfo;
        spriteRenderer.sprite = _hatInfo.hatSprite;
    }

    public float GetHatHeight()
    {
        return hatInfo.hatHeight;
    }

    public HatType GetHatType()
    {
        return hatInfo.hatType;
    }
}
