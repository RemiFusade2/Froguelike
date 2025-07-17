using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A Story contains all the chapters that are part of that story, in order.
/// </summary>
[System.Serializable]
[CreateAssetMenu(fileName = "Story Data", menuName = "ScriptableObjects/Froguelike/Story Data", order = 1)]
public class Story : ScriptableObject
{
    [Header("Name of story")]
    [Tooltip("Not used for anything, just to know what the story is about")]
    public string nameOfStory;

    [Header("Chapters in this story:")]
    [Tooltip("1-5 chapters, they must be in order")]
    public List<ChapterData> listOfChaptersInStory;
}
