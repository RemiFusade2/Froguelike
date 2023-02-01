using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class ShopItemData_Editor : EditorWindow
{
    [MenuItem("Window/UI Toolkit/ShopItemData_Editor")]
    public static void ShowExample()
    {
        ShopItemData_Editor wnd = GetWindow<ShopItemData_Editor>();
        wnd.titleContent = new GUIContent("ShopItemData_Editor");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        //VisualElement label = new Label("Hello World! From C#");
        //root.Add(label);

        // Import UXML
        //var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/ShopItemData_Editor.uxml");
        //VisualElement labelFromUXML = visualTree.Instantiate();
        //root.Add(labelFromUXML);

        // A stylesheet can be added to a VisualElement.
        // The style will be applied to the VisualElement and all of its children.
        //var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/ShopItemData_Editor.uss");
        //VisualElement labelWithStyle = new Label("Hello World! With Style");
        //labelWithStyle.styleSheets.Add(styleSheet);
        //root.Add(labelWithStyle);
    }
}