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
    public TongueEffect statusEffect;
    public float blowUpSpeed = 30;
    public float maxRadius = 30;

    private Coroutine explosionBlowUpCoroutine;

    public void TriggerExplosion(Action endOfExplosionAction)
    {
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
        endOfExplosionAction();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy") && GameManager.instance.isGameRunning)
        {
            float explosionDuration = (maxRadius / blowUpSpeed); // After explosion effect, the global effect will take over.
            switch (statusEffect)
            {
                case TongueEffect.FREEZE:
                    EnemiesManager.instance.ApplyFreezeEffect(collision.name, explosionDuration);
                    break;
                case TongueEffect.POISON:
                    EnemiesManager.instance.AddPoisonDamageToEnemy(collision.name, 0.1f, explosionDuration);
                    break;
                case TongueEffect.CURSE:
                    EnemiesManager.instance.ApplyCurseEffect(collision.name, explosionDuration);
                    break;
                default:
                    break;
            }
        }
    }
}
