using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


public class AssetOrganiserEditor : EditorWindow
{
   

    [MenuItem("Window/AssetOrganiserEditor")]
    public static void ShowExample()
    {
        AssetOrganiserEditor wnd = GetWindow<AssetOrganiserEditor>();
        wnd.titleContent = new GUIContent("Asset Organiser");
    }

    public void CreateGUI()
    {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/AssetOrganiserEditor.uxml");

        VisualElement root = visualTree.Instantiate();
        rootVisualElement.Add(root);

        var folderTree = rootVisualElement.Q<TreeView>("FolderStructure");

        List<TreeViewItemData<FolderNode>> rootItems = BuildTreeViewData(FolderStructureManager.DefaultFolderStructure);

        folderTree.SetRootItems(rootItems);
        folderTree.makeItem = () => new Label();
        folderTree.bindItem = (element, index) =>
        {
            var label = element as Label;
            label.text = (folderTree.GetItemDataForIndex<FolderNode>(index)).DisplayName;
        };

        var addFolderButton = rootVisualElement.Q<Button>("AddFolderButton");
        if (addFolderButton != null)
        {
            addFolderButton.SetEnabled(false);

            addFolderButton.clicked += () => Debug.Log("Add Folder Button Clicked!");
        }

        
    }



    private List<TreeViewItemData<FolderNode>> BuildTreeViewData(List<FolderNode> folderNodes)
    {
        List<TreeViewItemData<FolderNode>> treeViewItems = new List<TreeViewItemData<FolderNode>>();

        if (folderNodes == null) return treeViewItems; 

        foreach (var node in folderNodes)
        {
            List<TreeViewItemData<FolderNode>> childItems = BuildTreeViewData(node.Children);

            var itemData = new TreeViewItemData<FolderNode>(
                id: node.Path.GetHashCode(), 
                data: node,
                children: childItems 
            );

            treeViewItems.Add(itemData);
        }

        return treeViewItems;
    }
}
