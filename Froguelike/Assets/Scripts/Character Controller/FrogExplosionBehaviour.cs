using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class FrogExplosionBehaviour : MonoBehaviour
{
    [Header("References")]
    public CircleCollider2D explosionCollider;
    public ParticleSystem explosionParticleSystem;

    [Header("Setting")]
    public CollectibleType collectibleType;
    public float blowUpSpeed = 30;
    public float maxRadius = 30;

    private Coroutine explosionBlowUpCoroutine;

    private void Start()
    {
        explosionCollider.radius = 0;
        explosionCollider.enabled = false;
    }

    public void TriggerExplosion(Action endOfExplosionAction)
    {
        explosionCollider.enabled = true;
        explosionParticleSystem.Play();
        if (explosionBlowUpCoroutine != null)
        {
            StopCoroutine(explosionBlowUpCoroutine);
        }
        explosionBlowUpCoroutine = StartCoroutine(ExplosionBlowUpAsync(endOfExplosionAction));        
    }

    private IEnumerator ExplosionBlowUpAsync(Action endOfExplosionAction)
    {
        float radius = 0;
        ShapeModule shape = explosionParticleSystem.shape;
        while (radius < maxRadius)
        {
            explosionCollider.radius = radius;
            shape.radius = radius;

            radius += blowUpSpeed * Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        explosionCollider.radius = 0;
        shape.radius = 0;
        explosionParticleSystem.Stop();
        explosionCollider.enabled = false;
        if (endOfExplosionAction != null)
        {
            endOfExplosionAction();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy") && GameManager.instance.isGameRunning)
        {
            float explosionDuration = (maxRadius / blowUpSpeed); // After explosion effect, the global effect will take over.
            switch (collectibleType)
            {
                case CollectibleType.POWERUP_FREEZEALL:
                    EnemiesManager.instance.ApplyFreezeEffect(collision.name, explosionDuration);
                    break;
                case CollectibleType.POWERUP_POISONALL:
                    EnemiesManager.instance.AddPoisonDamageToEnemy(collision.name, 0.1f, explosionDuration);
                    break;
                case CollectibleType.POWERUP_CURSEALL:
                    EnemiesManager.instance.ApplyCurseEffect(collision.name, explosionDuration);
                    break;
                case CollectibleType.POWERUP_LEVELDOWNBUGS:
                    EnemiesManager.instance.SwitchTierOfEnemy(collision.name, -1, explosionDuration);
                    break;
                case CollectibleType.POWERUP_LEVELUPBUGS:
                    EnemiesManager.instance.SwitchTierOfEnemy(collision.name, 1, explosionDuration);
                    break;
                default:
                    break;
            }
        }
    }
}
