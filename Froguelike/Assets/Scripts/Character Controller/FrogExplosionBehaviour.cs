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

    public void TriggerExplosion()
    {
        explosionParticleSystem.Play();
        if (explosionBlowUpCoroutine != null)
        {
            StopCoroutine(explosionBlowUpCoroutine);
        }
        explosionBlowUpCoroutine = StartCoroutine(ExplosionBlowUpAsync());        
    }

    private IEnumerator ExplosionBlowUpAsync()
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
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy") && GameManager.instance.isGameRunning)
        {
            switch(statusEffect)
            {
                case TongueEffect.FREEZE:
                    EnemiesManager.instance.ApplyFreezeEffect(collision.name, 10);
                    break;
                case TongueEffect.POISON:
                    EnemiesManager.instance.AddPoisonDamageToEnemy(collision.name, 0.1f, 10);
                    break;
                case TongueEffect.CURSE:
                    EnemiesManager.instance.ApplyCurseEffect(collision.name, 10);
                    break;
                default:
                    break;
            }
        }
    }
}
