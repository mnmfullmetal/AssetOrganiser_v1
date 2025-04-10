using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;


public class AssetOrganiserEditor : EditorWindow
{
    private List<FolderNode> workingPresetCopy;
   
    [MenuItem("Window/AssetOrganiserEditor")]
    public static void DisplayWindow()
    {
        AssetOrganiserEditor wnd = GetWindow<AssetOrganiserEditor>();
        wnd.titleContent = new GUIContent("Asset Organiser");
    }

    public void CreateGUI()
    {
        // Load and Instantiate uxml
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/AssetOrganiserEditor.uxml");
        VisualElement root = visualTree.Instantiate();
        rootVisualElement.Add(root);

        // Query for the TreeView element
        var folderTree = rootVisualElement.Q<TreeView>("FolderStructure");

        // Create copy of folder structure to edit and work with
        workingPresetCopy = new List<FolderNode>();
        foreach( var folder in FolderStructureManager.DefaultFolderStructure)
        {
            workingPresetCopy.Add(FolderStructureManager.CloneFolderNode(folder));
        }

        // Build the TreeViewItemData list
        List<TreeViewItemData<FolderNode>> rootItems = BuildTreeViewData(workingPresetCopy);

        // Populate the TreeView
        folderTree.SetRootItems(rootItems);
        folderTree.makeItem = () => new Label();
        folderTree.bindItem = (element, index) =>
        {
            var label = element as Label;
            label.text = (folderTree.GetItemDataForIndex<FolderNode>(index)).DisplayName;
        };

        //Query for "Add folder" button and textfield
        var addFolderButton = rootVisualElement.Q<Button>("AddFolderButton");
        var addFolderText = rootVisualElement.Q<TextField>("AddFolderTextField");
        if (addFolderButton != null && addFolderText != null)
        {
            // Ensure it STARTS disabled, matching the initial empty text field
            addFolderButton.SetEnabled(false);

            addFolderText.RegisterValueChangedCallback(evt =>
            {
                if (string.IsNullOrWhiteSpace(evt.newValue))
                {
                    addFolderButton.SetEnabled(false);
                }
                else
                {
                    addFolderButton.SetEnabled(true);
                }
            });

            // Add folder to the TreeView when "Add Folder" button is clicked
            addFolderButton.clicked += () =>
            {
                var parentFolder = folderTree.selectedItem as FolderNode;
                var newFolderName = addFolderText.value;
                if (parentFolder == null || string.IsNullOrEmpty(newFolderName))
                {
                    return;
                }
                 
                var newFolder = new FolderNode();

                string trimmedFolderName = newFolderName.Trim();
                newFolder.DisplayName = trimmedFolderName;

                var newPath = Path.Combine(parentFolder.Path, newFolderName);
                newFolder.Path = newPath;

                // Add the new folder to the structure, rebuild the editor, and reset the value of the text field for the user.
                parentFolder.Children.Add(newFolder);
                List<TreeViewItemData<FolderNode>> updatedRootItems = BuildTreeViewData(workingPresetCopy);
                folderTree.SetRootItems(updatedRootItems);
                folderTree.Rebuild();
                addFolderText.value = null;

            };
        }

        var deleteFolderButton = rootVisualElement.Q<Button>("RemoveFolderButton");
        deleteFolderButton.clicked += () =>
        {
            var folderToDelete = folderTree.selectedItem as FolderNode;
            if (folderToDelete == null || folderToDelete.Path == "Assets/")
            {
                return;
            }

            bool deleted = false;
            if (workingPresetCopy.Remove(folderToDelete))
            {
                deleted = true;
            }
            else
            {
                var parentNode = FolderStructureManager.FindParentNode(workingPresetCopy, folderToDelete);
                if (parentNode != null)
                {
                    if (parentNode.Children.Remove(folderToDelete))
                    {
                        deleted = true;
                    }
                    else
                    {
                        Debug.LogError("Failed to remove child from parent list, though parent was found.");
                    }
                }
                else
                {
                    Debug.LogError($"Could not find parent for node '{folderToDelete.DisplayName}' during deletion attempt.");
                    return;
                }

            }

            if (deleted)
            {
                List<TreeViewItemData<FolderNode>> updatedRootItems = BuildTreeViewData(workingPresetCopy);
                folderTree.SetRootItems(updatedRootItems);
                folderTree.Rebuild();
            }

        };

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
