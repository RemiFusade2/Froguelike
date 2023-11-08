using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ResolutionPanelBehaviour : MonoBehaviour
{
    public TextMeshProUGUI resolutionTextMesh;

    public GameObject selectedImage;

    public void Initialize(string resolutionText)
    {
        resolutionTextMesh.text = resolutionText;
    }

    // TODO use this!
    public void SetSelected(bool isSelected)
    {
        Debug.Log("used it!");
        selectedImage.SetActive(isSelected);
    }
}
