using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class AssetImportOrganiser : AssetPostprocessor
{
    // OnPostprocessTexture called when texture2D has finished importing into editor, but is not yet compressed. 
    private void OnPostprocessTexture(Texture2D texture)
    {
        Debug.Log("texture imported" + assetPath);

        // EditorApplication.delaycall is a static event of EditorApplication class, I have used this to register a delegate function that will be called after the editor updates. 
        EditorApplication.delayCall += () => MoveTexture(assetPath);

    }

    //Movetexture Function called when editor refreshes, after importation of assets
    private void MoveTexture(string path)
    {
        string newPath = Path.Combine("Assets/Images/", Path.GetFileName(path));

        if (path == newPath)
        {
            Debug.Log("Texture already in correct folder: " + newPath);
           
            EditorApplication.delayCall -= () => MoveTexture(path);
            return; 
        }

        string moveResult = AssetDatabase.MoveAsset(path, newPath);

        if (moveResult == "")
        {
            Debug.Log("texture moved to: " + newPath);
        }
        else
        {
            Debug.Log("texture move failed. Error: " + moveResult);
        }
       
        EditorApplication.delayCall -= () => MoveTexture(path);
    }
}
