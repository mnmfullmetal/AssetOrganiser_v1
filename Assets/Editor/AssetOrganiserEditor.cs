using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System;



public class AssetOrganiserEditor : EditorWindow
{
    // wrapper for correct json deserialisation
    [Serializable]
    private class FolderNodeListWrapper
    {
        public List<FolderNode> RootNodes = new List<FolderNode>();
    }

    private FolderNode nodeSelectedForEdit = null;
    private List<FolderNode> workingPresetCopy;
    private ListView associatedExtensionsList;
    private DropdownField presetDropdown;
    

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
        workingPresetCopy = DeepCloneList(FolderStructureManager.DefaultFolderStructure);

        // Build the TreeViewItemData list
        List<TreeViewItemData<FolderNode>> rootItems = BuildTreeViewData(workingPresetCopy);

        // Populate the TreeView
        folderTree.SetRootItems(rootItems);
        folderTree.makeItem = () => new Label();
        folderTree.bindItem = (element, index) =>
        {
            var label = element as Label;
            label.text = (folderTree.GetItemDataForIndex<FolderNode>(index)).displayName;
        };

        // Query for the AssociatedExtensionsPanel and its elements.
        var associatedExtensionsPanel = rootVisualElement.Q<VisualElement>("AssociatedExtensionsPanel");
        associatedExtensionsList = rootVisualElement.Q<ListView>("AssociatedExtensionsList");
        var addMappingsButton = rootVisualElement.Q<Button>("AddMappingButton");
        var removeMappingsButton = rootVisualElement.Q<Button>("RemoveMappingButton");
        if (associatedExtensionsPanel != null && associatedExtensionsList != null && addMappingsButton != null && removeMappingsButton != null)
        {
            associatedExtensionsPanel.style.display = DisplayStyle.None;

            // Populate the Associated Extensions list 
            associatedExtensionsList.makeItem = () => new Label();
            associatedExtensionsList.bindItem = (element, index) =>
            {
                var label = element as Label;
                if (label != null && associatedExtensionsList.itemsSource != null && index >= 0 && index < associatedExtensionsList.itemsSource.Count)
                {
                    label.text = associatedExtensionsList.itemsSource[index] as string ?? "Invalid Data"; 
                }
                else if (label != null)
                {
                    label.text = "Error binding item"; 
                }
            };

            folderTree.selectionChanged += (evt =>
            {
                var selection = folderTree.selectedItem as FolderNode;
                if (selection == null || selection.path == "Assets/")
                {
                    associatedExtensionsPanel.style.display = DisplayStyle.None;
                }
                else
                {
                    associatedExtensionsPanel.style.display = DisplayStyle.Flex;

                    if (associatedExtensionsList != null)
                    {
                    
                        associatedExtensionsList.itemsSource = selection.associatedExtensions ?? new List<string>();

                        associatedExtensionsList.RefreshItems();
                    }


                }
            });

            removeMappingsButton.clicked += () =>
            {
                var mappingToRemove = associatedExtensionsList.selectedItem as string;
                if (string.IsNullOrEmpty(mappingToRemove))
                {
                    Debug.LogWarning("Selected item in extension list was not a valid string.");
                    return;
                }

                var selectedFolder = folderTree.selectedItem as FolderNode;
                if (selectedFolder == null)
                {
                    Debug.LogWarning("Selected folder not valid");
                    return;
                }

                selectedFolder.associatedExtensions.Remove(mappingToRemove);
                associatedExtensionsList.RefreshItems();
            };

            addMappingsButton.clicked += () =>
            {
                var selectedNode = folderTree.selectedItem as FolderNode;
                if (selectedNode == null || selectedNode.path == "Assets/")
                {
                    Debug.LogWarning("Select a valid folder node first.");
                    return;
                }

                nodeSelectedForEdit = selectedNode;

                AddMappingEditor wnd = GetWindow<AddMappingEditor>();
                wnd.titleContent = new GUIContent("Add Mapping");
                wnd.TargetNode = selectedNode;

                wnd.OnApplyMappings += HandleMappingsApplied; 
            };

        }



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

