using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MenuTongueLineRendererBehaviour : MonoBehaviour
{
    [Header("References")]
    public LineRenderer tongueLineRenderer;
    public LineRenderer tongueOutlineLineRenderer;
    public MenuTongueTipBehaviour tongueTip;

    public void SetLineRenderersWidth(float width, float outlineWeight, float length)
    {
        float tipRatio = (length - 0.5f) / (length+0.01f);
        tipRatio = Mathf.Clamp(tipRatio, 0, 1);

        // The tongue line renderer goes from 0.5 * width for its root and 1 * width for the tip
        Keyframe[] tongueLineRendererWidthKeys = new Keyframe[3];

        tongueLineRendererWidthKeys[0] = new Keyframe(0, 0.15f, 0, 3);
        tongueLineRendererWidthKeys[1] = new Keyframe(tipRatio, 0.5f, 0, 3);
        tongueLineRendererWidthKeys[2] = new Keyframe(1, 1, 0, 0);

        AnimationCurve tongueLineRendererWidthCurve = new AnimationCurve(tongueLineRendererWidthKeys);
        tongueLineRenderer.widthCurve = tongueLineRendererWidthCurve;
        tongueLineRenderer.widthMultiplier = width;

        // The outline renderer does about the same thing, except the root width is (0.5 * width + outline * 2) and the tip width is (1 * width + outline * 2)
        Keyframe[] outlineLineRendererWidthKeys = new Keyframe[3];
        float outlineRootWidthRatio = (0.1f * width + outlineWeight * 2) / (width + outlineWeight * 2);

        outlineLineRendererWidthKeys[0] = new Keyframe(0, 0.4f, 0, 3);
        outlineLineRendererWidthKeys[1] = new Keyframe(tipRatio, 0.6f, 0, 3);
        outlineLineRendererWidthKeys[2] = new Keyframe(1, 1, 0, 0);

        AnimationCurve outlineLineRendererWidthCurve = new AnimationCurve(outlineLineRendererWidthKeys);
        tongueOutlineLineRenderer.widthCurve = outlineLineRendererWidthCurve;
        tongueOutlineLineRenderer.widthMultiplier = width + outlineWeight * 2;
    }
}
