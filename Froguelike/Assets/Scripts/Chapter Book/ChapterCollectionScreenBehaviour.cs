using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public TextMeshProUGUI descriptionText;
    public GameObject fixedCollectiblesParent;
    public GameObject powerUpsParent;

    [Header("Glossary spread")]
    public GameObject glossarySpreadGO;

    private int totalNrOfSpreadsNeeded;
    private int totalNrOfChapters;
    private int neededNrOfTOCSpreads;
    private int currentSpreadNr = 1;
    private int nrOfChaptersOnSpread;

    private bool setUpMaterials = true;

    private void Awake()
    {
        totalNrOfChapters = ChapterManager.instance.chaptersScriptableObjectsList.Count;
        neededNrOfTOCSpreads = 1;
        while ((totalNrOfChapters + 1 - (25 + ((neededNrOfTOCSpreads - 1) * 26))) > 0)
        {
            neededNrOfTOCSpreads++;
        }
        totalNrOfSpreadsNeeded = neededNrOfTOCSpreads + totalNrOfChapters + 1;
    }

    // Takes the player back to the Table of Contents spread where the chapter they were viewing is listed.
    public void PressTOCButton()
    {
        if (currentSpreadNr == totalNrOfSpreadsNeeded)
        {
            DisplayTOC(1);
            UIManager.instance.SetSelectedButton(glossaryButton);
        }
        else
        {
            int chapterIndex = currentSpreadNr - neededNrOfTOCSpreads;
            int goToTOCSpreadNr = 1;
            while (chapterIndex - (25 + ((goToTOCSpreadNr - 1) * 26)) > 0)
            {
                goToTOCSpreadNr++;
            }

            DisplayTOC(goToTOCSpreadNr);

            chapterIndex -= (goToTOCSpreadNr - 1) * 26;
            UIManager.instance.SetSelectedButton(tocEntryParent.GetChild(chapterIndex).GetComponent<Button>());
        }
    }

    public void DisplayTOC(int tocSpread)
    {
        currentSpreadNr = tocSpread;

        chapterSpreadGO.SetActive(false);
        glossarySpreadGO.SetActive(false);
        tableOfContentsGO.SetActive(true);
        // Activate the right buttons.
        nextSpreadButton.interactable = true;
        nextSpreadButton.gameObject.SetActive(true);
        glossaryButton.interactable = true;
        glossaryButton.gameObject.SetActive(true);
        previousTOCButton.interactable = false;
        previousTOCButton.gameObject.SetActive(false);

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
            UIManager.instance.SetSelectedButton(nextSpreadButton);
        }
        else
        {
            headerTextGO.SetActive(false);
            nrOfChaptersOnSpread = 26;
            // Show the "go to previous spread" button
            previousSpreadButton.interactable = true;
            previousSpreadButton.gameObject.SetActive(true);
        }

        int startOfRangeToDisplay = Mathf.Max(((tocSpread - 1) * nrOfChaptersOnSpread) - 1, 0);

        for (int chapterIndex = startOfRangeToDisplay; chapterIndex < startOfRangeToDisplay + nrOfChaptersOnSpread; chapterIndex++)
        {
            if (chapterIndex >= totalNrOfChapters) break;
            ChapterData thisChapterData = StoryManager.instance.GetListOfChaptersFromListOfStories()[chapterIndex];
            GameObject tocEntryGO = Instantiate(tocEntryPrefab, tocEntryParent);
            TOCEntryButton tocEntry = tocEntryGO.GetComponent<TOCEntryButton>();
            tocEntry.Initialize(thisChapterData, chapterIndex + 1, this);
        }

        UpdateButtons(currentSpreadNr);
    }

    public void PressTOCEntryButton(ChapterData chapterData)
    {
        int chapterIndex = StoryManager.instance.GetListOfChaptersFromListOfStories().IndexOf(chapterData);
        currentSpreadNr = chapterIndex + neededNrOfTOCSpreads + 1;
        DisplayChapterInfo(chapterIndex);

        UIManager.instance.SetSelectedButton(previousTOCButton);
    }

    public void DisplayChapterInfo(int chapterIndex)
    {
        ChapterData chapter = StoryManager.instance.GetListOfChaptersFromListOfStories()[chapterIndex];
        Chapter chapterInfo = ChapterManager.instance.GetChapterFromID(chapter.chapterID);

        setUpMaterials = ChapterInfoDisplayManager.instance.DisplayChapterSpread(chapterInfo, titleText, descriptionText, setUpMaterials, fixedCollectiblesParent, powerUpsParent);

        tableOfContentsGO.SetActive(false);
        glossarySpreadGO.SetActive(false);
        chapterSpreadGO.SetActive(true);
        // Activate the right buttons.
        previousSpreadButton.interactable = true;
        previousSpreadButton.gameObject.SetActive(true);
        nextSpreadButton.interactable = true;
        nextSpreadButton.gameObject.SetActive(true);
        glossaryButton.interactable = true;
        glossaryButton.gameObject.SetActive(true);
        previousTOCButton.interactable = true;
        previousTOCButton.gameObject.SetActive(true);

        titleText.SetText(chapter.chapterTitle);

        UpdateButtons(currentSpreadNr);
    }

    public void DisplayGlossary()
    {
        currentSpreadNr = totalNrOfSpreadsNeeded; 

        tableOfContentsGO.SetActive(false);
        chapterSpreadGO.SetActive(false);
        glossarySpreadGO.SetActive(true);

        // Activate the right buttons.
        previousSpreadButton.interactable = true;
        previousSpreadButton.gameObject.SetActive(true);
        nextSpreadButton.interactable = false;
        nextSpreadButton.gameObject.SetActive(false);
        glossaryButton.interactable = false;
        glossaryButton.gameObject.SetActive(false);
        previousTOCButton.interactable = true;
        previousTOCButton.gameObject.SetActive(true);
        UIManager.instance.SetSelectedButton(previousSpreadButton);

        UpdateButtons(currentSpreadNr);
    }

    public void UpdateButtons(int spread)
    {
        // Update buttons!
        if (spread < neededNrOfTOCSpreads)
        {
            previousSpreadButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Table of contents");
            nextSpreadButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Table of contents");
        }
        else if (spread == neededNrOfTOCSpreads || spread == neededNrOfTOCSpreads + 1)
        {
            previousSpreadButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Table of contents");
            nextSpreadButton.GetComponentInChildren<TextMeshProUGUI>().SetText(StoryManager.instance.GetListOfChaptersFromListOfStories()[spread - neededNrOfTOCSpreads].chapterTitle);
        }
        else if (spread >= neededNrOfTOCSpreads + totalNrOfChapters)
        {
            previousSpreadButton.GetComponentInChildren<TextMeshProUGUI>().SetText(StoryManager.instance.GetListOfChaptersFromListOfStories()[spread - neededNrOfTOCSpreads - 2].chapterTitle);
            nextSpreadButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Glossary");
        }
        else
        {
            previousSpreadButton.GetComponentInChildren<TextMeshProUGUI>().SetText(StoryManager.instance.GetListOfChaptersFromListOfStories()[spread - neededNrOfTOCSpreads - 2].chapterTitle);
            nextSpreadButton.GetComponentInChildren<TextMeshProUGUI>().SetText(StoryManager.instance.GetListOfChaptersFromListOfStories()[spread - neededNrOfTOCSpreads].chapterTitle);
        }
    }

    // Used by the buttons, the bool is used to change the current page index. True means +1 false means -1.
    public void ChangePageWithButton(bool nextPage)
    {
        currentSpreadNr += nextPage ? 1 : -1;
        ChangePage(currentSpreadNr);
    }

    public void ChangePage(int spread)
    {
        // Check the current page and decide what the next page is going to be.
        if (spread <= neededNrOfTOCSpreads)
        {
            DisplayTOC(spread);
        }
        else if (spread > neededNrOfTOCSpreads && spread <= neededNrOfTOCSpreads + totalNrOfChapters)
        {
            DisplayChapterInfo(spread - neededNrOfTOCSpreads - 1);
        }
        else
        {
            DisplayGlossary();
        }
    }
}
