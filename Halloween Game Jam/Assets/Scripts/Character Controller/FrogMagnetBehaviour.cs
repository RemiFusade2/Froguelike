using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrogMagnetBehaviour : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Collectible"))
        {
            CollectiblesManager.instance.CaptureCollectible(collision.transform);
        }
    }
}
