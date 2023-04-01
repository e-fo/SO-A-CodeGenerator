using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PathUtil
{
    public static void CreatePath(string path)
    {
        // Split the path into individual components
        string[] pathParts = path.Split('/');
        string partialPath = pathParts[0];
        // Iterate through each component and create the directory if it does not exist
        for (int i = 0; i < pathParts.Length -1; i++)
        {
            if (!AssetDatabase.IsValidFolder(partialPath))
            {
                var parentf = partialPath.Substring(0,partialPath.LastIndexOf('/'));
                var folder = pathParts[i];
                AssetDatabase.CreateFolder(
                    partialPath.Substring(0,partialPath.LastIndexOf('/')), 
                    pathParts[i]
                    );
            }
            partialPath += $"/{pathParts[i + 1]}";
        }
    }
}