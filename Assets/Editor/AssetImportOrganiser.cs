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
            // Editor folder exclusion
            if (assetPath.StartsWith("Assets/Editor/"))
            {
                continue;
            }

           
            string extension = Path.GetExtension(assetPath)?.ToLowerInvariant();

            if (!string.IsNullOrEmpty(extension))
            {
                // Instead of checking AssetFolderMap...
                // Search the DEFINED structure for the target path.
                // Later, you might get the 'active' structure if you have presets.
                string destinationFolder = FolderStructureManager.FindTargetPathForExtension( extension, FolderStructureManager.DefaultFolderStructure);

                if (!string.IsNullOrEmpty(destinationFolder))
                {
                    // Schedule the move using the path found from FolderNode structure
                    string currentAssetPath = assetPath; // Capture loop variable for the lambda
                    EditorApplication.delayCall += () => MoveAssetToFolder(currentAssetPath, destinationFolder);
                }
                else
                {
                    // No folder found in the structure definition for this extension
                    Debug.Log($"No defined folder found in structure for extension: {extension} (Asset: {assetPath})");
                }              
            }          
        }
    }

    private static void MoveAssetToFolder(string assetPath, string destinationFolder)
    {
        string newPath = Path.Combine(destinationFolder, Path.GetFileName(assetPath));

        if (!File.Exists(assetPath))
        {
            return; 
        }

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

        if (File.Exists(newPath))
        {
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(newPath);
            Debug.LogWarning($"Asset already exists at '{newPath}'. Moving to '{uniquePath}' instead.");
            newPath = uniquePath;
        }

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
