using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AssetFolderSettings
{
    // Dict of supported asset types (key) and their corresponding path in the folder structure (value)
    public static Dictionary<string, string> assetFolderMap = new Dictionary<string, string>()
    {
                { ".png", "Assets/Images/" },
                { ".jpg", "Assets/Images/" },
                { ".jpeg", "Assets/Images/" },
                { ".mat", "Assets/Materials/" },
                { ".prefab", "Assets/Prefabs/" },
                { ".fbx", "Assets/Models/" },
                { ".mp3", "Assets/Audio/" },
                { ".wav", "Assets/Audio/" },
                { ".cs", "Assets/Scripts/" }

    };

}
