using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class AssetImportOrganiser : AssetPostprocessor
{

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            // Exclude Files in Editor folder
            if (assetPath.StartsWith("Assets/Editor/"))
            {
                continue;
            }
            
            // Get file extension of processed assets
            string extension = Path.GetExtension(assetPath)?.ToLowerInvariant();

            if (!string.IsNullOrEmpty(extension))
            {
                string destinationFolder = FolderStructureManager.FindTargetPathForExtension( extension, FolderStructureManager.DefaultFolderStructure);

                if (!string.IsNullOrEmpty(destinationFolder))
                {
                    // Schedule moving asset to target path 
                    string currentAssetPath = assetPath; 
                    EditorApplication.delayCall += () => MoveAssetToFolder(currentAssetPath, destinationFolder);
                }
                else
                {
                    Debug.Log($"No defined folder found in structure for extension: {extension} (Asset: {assetPath})");
                }              
            }          
        }
    }

    private static void MoveAssetToFolder(string assetPath, string destinationFolder)
    {
        // Create path for asset to move to
        string newPath = Path.Combine(destinationFolder, Path.GetFileName(assetPath));

        if (!File.Exists(assetPath))
        {
            return; 
        }

        //  Create paths normalised to Unity path structure
        string fileName = Path.GetFileName(assetPath);
        string targetPath = Path.Combine(destinationFolder, fileName).Replace('\\', '/');
        string normalizedAssetPath = Path.GetFullPath(assetPath).Replace('\\', '/');
        string normalizedTargetPath = Path.GetFullPath(targetPath).Replace('\\', '/');

        // Compare the normalized paths, ignoring case differences
        if (normalizedAssetPath.Equals(normalizedTargetPath, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"Asset Organiser: Asset '{fileName}' is already in the correct folder '{destinationFolder}'. No move needed."); 
            return; 
        }

        // Handle event of identical file paths
        if (File.Exists(newPath))
        {
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(newPath);
            Debug.LogWarning($"Asset already exists at '{newPath}'. Moving to '{uniquePath}' instead.");
            newPath = uniquePath;
        }

        // Move asset from its current path to the newly defined path
        string moveResult = AssetDatabase.MoveAsset(assetPath, newPath);

        if (string.IsNullOrEmpty(moveResult))
        {
            Debug.Log($"Asset moved to: {newPath}");
        }
        else
        {
            Debug.LogError($"Asset move failed for '{assetPath}' to '{newPath}'. Error: {moveResult}");
        }

    }
}
