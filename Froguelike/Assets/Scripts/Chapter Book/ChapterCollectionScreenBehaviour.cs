using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChapterCollectionScreenBehaviour : MonoBehaviour
{
    public GameObject tocEntryPrefab;
    public Transform tocParent;
    public Transform trash;

    // TODO remove
    private int nr = 1;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            DisplayTOC();
        }
    }

    public void DisplayTOC()
    {
        while (tocParent.childCount > 0)
        {
            Destroy(tocParent.GetChild(0).gameObject);
            tocParent.GetChild(0).SetParent(trash);
        }

        int chapterNr = 1;

        foreach (Story story in StoryManager.instance.storyScriptableObjectList)
        {
            for (int chapterIndex = 0; chapterIndex < story.listOfChaptersInStory.Count; chapterIndex++)
            {
                ChapterData thisChapterData = story.listOfChaptersInStory[chapterIndex];

                GameObject tocEntryGO = Instantiate(tocEntryPrefab, tocParent);
                TOCEntryButton tocEntry = tocEntryGO.GetComponent<TOCEntryButton>();
                tocEntry.Initialize(thisChapterData, chapterNr, chapterIndex);
                chapterNr++;
            }
        }
    }
}
