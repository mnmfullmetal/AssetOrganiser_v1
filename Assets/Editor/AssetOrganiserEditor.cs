using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using Unity.Plastic.Newtonsoft.Json;
using System.Linq;
using System;
using System.Net.WebSockets;


public class AssetOrganiserEditor : EditorWindow
{
    // wrapper for correct json deserialisation
    [Serializable]
    private class FolderNodeListWrapper
    {
        public List<FolderNode> RootNodes = new List<FolderNode>();
    }

    private List<FolderNode> workingPresetCopy;
    private DropdownField presetDropdown;
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

        // Query for the "Load Preset" button and its dropdownfield of saved presets. 
        var loadPresetButton = rootVisualElement.Q<Button>("LoadPresetButton");
        presetDropdown = rootVisualElement.Q<DropdownField>("LoadPresetDropdown");

        // Create dropdown choices from saved presets
        RefreshPresetDropdown();

        loadPresetButton.clicked += () =>
        {
            var selectedPreset = presetDropdown.value;
            if (string.IsNullOrEmpty(selectedPreset))
            {
                Debug.LogWarning("No preset selected");
                return;
            }

            var rootSaveDirectory = FolderStructureManager.PresetSaveDirectory;
            var loadPath = Path.Combine(rootSaveDirectory, selectedPreset).Replace('\\', '/');
            loadPath = loadPath + ".json";
            if (!File.Exists(loadPath))
            {
                EditorUtility.DisplayDialog("Load Error", $"Preset file '{selectedPreset}.json' not found.", "OK");

                RefreshPresetDropdown(); 
                return;
            }


            try
            {
                var jsonData = File.ReadAllText(loadPath);
                FolderNodeListWrapper loadedWrapper = JsonUtility.FromJson<FolderNodeListWrapper>(jsonData);
                List<FolderNode> loadedPresetList = null;
                if (loadedWrapper != null)
                {
                    loadedPresetList = loadedWrapper.RootNodes;
                }

                if (loadedPresetList != null)
                {
                    workingPresetCopy = DeepCloneList(loadedWrapper.RootNodes);

                    // Refresh the TreeView with the newly loaded data
                    List<TreeViewItemData<FolderNode>> updatedRootItems = BuildTreeViewData(workingPresetCopy);
                    folderTree.SetRootItems(updatedRootItems);

                    EditorUtility.DisplayDialog("Load Successful", $"Preset '{selectedPreset}' loaded.", "OK");
                    Debug.Log($"Preset '{selectedPreset}' loaded successfully.");
                }
                else
                {
                    throw new Exception("Preset file might be corrupted or in an invalid format.");
                }

            }
            catch (Exception e)
            {
                // Log a detailed error to the Unity Console for debugging
                Debug.LogError($"Failed to load preset '{selectedPreset}' from '{loadPath}'. An unexpected error occurred: {e.Message}\nStack Trace: {e.StackTrace}");

                
                EditorUtility.DisplayDialog(
                    "Load Preset Error",
                    $"Failed to load preset '{selectedPreset}'.\n\nThe file might be corrupted, unreadable, or not in the expected format.\n\nError details: {e.Message}",
                    "OK");
            }

            presetDropdown.index = (presetDropdown.choices.Count > 0) ? 0 : -1;


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
                
                var saveDirectory = FolderStructureManager.PresetSaveDirectory;
                var savePath = Path.Combine(saveDirectory, presetName).Replace("\\","/");
                savePath = savePath + ".json";

                try
                {
                    var wrapper = new FolderNodeListWrapper();
                    wrapper.RootNodes = workingPresetCopy; 
                    var jsonData = JsonUtility.ToJson(wrapper, true);
                    File.WriteAllText(savePath, jsonData);
                    EditorUtility.DisplayDialog("Save succesful", "Preset saved succesfully" , "OK");
                    RefreshPresetDropdown();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to save preset '{presetName}'. An unexpected error occurred: {e.Message}");
                    EditorUtility.DisplayDialog("Save Error", $"Failed to save preset '{presetName}'.\nError: {e.Message}", "OK");
                }

                savePresetText.value = string.Empty;
            };
        }

        var deletePresetButton = rootVisualElement.Q<Button>("DeletePresetButton");
        if (deletePresetButton != null )
        {
            deletePresetButton.clicked += () =>
            {
                var selectedItem = presetDropdown.value;
                if (string.IsNullOrEmpty(selectedItem))
                {
                    return;
                }

                var rootPresetFolder = FolderStructureManager.PresetSaveDirectory;
                var fileToDelete = Path.Combine(rootPresetFolder, selectedItem).Replace("\\", "/");
                fileToDelete = fileToDelete + ".json";
                if (!File.Exists(fileToDelete))
                {
                    return;
                }

                try
                {
                    File.Delete(fileToDelete);
                    EditorUtility.DisplayDialog("Deletion Successful", $"File '{fileToDelete}' Deleted.", "OK");

                }
                catch ( Exception e )
                {
                    Debug.LogError($"Failed to delete file '{fileToDelete}'. An unexpected error occurred: {e.Message}");

                }

                RefreshPresetDropdown();
                presetDropdown.index = (presetDropdown.choices.Count > 0) ? 0 : -1;





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


    private void RefreshPresetDropdown()
    {
        if (presetDropdown == null) return;

        var presetDirectory = FolderStructureManager.PresetSaveDirectory;
        List<string> presetNames = new List<string>();
        try
        {
            var filePaths = Directory.GetFiles(presetDirectory, "*.json");
            Debug.Log($"Found {filePaths.Length} *.json files.");

            foreach (var path in filePaths)
            {
                var presetName = Path.GetFileNameWithoutExtension(path);
                Debug.Log($"-- Extracted name: {presetName}");
                presetNames.Add(presetName);

            }

        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error reading presets from '{presetDirectory}': {ex.Message}");
        
            presetNames.Clear();
        }

        presetDropdown.choices = presetNames;
        Debug.Log($"Assigning {presetNames.Count} names to dropdown: {string.Join(", ", presetNames)}");

    }

    private List<FolderNode> DeepCloneList(List<FolderNode> originalList)
    {
        if (originalList == null) return null; 

        List<FolderNode> newList = new List<FolderNode>();
        foreach (var node in originalList)
        {
            // Use the static method from FolderStructureManager to clone each node
            FolderNode clonedNode = FolderStructureManager.CloneFolderNode(node);
            if (clonedNode != null)
            {
                newList.Add(clonedNode);
            }
        }
        return newList;
    }
}
