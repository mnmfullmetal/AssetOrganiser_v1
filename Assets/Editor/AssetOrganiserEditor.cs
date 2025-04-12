using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using Unity.Plastic.Newtonsoft.Json;
using System.Linq;


public class AssetOrganiserEditor : EditorWindow
{
    private List<FolderNode> workingPresetCopy;
    private static readonly char[] invalidFileNameChars = Path.GetInvalidFileNameChars();

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
        if (deleteFolderButton != null)
        {
            deleteFolderButton.SetEnabled(false);

            folderTree.selectionChanged += (evt => 
            { 
                var selection = folderTree.selectedItem as FolderNode;
                if (selection == null || selection.Path == "Assets/")
                {
                    deleteFolderButton.SetEnabled(false);
                }
                else
                {
                    deleteFolderButton.SetEnabled(true);
                }
            });

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

        var savePresetButton = rootVisualElement.Q<Button>("SavePresetButton");
        var savePresetText = rootVisualElement.Q<TextField>("SavePresetTextField");
        if(savePresetButton != null && savePresetText != null)
        {
            savePresetButton.clicked += () =>
            {
                var presetName = savePresetText.value;
                
                if (string.IsNullOrWhiteSpace(presetName) || presetName.IndexOfAny(invalidFileNameChars) != -1)
                {
                    Debug.LogWarning($"Invalid Preset Name: '{presetName}'. Name cannot be empty, whitespace, or contain invalid characters (e.g., / \\ : * ? \" < > |).");
                    return;

                }
                
                var saveDirectory = "ProjectSettings/AssetOrganiserPresets";
                if (!Directory.Exists(saveDirectory))
                {
                   Directory.CreateDirectory(saveDirectory);
                }
                var savePath = Path.Combine(saveDirectory, presetName).Replace("\\","/");
                savePath = savePath + ".json";

                try
                {
                    var jsonData = JsonUtility.ToJson(workingPresetCopy, true);
                    File.WriteAllText(savePath, jsonData);
                    EditorUtility.DisplayDialog("Save succesful", "Preset saved succesfully" , "OK");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to save preset '{presetName}'. An unexpected error occurred: {e.Message}");
                    EditorUtility.DisplayDialog("Save Error", $"Failed to save preset '{presetName}'.\nError: {e.Message}", "OK");
                }

            };
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
