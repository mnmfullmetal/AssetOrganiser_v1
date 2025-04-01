using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Drawing;

public class AssetImportOrganiser : AssetPostprocessor
{

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            string extension = Path.GetExtension(Path.GetFileName(assetPath));

            if (AssetFolderSettings.assetFolderMap.ContainsKey(extension))
            {
                string destinationFolder = AssetFolderSettings.assetFolderMap[extension];
                EditorApplication.delayCall += () => MoveAssetToFolder(assetPath, destinationFolder);
            }
            else
            {
                Debug.Log("Asset type not handled: " + extension + " path: " + assetPath);
            }

        }

    }

    private static void MoveAssetToFolder(string assetPath, string destinationFolder)
    {
        string newPath = Path.Combine(destinationFolder, Path.GetFileName(assetPath));

        if (assetPath == newPath)
        {
            Debug.Log("asset already in correct folder" + newPath);
            EditorApplication.delayCall -= () => MoveAssetToFolder(assetPath, destinationFolder);
            return;
        }


        string moveResult = AssetDatabase.MoveAsset(assetPath, newPath);

        if (moveResult == "")
        {
            Debug.Log("Asset moved correctly: " + newPath);
        }
        else
        {
            Debug.Log("Asset move failed. Error: " + moveResult);
            Debug.Log("Error message: " + moveResult);
        }

        EditorApplication.delayCall -= () => MoveAssetToFolder(assetPath, destinationFolder);
    }
}
