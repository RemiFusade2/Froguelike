using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class FrogExplosionBehaviour : MonoBehaviour
{
    [Header("References")]
    public ParticleSystem explosionParticleSystem;

    [Header("Setting")]
    public CollectibleType collectibleType;
    public float blowUpSpeed = 30;
    public float maxRadius = 30;

    [Range(1, 100)]
    public int splitCount = 20;

    private Coroutine explosionBlowUpCoroutine;

    public void TriggerExplosion(Action endOfExplosionAction)
    {
        if (explosionBlowUpCoroutine != null)
        {
            StopCoroutine(explosionBlowUpCoroutine);
        }
        explosionBlowUpCoroutine = StartCoroutine(ExplosionBlowUpAsync(endOfExplosionAction));        
    }

    public void StopAndResetExplosion()
    {
        ShapeModule shape = explosionParticleSystem.shape;
        if (explosionBlowUpCoroutine != null)
        {
            StopCoroutine(explosionBlowUpCoroutine);
        }
        shape.radius = 0;
        explosionParticleSystem.Stop();
        explosionParticleSystem.Clear();
    }

    private IEnumerator ExplosionBlowUpAsync(Action endOfExplosionAction)
    {
        float radius = 0;
        ShapeModule shape = explosionParticleSystem.shape;
        List<List<EnemyInstance>> enemiesList = EnemiesManager.instance.GetActiveEnemiesSplitByDistanceToFrog(maxRadius, splitCount);
        explosionParticleSystem.Play();
        int enemyListIndex = 0;
        int lastEnemyListIndex = -1;
        float explosionDuration = maxRadius / blowUpSpeed;
        while (radius < maxRadius)
        {
            shape.radius = radius;

            radius += blowUpSpeed * Time.fixedDeltaTime;

            if (enemyListIndex > lastEnemyListIndex)
            {
                // Apply effect on all enemies from that list
                foreach (EnemyInstance enemy in enemiesList[enemyListIndex])
                {
                    if (enemy.active && enemy.alive)
                    {
                        ApplyEffectToEnemy(enemy, explosionDuration);
                    }
                }
                lastEnemyListIndex = enemyListIndex;
            }
            enemyListIndex = Mathf.FloorToInt(radius * splitCount / maxRadius);

            yield return new WaitForFixedUpdate();
        }

        shape.radius = 0;
        explosionParticleSystem.Stop();
        if (endOfExplosionAction != null)
        {
            endOfExplosionAction();
        }
    }

    private void ApplyEffectToEnemy(EnemyInstance enemy, float duration)
    {
        switch (collectibleType)
        {
            case CollectibleType.POWERUP_FREEZEALL:
                EnemiesManager.instance.ApplyFreezeEffect(enemy, duration);
                break;
            case CollectibleType.POWERUP_POISONALL:
                EnemiesManager.instance.AddPoisonDamageToEnemy(enemy, 0.1f, duration);
                break;
            case CollectibleType.POWERUP_CURSEALL:
                EnemiesManager.instance.ApplyCurseEffect(enemy, duration);
                break;
            case CollectibleType.POWERUP_LEVELDOWNBUGS:
                EnemiesManager.instance.SwitchTierOfEnemy(enemy, -1, duration);
                break;
            case CollectibleType.POWERUP_LEVELUPBUGS:
                EnemiesManager.instance.SwitchTierOfEnemy(enemy, 1, duration);
                break;
            default:
                break;
        }
    }
}
