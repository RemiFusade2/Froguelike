using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#region Property Drawers (custom inspector stuff)

[CustomPropertyDrawer(typeof(WeaponStatValue))]
public class WeaponStatValueDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        // Stat type first
        Rect statRect = new Rect(position.x, position.y, position.width / 2, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(statRect, property.FindPropertyRelative("stat"), GUIContent.none);

        // Then Value
        Rect valueRect = new Rect(position.x + position.width / 2, position.y, position.width / 2, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("value"), GUIContent.none);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}

#endregion
