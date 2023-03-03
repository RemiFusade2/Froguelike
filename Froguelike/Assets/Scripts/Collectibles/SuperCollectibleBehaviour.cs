using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperCollectibleBehaviour : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer collectibleRenderer;

    [HideInInspector]
    public FixedCollectible collectibleInfo;

    public void InitializeCollectible(FixedCollectible collectible)
    {
        collectibleInfo = collectible;
        collectibleRenderer.sprite = DataManager.instance.GetSpriteForCollectible(collectible);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            CollectiblesManager.instance.CollectSuperCollectible(collectibleInfo);
            Destroy(this.gameObject);
        }
    }
}
