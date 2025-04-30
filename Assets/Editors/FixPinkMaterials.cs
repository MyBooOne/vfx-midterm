using UnityEditor;
using UnityEngine;

public class FixPinkMaterials
{
    [MenuItem("Tools/Fix Pink Materials")]
    static void FixAllMaterials()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Texture_Me/Magic Forest" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
            {
                mat.shader = Shader.Find("Standard");
            }
        }

        Debug.Log("✅ แก้ Material เป็น Standard เรียบร้อย");
    }
}

