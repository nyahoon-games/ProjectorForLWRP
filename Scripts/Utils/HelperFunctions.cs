//
// HelperFunctions.cs
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

using System.Collections.Generic;
using UnityEngine;

namespace ProjectorForLWRP
{
    public static class HelperFunctions
    {
        public static void GarbageFreeSort<T>(List<T> list, IComparer<T> comparer)
        {
            int count = list.Count;
            for (int i = 1; i < count; ++i)
            {
                T rhs = list[i];
                bool inserted = false;
                for (int j = i - 1; 0 <= j; --j)
                {
                    T lhs = list[j];
                    if (comparer.Compare(lhs, rhs) < 0)
                    {
                        list[j + 1] = rhs;
                        inserted = true;
                        break;
                    }
                    list[j + 1] = lhs;
                }
                if (!inserted)
                {
                    list[0] = rhs;
                }
            }
        }
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

        public static EnumType MaterialKeywordSelectGUI<EnumType>(Material material, string label) where EnumType : System.Enum
        {
            EnumType currentValue = ShaderUtils.GetMaterialKeyword<EnumType>(material);
            EnumType valueSelected = (EnumType)UnityEditor.EditorGUILayout.EnumPopup(label, currentValue);
            if (!currentValue.Equals(valueSelected))
            {
                ShaderUtils.SetMaterialKeyword(material, valueSelected);
            }
            return valueSelected;
        }
#endif
    }
}
