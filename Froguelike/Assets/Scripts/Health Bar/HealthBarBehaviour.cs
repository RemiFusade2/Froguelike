using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBarBehaviour : MonoBehaviour
{
    [Header("Settings - Logs")]
    public VerboseLevel verbose;

    [Header("References")]
    public SpriteRenderer frame;
    public SpriteRenderer background;
    public SpriteRenderer filling;
    public SpriteRenderer tempFillingForDamage;
    public SpriteRenderer tempFillingForHealing;

    [Header("Settings - Size")]
    public float distancePerPixel = 0.0625f;
    public float heightInPixels = 2;
    public float HPPerPixelWidth = 5;

    [Header("Settings - Color")]
    public Color standardHealthColor = Color.white;
    public Color damageHealthColor = Color.white;
    public Color healingHealthColor = Color.white;

    [Header("Settings - Delays")]
    public float speedApplyDamage = 100; // once damage starts being validated as "current", it moves at this speed (HP/s)
    [Space]
    public float speedApplyDamageIfDead = 300; // if health is < -10, it moves at this speed (HP/s)
    [Space]
    public float speedApplyHealing = 60; // once healing starts being validated as "current", it moves at this speed (HP/s)
    [Space]
    public float healthRecoveryDelay = 0; // Time without damage before health starts being recovered
    [Space]
    public float criticalBlinkingDelay = 1;
    public float superCriticalBlinkingDelay = 0.5f;

    [Header("Runtime")]
    public float maxHealth;
    public float healthRecovery; // Amount of health recovered per second, only if no damage was taken after a delay.
    [Space]
    public float currentHealthShown; // HP bar shows this amount
    public float currentHealthTarget; // This is current HP. HP bar shows a line to this amount in a different color.

    private float lastDamageTakenTime;

    private bool blink;
    private Coroutine blinkCoroutine;

    private bool preventHealthRecovery;

    // Start is called before the first frame update
    void Start()
    {
        preventHealthRecovery = false;
        InitializeRenderers();
        SetMaxHealth(50, setHealthToMax: true);
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.instance.gameIsPaused && 
            !ChapterManager.instance.chapterChoiceIsVisible && 
            !RunManager.instance.levelUpChoiceIsVisible && 
            Time.time - lastDamageTakenTime > healthRecoveryDelay &&
            !preventHealthRecovery)
        {
            // Recovery is active
            float actualHealthRecovery = healthRecovery;
            if (IsCritical())
            {
                // When frog is in critical health, we increase its health recovery
                actualHealthRecovery += 1;
            }
            if (RunManager.instance.currentPlayedCharacter.characterID.Equals("SWIMMING_FROG") && !RunManager.instance.player.IsOnLand())
            {
                // If we play as Kermit and are in a pond right now, health recovery is increased
                actualHealthRecovery += 2;
            }
            IncreaseHealth(actualHealthRecovery * Time.deltaTime, cancelDamage: false);
        }

        if (currentHealthShown < currentHealthTarget)
        {
            // Increase health shown, limit to health target
            currentHealthShown = Mathf.Clamp(currentHealthShown + speedApplyHealing * Time.deltaTime, 0, currentHealthTarget);
        }
        else if (currentHealthShown > currentHealthTarget)
        {
            // Decrease health shown, limit to health target
            if (currentHealthTarget <= -10)
            {
                currentHealthShown = Mathf.Clamp(currentHealthShown - speedApplyDamageIfDead * Time.deltaTime, currentHealthTarget, maxHealth);
            }
            else
            {
                currentHealthShown = Mathf.Clamp(currentHealthShown - speedApplyDamage * Time.deltaTime, currentHealthTarget, maxHealth);
            }
        }

        if (IsCritical() && blinkCoroutine == null)
        {
            // Start blinking if there's no blink
            blinkCoroutine = StartCoroutine(ToggleBlinkAsync());
            blink = true;
        }
        else if (!IsCritical())
        {
            // Stop blinking if there's a blink
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }
            blink = true;
        }

        UpdateHealthBarView();
    }


    /// <summary>
    /// Returns true if frog is dead :-(
    /// </summary>
    /// <returns></returns>
    public bool IsDead()
    {
        return currentHealthShown <= -10;
    }

    /// <summary>
    /// Returns true if frog has less than 10% max HP left
    /// </summary>
    /// <returns></returns>
    public bool IsCritical()
    {
        return currentHealthShown <= (maxHealth * 0.1f);
    }

    /// <summary>
    /// Returns true if frog has negative HP (not dead but almost)
    /// </summary>
    /// <returns></returns>
    public bool IsSuperCritical()
    {
        return currentHealthShown <= 0;
    }

    /// <summary>
    /// Set the new max health. Resize the Health Bar accordingly.
    /// </summary>
    /// <param name="newMaxHealth"></param>
    public void SetMaxHealth(float newMaxHealth, bool setHealthToMax = false)
    {
        maxHealth = newMaxHealth;
        if (setHealthToMax)
        {
            ResetHealth();            
        }
        UpdateHealthBarView(resizeHealthBar: true);
    }
    public void IncreaseMaxHealth(float maxHealthIncrease)
    {
        SetMaxHealth(maxHealth + maxHealthIncrease);
    }

    public void SetHealthRecovery(float newHealthRecovery)
    {
        healthRecovery = newHealthRecovery;
    }

    public void ResetHealth()
    {
        currentHealthShown = maxHealth;
        currentHealthTarget = maxHealth;
        preventHealthRecovery = false;
        UpdateHealthBarView(resizeHealthBar: true);
    }

    /// <summary>
    /// Increase health.
    /// </summary>
    /// <param name="amount"></param>
    /// <param name="cancelDamage"></param>
    public void IncreaseHealth(float amount, bool cancelDamage)
    {
        if (currentHealthTarget < 0 && amount > 0 && cancelDamage)
        {
            // Just in case Frog was about to die but it found a health pick-up
            preventHealthRecovery = false;
            currentHealthTarget = Mathf.Clamp(amount, 0, maxHealth);
        }
        else
        {
            // Compute new health target from target health
            currentHealthTarget = Mathf.Clamp(currentHealthTarget + amount, float.MinValue, maxHealth);
        }

        if (verbose == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"IncreaseHealth +{amount}, cancel damage: {cancelDamage}. currentHealthTarget = {currentHealthTarget}");
        }
    }

    /// <summary>
    /// Decrease health.
    /// </summary>
    /// <param name="amount"></param>
    public void DecreaseHealth(float amount)
    {
        if (amount > 0) 
        {
            lastDamageTakenTime = Time.time;
        }

        // Compute new health target
        currentHealthTarget = Mathf.Clamp(currentHealthTarget - amount, float.MinValue, maxHealth);

        if (currentHealthTarget <= -10)
        {
            // Game over incoming
            preventHealthRecovery = true;
        }

        if (verbose == VerboseLevel.MAXIMAL)
        {
            Debug.Log($"DecreaseHealth -{amount}. currentHealthTarget = {currentHealthTarget}");
        }
    }
    private IEnumerator ToggleBlinkAsync()
    {
        while (true)
        {
            yield return new WaitForSeconds(IsSuperCritical() ? superCriticalBlinkingDelay : criticalBlinkingDelay);
            blink = !blink;
            if (verbose == VerboseLevel.MAXIMAL)
            {
                Debug.Log($"blink = {blink}");
            }
        }
    }

    private void InitializeRenderers()
    {
        filling.color = standardHealthColor;
        tempFillingForDamage.color = damageHealthColor;
        tempFillingForHealing.color = healingHealthColor;
    }

    private void UpdateHealthBarView(bool resizeHealthBar = false)
    {
        float healthShown = Mathf.Clamp(currentHealthShown, 1, maxHealth); // Prevent the last pixel of life from disappearing before the game is over
        float healthTarget = Mathf.Clamp(currentHealthTarget, 1, maxHealth); // Prevent the last pixel of life from disappearing before the game is over

        int healthBarWidthInPixels = Mathf.CeilToInt((maxHealth-0.1f) / HPPerPixelWidth);
        int healthBarFillWidthInPixels = Mathf.CeilToInt((healthShown-0.1f) / HPPerPixelWidth);

        int startPositionOfHealthBarInPixels = -Mathf.CeilToInt(healthBarWidthInPixels / 2);
        int positionOfShownHealthInPixels = startPositionOfHealthBarInPixels + healthBarFillWidthInPixels;

        int positionOfTargetHealthInPixels = startPositionOfHealthBarInPixels + Mathf.CeilToInt((healthTarget - 0.1f) / HPPerPixelWidth);

        Vector2 newSize;
        if (resizeHealthBar)
        {
            // Set Background and Frame size
            newSize = distancePerPixel * new Vector2(healthBarWidthInPixels + 2, heightInPixels + 2); // +2 because frame is 2 pixels around health bar
            background.size = newSize;
            frame.size = newSize;
            // Set Background and Frame position
            Vector3 framePosition = new Vector3((startPositionOfHealthBarInPixels - 1) * distancePerPixel, background.transform.localPosition.y, background.transform.localPosition.z);
            background.transform.localPosition = framePosition;
            frame.transform.localPosition = framePosition;
        }

        bool healthBarIsVisible = IsCritical() ? blink : true;

        // Set fill size (use currentHealthShown)
        newSize = distancePerPixel * new Vector2(healthBarFillWidthInPixels, heightInPixels);
        filling.size = newSize;
        filling.transform.localPosition = new Vector3(distancePerPixel * startPositionOfHealthBarInPixels, filling.transform.localPosition.y, filling.transform.localPosition.z);
        filling.enabled = healthBarIsVisible;

        // Set tempFilling if needed
        int healthBarTempFillWidthInPixels;
        if (healthTarget < healthShown)
        {
            // Damage must be shown
            healthBarTempFillWidthInPixels = positionOfShownHealthInPixels - positionOfTargetHealthInPixels;
            newSize = distancePerPixel * new Vector2(-healthBarTempFillWidthInPixels, heightInPixels);
            tempFillingForDamage.size = newSize;
            tempFillingForDamage.transform.localPosition = new Vector3(positionOfShownHealthInPixels * distancePerPixel, tempFillingForDamage.transform.localPosition.y, tempFillingForDamage.transform.localPosition.z);

            tempFillingForHealing.enabled = false;
            tempFillingForDamage.enabled = healthBarIsVisible;
        }
        else if (healthTarget > healthShown)
        {
            // Healing must be shown
            healthBarTempFillWidthInPixels = positionOfTargetHealthInPixels - positionOfShownHealthInPixels;
            newSize = distancePerPixel * new Vector2(healthBarTempFillWidthInPixels, heightInPixels);
            tempFillingForHealing.size = newSize;
            tempFillingForHealing.transform.localPosition = new Vector3(positionOfShownHealthInPixels * distancePerPixel, tempFillingForHealing.transform.localPosition.y, tempFillingForHealing.transform.localPosition.z);

            tempFillingForDamage.enabled = false;
            tempFillingForHealing.enabled = healthBarIsVisible;
        }
        else
        {
            tempFillingForDamage.enabled = false;
            tempFillingForHealing.enabled = false;
        }

    }
}
