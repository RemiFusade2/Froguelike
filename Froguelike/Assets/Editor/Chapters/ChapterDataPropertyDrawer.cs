using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#region Property Drawers (custom inspector stuff)

[CustomPropertyDrawer(typeof(ChapterCondition))]
public class ChapterConditionDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        float leftColumnWidth = 60;
        float boxWidth = 20;

        // Not would affect the whole condition
        Rect notLabelRect = new Rect(position.x, position.y, leftColumnWidth - boxWidth, EditorGUIUtility.singleLineHeight);
        Rect notRect = new Rect(position.x + leftColumnWidth - boxWidth, position.y, boxWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(notLabelRect, new GUIContent("(NOT)"));
        EditorGUI.PropertyField(notRect, property.FindPropertyRelative("not"), GUIContent.none);

        // Start with the type of condition (this will determine which other fields are shown)
        Rect typeRect = new Rect(position.x + leftColumnWidth, position.y, position.width - leftColumnWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(typeRect, property.FindPropertyRelative("conditionType"), GUIContent.none);

        position.y += EditorGUIUtility.singleLineHeight;
        position.x += leftColumnWidth;
        position.width -= leftColumnWidth;

        var conditionType = property.FindPropertyRelative("conditionType");
        if (conditionType.enumValueFlag == (int)ChapterConditionType.UNLOCKED)
        {
            // Nothing more
        }
        else if (conditionType.enumValueFlag == (int)ChapterConditionType.PLAYED_CHAPTER)
        {
            // Choose chapter data     
            float labelWidth = 60;
            Rect chapterLabelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect chapterDataRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(chapterLabelRect, new GUIContent("Chapter:"));
            EditorGUI.PropertyField(chapterDataRect, property.FindPropertyRelative("chapterData"), GUIContent.none);
        }
        else if (conditionType.enumValueFlag == (int)ChapterConditionType.CHAPTER_COUNT)
        {
            // Choose chapter Min and Max values  
            float labelWidth = 40;
            Rect chapterMinLabelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect chapterMinRect = new Rect(position.x + labelWidth, position.y, position.width / 2 - labelWidth - 10, EditorGUIUtility.singleLineHeight);
            Rect chapterMaxLabelRect = new Rect(position.x + position.width / 2 + 10, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect chapterMaxRect = new Rect(position.x + position.width / 2 + labelWidth + 10, position.y, position.width / 2 - labelWidth - 10, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(chapterMinLabelRect, new GUIContent("Min:"));
            EditorGUI.PropertyField(chapterMinRect, property.FindPropertyRelative("minChapterCount"), GUIContent.none);
            EditorGUI.LabelField(chapterMaxLabelRect, new GUIContent("Max:"));
            EditorGUI.PropertyField(chapterMaxRect, property.FindPropertyRelative("maxChapterCount"), GUIContent.none);
        }
        else if (conditionType.enumValueFlag == (int)ChapterConditionType.ENVIRONMENT)
        {
            // Choose Environment type
            float labelWidth = 80;
            Rect environmentTypeLabelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect environmentTypeRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(environmentTypeLabelRect, new GUIContent("Environment:"));
            EditorGUI.PropertyField(environmentTypeRect, property.FindPropertyRelative("environmentType"), GUIContent.none);
        }
        else if (conditionType.enumValueFlag == (int)ChapterConditionType.HAT)
        {
            // Choose Hat type
            float labelWidth = 40;
            Rect hatTypeLabelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect hatTypeRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(hatTypeLabelRect, new GUIContent("Hat:"));
            EditorGUI.PropertyField(hatTypeRect, property.FindPropertyRelative("hatType"), GUIContent.none);
        }
        else if (conditionType.enumValueFlag == (int)ChapterConditionType.FRIEND)
        {
            // Choose Friend type
            float labelWidth = 50;
            Rect friendTypeLabelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect friendTypeRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(friendTypeLabelRect, new GUIContent("Friend:"));
            EditorGUI.PropertyField(friendTypeRect, property.FindPropertyRelative("friendType"), GUIContent.none);
        }
        else if (conditionType.enumValueFlag == (int)ChapterConditionType.RUN_ITEM)
        {
            // Choose Run item
            float labelWidth = 80;
            Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect propertyRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, new GUIContent("Item name:"));
            EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("itemName"), GUIContent.none);
        }
        else if (conditionType.enumValueFlag == (int)ChapterConditionType.CHARACTER)
        {
            // Choose Character
            float labelWidth = 75;
            Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect propertyRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, new GUIContent("Character:"));
            EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("characterData"), GUIContent.none);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int numberOfLines = 2;

        var conditionType = property.FindPropertyRelative("conditionType");
        if (conditionType.enumValueFlag == (int)ChapterConditionType.UNLOCKED)
        {
            numberOfLines = 1;
        }

        return numberOfLines * EditorGUIUtility.singleLineHeight;
    }
}

[CustomPropertyDrawer(typeof(FixedCollectible))]
public class FixedCollectibleDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        // Coordinates first
        {
            float labelWidth = 100;
            Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect propertyRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, new GUIContent("Tile coordinates:"));
            EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("tileCoordinates"), GUIContent.none);
        }
        position.y += EditorGUIUtility.singleLineHeight;

        // Then type of collectible
        {
            float labelWidth = 100;
            Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect propertyRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, new GUIContent("Collectible type:"));
            EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("collectibleType"), GUIContent.none);
        }
        position.y += EditorGUIUtility.singleLineHeight;


        // Then depending on type, feed the data for the right collectible type
        var collectibleType = property.FindPropertyRelative("collectibleType");
        if (collectibleType.enumValueFlag == (int)FixedCollectibleType.FRIEND)
        {
            float labelWidth = 100;
            Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect dataRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, new GUIContent("Friend:"));
            EditorGUI.PropertyField(dataRect, property.FindPropertyRelative("collectibleFriendType"), GUIContent.none);
        }
        else if (collectibleType.enumValueFlag == (int)FixedCollectibleType.HAT)
        {
            float labelWidth = 100;
            Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect dataRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, new GUIContent("Hat:"));
            EditorGUI.PropertyField(dataRect, property.FindPropertyRelative("collectibleHatType"), GUIContent.none);
        }
        else if (collectibleType.enumValueFlag == (int)FixedCollectibleType.STATS_ITEM)
        {
            float labelWidth = 100;
            Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect dataRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, new GUIContent("Stat Item:"));
            EditorGUI.PropertyField(dataRect, property.FindPropertyRelative("collectibleStatItemData"), GUIContent.none);
        }
        else if (collectibleType.enumValueFlag == (int)FixedCollectibleType.WEAPON_ITEM)
        {
            float labelWidth = 100;
            Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect dataRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, new GUIContent("Weapon Item:"));
            EditorGUI.PropertyField(dataRect, property.FindPropertyRelative("collectibleWeaponItemData"), GUIContent.none);
        }
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int numberOfLines = 3;
        return numberOfLines * EditorGUIUtility.singleLineHeight;
    }
}

[CustomPropertyDrawer(typeof(ChapterWeightChange))]
public class ChapterWeightChangeDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        // Chapter data
        Rect statRect = new Rect(position.x, position.y, position.width / 2, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(statRect, property.FindPropertyRelative("chapter"), GUIContent.none);

        // Weight change value
        Rect valueRect = new Rect(position.x + position.width / 2, position.y, position.width / 2, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("weightChange"), GUIContent.none);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}

#endregion