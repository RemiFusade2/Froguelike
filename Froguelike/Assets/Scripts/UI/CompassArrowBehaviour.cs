using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CompassArrowBehaviour : MonoBehaviour
{
    [Header("Reference")]
    public Animator arrowAnimator;
    public RectTransform arrowRectTransform;
    public Image arrowImage;

    [Header("Runtime")]
    public Vector2Int collectibleTileCoordinates;
    public Transform collectibleTransform;
    [Space]
    public Vector3 currentArrowPosition;
    public Vector3 targetArrowPosition;

    private void Awake()
    {
        currentArrowPosition = Vector3.one * float.MaxValue;
    }

    private void Start()
    {
        arrowImage.enabled = false;
    }

    private void Update()
    {
        UpdateCompass();
        MoveArrowTowardsTarget();
    }

    private void MoveArrowTowardsTarget()
    {
        currentArrowPosition = Vector3.Lerp(currentArrowPosition, targetArrowPosition, 0.1f);
        arrowRectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, currentArrowPosition.x, 32);
        arrowRectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, currentArrowPosition.y, 32);
    }

    private void UpdateCompass()
    {
        if (!collectibleTileCoordinates.Equals(new Vector2Int(int.MaxValue, int.MaxValue)))
        {
            // Enable arrow image if needed
            if (!arrowImage.enabled)
            {
                arrowImage.enabled = true;
            }

            // Compute direction vector & distance
            Vector2 targetDirection = Vector2.zero;
            float targetDistance = 200; // value that is large enough to go off screen
            if (collectibleTransform != null)
            {
                targetDirection = (collectibleTransform.position - GameManager.instance.player.transform.position);
                targetDistance = targetDirection.magnitude;
            }
            else
            {
                targetDirection = new Vector3(collectibleTileCoordinates.x * MapBehaviour.instance.tileSize.x, collectibleTileCoordinates.y * MapBehaviour.instance.tileSize.y, 0) - GameManager.instance.player.transform.position;
            }
            targetDirection.Normalize();

            // Update animator using collectible direction
            float angle = Vector3.SignedAngle(targetDirection, Vector3.up, Vector3.forward);
            arrowAnimator.SetFloat("angle", angle);

            RaycastHit2D hit = Physics2D.Raycast(GameManager.instance.player.transform.position, targetDirection, targetDistance, LayerMask.GetMask("CompassFrameTriggers"));
            if (hit.collider != null)
            {
                // There is a collision. Update arrow position accordingly
                arrowImage.enabled = true;
                Vector3 hitScreenPosition = GameManager.instance.gameCamera.GetComponent<Camera>().WorldToScreenPoint(hit.point);
                hitScreenPosition.x -= 26; // Why 26? I don't know!
                hitScreenPosition.y -= 26;
                if (currentArrowPosition.magnitude > 1000000)
                {
                    currentArrowPosition = hitScreenPosition;
                }
                targetArrowPosition = hitScreenPosition;
            }
            else if (collectibleTransform != null)
            {
                // No collision, but collectible transform exists.
                // It may be that the collectible is visible on the map. Therefore the compass arrow should be hidden 
                arrowImage.enabled = false;
                currentArrowPosition = Vector3.one * float.MaxValue;
            }

            if (collectibleTransform != null)
            {
                collectibleTransform.GetComponent<FixedCollectibleBehaviour>().SetArrowVisibility(!arrowImage.enabled);
            }
        }
    }

    public void SetCollectibleTileCoordinates(Vector2Int collectibleTileCoordinates)
    { 
        this.collectibleTileCoordinates = collectibleTileCoordinates;
    }

    public void SetCollectibleTransform(Transform collectibleTransform)
    {
        this.collectibleTransform = collectibleTransform;
    }
}
