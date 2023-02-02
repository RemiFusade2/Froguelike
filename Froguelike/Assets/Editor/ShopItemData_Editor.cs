using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

/// <summary>
/// Custom Inspector for ShopItemData scriptable objects.
/// </summary>
[CustomEditor(typeof(ShopItemData))]
public class ShopItemData_Editor : Editor
{
    // XML file containing the UI info for the custom inspector
    public VisualTreeAsset inspectorXML;

    public override VisualElement CreateInspectorGUI()
    {
        // Create a new VisualElement to be the root of our inspector UI
        VisualElement root = new VisualElement();

        // Load and clone a visual tree from UXML
        inspectorXML.CloneTree(root);

        // Draw the default inspector as a foldout menu
        VisualElement inspectorFoldout = root.Q("Default_Inspector");
        InspectorElement.FillDefaultInspector(inspectorFoldout, serializedObject, this);

        return root;
    }
}