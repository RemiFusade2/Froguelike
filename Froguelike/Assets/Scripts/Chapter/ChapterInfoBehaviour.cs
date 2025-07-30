using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChapterInfoBehaviour : MonoBehaviour
{
    public TextMeshProUGUI infoTitleTextMesh;
    public TextMeshProUGUI infoDescriptionTextMesh;
    public GameObject fixedCollectiblesParent;
    public GameObject powerUpsParent;
    public List<CollectibleSprites> collectibleSprites;

    private bool materialSetUpFinished = false;

    public void DisplayChapter(Chapter chapterInfo)
    {
        materialSetUpFinished = ChapterInfoDisplayManager.instance.DisplayChapterPage(chapterInfo, infoTitleTextMesh, infoDescriptionTextMesh, materialSetUpFinished, fixedCollectiblesParent, powerUpsParent);
    }
}
