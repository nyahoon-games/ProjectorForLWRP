using UnityEngine;

namespace ProjectorForLWRP
{
    public static class HelperFunctions
    {
#if UNITY_EDITOR
        public static Material FindMaterial(string shaderName)
        {
            // the material is supposed to be placed at the same path as the shader.
            Shader shader = Shader.Find(shaderName);
            if (shader == null) return null;
            string path = UnityEditor.AssetDatabase.GetAssetPath(shader);
            path = path.Substring(0, path.Length - 6); // remove "shader" extension
            path += "mat"; // add "mat" extension
            return UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(Material)) as Material;
        }
#endif
    }
}
