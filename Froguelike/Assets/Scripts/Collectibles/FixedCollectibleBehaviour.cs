using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixedCollectibleBehaviour : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer collectibleRenderer;
    public SpriteRenderer arrowRendererUnknownItem;
    public SpriteRenderer arrowRendererKnownItem;

    [HideInInspector]
    public FixedCollectible collectibleInfo;

    public void InitializeCollectible(FixedCollectible collectible)
    {
        collectibleInfo = collectible;
        collectibleRenderer.sprite = DataManager.instance.GetSpriteForCollectible(collectible);
        collectibleRenderer.flipY = collectible.collectibleType == FixedCollectibleType.HAT;
        arrowRendererUnknownItem.enabled = true;
        arrowRendererKnownItem.enabled = false;
    }

    public void SetArrowVisibility(bool visible, bool knownItem)
    {
        arrowRendererUnknownItem.enabled = false;
        arrowRendererKnownItem.enabled = false;
        if (knownItem)
        {
            arrowRendererKnownItem.enabled = visible;
        }
        else
        {
            arrowRendererUnknownItem.enabled = visible;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            CollectiblesManager.instance.CollectFixedCollectible(collectibleInfo);
            Destroy(this.gameObject);
        }
    }
}
