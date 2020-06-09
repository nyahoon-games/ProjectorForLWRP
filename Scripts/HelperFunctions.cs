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

        public static EnumType MaterialKeywordSelectGUI<EnumType>(Material material, string label, string[] keywords, EnumType[] values) where EnumType : System.Enum
        {
            Debug.Assert(keywords.Length == values.Length);
            EnumType currentValue = values[0];
            for (int i = 0; i < keywords.Length; ++i)
            {
                if (material.IsKeywordEnabled(keywords[i]))
                {
                    currentValue = values[i];
                    break;
                }
            }
            EnumType valueSelected = (EnumType)UnityEditor.EditorGUILayout.EnumPopup(label, currentValue);
            if (!currentValue.Equals(valueSelected))
            {
                for (int i = 0; i < keywords.Length; ++i)
                {
                    if (valueSelected.Equals(values[i]))
                    {
                        material.EnableKeyword(keywords[i]);
                    }
                    else
                    {
                        material.DisableKeyword(keywords[i]);
                    }
                }
            }
            return valueSelected;
        }
#endif
    }
}
