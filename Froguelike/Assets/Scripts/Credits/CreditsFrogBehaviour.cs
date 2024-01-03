using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreditsFrogBehaviour : MonoBehaviour
{
    public Animator frogAnimator;
    public Transform frogTransform;

    private float angle;

    public void ClickOnFrog()
    {
        frogAnimator.SetTrigger("Jump");
    }

    public void PlaySound()
    {
        SoundManager.instance.PlayCreditFrogCroakingSound();
    }

    public void ResetFrog()
    {
        angle = 0;
        frogTransform.localRotation = Quaternion.Euler(0, 0, angle);
        if (frogAnimator.gameObject.activeInHierarchy)
        {
            frogAnimator.SetTrigger("Reset");
            frogAnimator.ResetTrigger("Jump");
        }
    }

    public void FrogTurnsAround()
    {
        angle = (angle == 0) ? 180 : 0;
        frogTransform.localRotation = Quaternion.Euler(0, 0, angle);
    }
}
