using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoryManager : MonoBehaviour
{
    public static StoryManager instance;

    public List<Story> storyScriptableObjectList;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public List<ChapterData> GetListOfChaptersFromListOfStories()
    {
        List<ChapterData> listOfChapters = new List<ChapterData> { };

        foreach (Story story in storyScriptableObjectList)
        {
            foreach (ChapterData chapter in story.listOfChaptersInStory)
            {
                listOfChapters.Add(chapter);
            }
        }

        return listOfChapters;
    }
}
