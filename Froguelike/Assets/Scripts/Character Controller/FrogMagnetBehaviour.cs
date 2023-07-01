using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrogMagnetBehaviour : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Collectible"))
        {
            FrogCharacterController playerScript = this.transform.GetComponentInParent<FrogCharacterController>();
            if (playerScript != null)
            {
                CollectiblesManager.instance.CaptureCollectible(collision.transform, playerScript);
            }
        }
    }
}
