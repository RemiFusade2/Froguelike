using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

enum TypeOfPage
{
    None,
    TOC,
    ChapterSpread,
    Glossary
}

public class ChapterCollectionScreenBehaviour : MonoBehaviour
{
    [Header("Buttons")]
    public Button previousSpreadButton;
    public Button nextSpreadButton;
    public Button previousTOCButton;
    public Button glossaryButton;

    [Header("Table of Contents")]
    public GameObject tableOfContentsGO;
    public GameObject headerTextGO;
    public GameObject tocEntryPrefab;
    public Transform tocEntryParent;
    public Transform trash;

    [Header("Chapter info")]
    public GameObject chapterSpreadGO;
    public TextMeshProUGUI titleText;

    private TypeOfPage currentPageType = TypeOfPage.None;
    private int currentPageNr = 1;
    private int nrOfChaptersOnSpread;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            DisplayTOC(1);
        }
    }

    public void DisplayTOC(int tocSpread)
    {
        currentPageNr = tocSpread;
        currentPageType = TypeOfPage.TOC;

        chapterSpreadGO.SetActive(false);
        tableOfContentsGO.SetActive(true);

        // Remove previous entries.
        while (tocEntryParent.childCount > 0)
        {
            Destroy(tocEntryParent.GetChild(0).gameObject);
            tocEntryParent.GetChild(0).SetParent(trash);
        }

        // Add new entries.

        // Add an empty entry if it is the first TOC spread.
        if (tocSpread == 1)
        {
            GameObject tocEntryGO = Instantiate(tocEntryPrefab, tocEntryParent);
            TOCEntryButton tocEntry = tocEntryGO.GetComponent<TOCEntryButton>();
            tocEntry.InitializeEmpty();
            headerTextGO.SetActive(true);
            nrOfChaptersOnSpread = 25;
            // Hide the "go to previous page" button
            previousSpreadButton.interactable = false;
            previousSpreadButton.gameObject.SetActive(false);
        }
        else
        {
            headerTextGO.SetActive(false);
            nrOfChaptersOnSpread = 26;
            // Show the "go to previous page" button
            previousSpreadButton.interactable = true;
            previousSpreadButton.gameObject.SetActive(true);
        }

        int startOfRangeToDisplay = Mathf.Max(((tocSpread - 1) * nrOfChaptersOnSpread) - 1, 0);

        for (int chapterIndex = startOfRangeToDisplay; chapterIndex < startOfRangeToDisplay + nrOfChaptersOnSpread; chapterIndex++)
        {
            if (chapterIndex >= StoryManager.instance.GetListOfChaptersFromListOfStories().Count) break;
            ChapterData thisChapterData = StoryManager.instance.GetListOfChaptersFromListOfStories()[chapterIndex];
            GameObject tocEntryGO = Instantiate(tocEntryPrefab, tocEntryParent);
            TOCEntryButton tocEntry = tocEntryGO.GetComponent<TOCEntryButton>();
            tocEntry.Initialize(thisChapterData, chapterIndex + 1);
        }
    }

    public void DisplayChapterInfo(int chapterIndex)
    {
        currentPageType = TypeOfPage.ChapterSpread;

        ChapterData chapter = StoryManager.instance.GetListOfChaptersFromListOfStories()[chapterIndex];
        tableOfContentsGO.SetActive(false);
        chapterSpreadGO.SetActive(true);
        titleText.SetText(chapter.chapterTitle);
    }

    public void DisplayGlossary()
    {
        currentPageType = TypeOfPage.Glossary;
    }

    public void UpdateButtons()
    {

    }

    // Used by the buttons, the bool is used to change the current page index. True means +1 false means -1.
    public void ChangePageWithButton(bool nextPage)
    {
        currentPageNr += nextPage ? 1 : -1;
        ChangePage(currentPageNr);
    }

    public void ChangePage(int spread)
    {
        int totalNrOfChapters = ChapterManager.instance.chaptersScriptableObjectsList.Count;
        int neededNrOfTOCPages = 1;
        while ((totalNrOfChapters + 1 - (25 + ((neededNrOfTOCPages - 1) * 26))) > 0)
        {
            neededNrOfTOCPages++;
        }

        // Check the current page and decide what the next page is going to be.
        // TODO don't think I need the enums!
        switch (currentPageType)
        {
            case TypeOfPage.None:
                DisplayTOC(spread);
                currentPageType = TypeOfPage.TOC;

                break;
            case TypeOfPage.TOC:
                if (spread <= neededNrOfTOCPages)
                {
                    DisplayTOC(spread);
                }
                else
                {
                    DisplayChapterInfo(spread - neededNrOfTOCPages - 1);
                }

                break;
            case TypeOfPage.ChapterSpread:
                if (spread < neededNrOfTOCPages)
                {
                    DisplayTOC(spread);
                }
                else
                {
                    DisplayChapterInfo(spread - neededNrOfTOCPages - 1);
                }

                break;
            case TypeOfPage.Glossary:
                break;
            default:
                break;
        }

        // Update buttons!

        if (spread < neededNrOfTOCPages)
        {
            previousSpreadButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Table of contents");
            nextSpreadButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Table of contents");
        }
        else if (spread == neededNrOfTOCPages || spread == neededNrOfTOCPages + 1)
        {
            previousSpreadButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Table of contents");
            nextSpreadButton.GetComponentInChildren<TextMeshProUGUI>().SetText(StoryManager.instance.GetListOfChaptersFromListOfStories()[spread - neededNrOfTOCPages].chapterTitle);
        }
        else if (spread == neededNrOfTOCPages + StoryManager.instance.GetListOfChaptersFromListOfStories().Count)
        {
            previousSpreadButton.GetComponentInChildren<TextMeshProUGUI>().SetText(StoryManager.instance.GetListOfChaptersFromListOfStories()[spread - neededNrOfTOCPages - 2].chapterTitle);
            nextSpreadButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Glossary");
        }
        else
        {
            previousSpreadButton.GetComponentInChildren<TextMeshProUGUI>().SetText(StoryManager.instance.GetListOfChaptersFromListOfStories()[spread - neededNrOfTOCPages - 2].chapterTitle);
            nextSpreadButton.GetComponentInChildren<TextMeshProUGUI>().SetText(StoryManager.instance.GetListOfChaptersFromListOfStories()[spread - neededNrOfTOCPages].chapterTitle);
        }
    }
}