            if (selectedPreset == "Default")
            {
                workingPresetCopy = DeepCloneList(FolderStructureManager.DefaultFolderStructure);

                List<TreeViewItemData<FolderNode>> updatedRootItems = BuildTreeViewData(workingPresetCopy);
                folderTree.SetRootItems(updatedRootItems);

                EditorUtility.DisplayDialog("Load Default", "Default structure loaded.", "OK");
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
                Debug.LogError($"Failed to load preset '{selectedPreset}' from '{loadPath}'. An unexpected error occurred: {e.Message}\nStack Trace: {e.StackTrace}");

                
                EditorUtility.DisplayDialog(
                    "Load Preset Error",
                    $"Failed to load preset '{selectedPreset}'.\n\nThe file might be corrupted, unreadable, or not in the expected format.\n\nError details: {e.Message}",
                    "OK");
            }

            RefreshPresetDropdown();

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
                newFolder.displayName = trimmedFolderName;

                var newPath = Path.Combine(parentFolder.path, newFolderName);
                newFolder.path = newPath;

                // Add the new folder to the structure, rebuild the editor, and reset the value of the text field for the user.
                parentFolder.children.Add(newFolder);
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
                if (selection == null || selection.path == "Assets/")
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
                if (folderToDelete == null || folderToDelete.path == "Assets/")
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
                        if (parentNode.children.Remove(folderToDelete))
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
                        Debug.LogError($"Could not find parent for node '{folderToDelete.displayName}' during deletion attempt.");
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
                
                if (string.IsNullOrWhiteSpace(presetName) || presetName.IndexOfAny(FolderStructureManager.invalidFileNameChars) != -1)
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
                    var jsonData = EditorJsonUtility.ToJson(wrapper, true);
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

        // Query for the "Delete Preset" button
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

            };
        }
      
    }

    // Helper to recursively create the treeview
    private List<TreeViewItemData<FolderNode>> BuildTreeViewData(List<FolderNode> folderNodes)
    {
        List<TreeViewItemData<FolderNode>> treeViewItems = new List<TreeViewItemData<FolderNode>>();

        if (folderNodes == null) return treeViewItems; 

        foreach (var node in folderNodes)
        {
            List<TreeViewItemData<FolderNode>> childItems = BuildTreeViewData(node.children);

            var itemData = new TreeViewItemData<FolderNode>(
                id: node.path.GetHashCode(), 
                data: node,
                children: childItems 
            );

            treeViewItems.Add(itemData);
        }

        return treeViewItems;
    }


    // Helper to refresh preset dropdown options 
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
        catch (Exception ex)
        {
            Debug.LogError($"Error reading presets from '{presetDirectory}': {ex.Message}");
        
            presetNames.Clear();
        }

        presetNames.Insert(0, "Default");
        presetDropdown.choices = presetNames;
        presetDropdown.index = presetNames.Count > 0 ? 0 : -1;
        presetDropdown.value = string.Empty;
        Debug.Log($"Assigning {presetNames.Count} names to dropdown: {string.Join(", ", presetNames)}");

    }


    private List<FolderNode> DeepCloneList(List<FolderNode> originalList)
    {
        if (originalList == null) return null; 

        List<FolderNode> newList = new List<FolderNode>();
        foreach (var node in originalList)
        {
            FolderNode clonedNode = FolderStructureManager.CloneFolderNode(node);
            if (clonedNode != null)
            {
                newList.Add(clonedNode);
            }
        }
        return newList;
    }

    private void HandleMappingsApplied(string extensionToAdd)
    {
        if (!string.IsNullOrEmpty(extensionToAdd))
        {
            var selectedFolder = nodeSelectedForEdit;
            if (selectedFolder == null)
            {
                return;
            }

          

            if (selectedFolder.associatedExtensions.Contains(extensionToAdd))
            {
                EditorUtility.DisplayDialog("Mapping Error", "extension mapping already exists in this fodler", "OK");
                return;
            }

            var conflictingNode = FolderStructureManager.IsExtensionAlreadyInUse(workingPresetCopy, selectedFolder, extensionToAdd);
            if (conflictingNode != null)
            {
                EditorUtility.DisplayDialog("Mapping Error", $"Extension mapping'{extensionToAdd}' already mapped to {conflictingNode.displayName}. User must remove existing mapping before applying to another ", "OK");
                return;
            }

            selectedFolder.associatedExtensions.Add(extensionToAdd);

            associatedExtensionsList.RefreshItems();
        }


    }
}
