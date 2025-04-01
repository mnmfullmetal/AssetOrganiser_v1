using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class AssetImportOrganiser : AssetPostprocessor
{
    private void OnPostprocessTexture(Texture2D texture)
    {
        Debug.Log("texture imported" + assetPath);
        string newPath = "Assets/Images/" + Path.GetFileName(assetPath);

        if (AssetDatabase.MoveAsset(assetPath, newPath) == "")
        {
            Debug.Log("texture moved to: " + newPath);
        }
        else
        {
            Debug.Log("texture move failed.");
        }
    }
}