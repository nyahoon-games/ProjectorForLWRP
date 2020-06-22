//
// ShaderUtils.cs
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectorForLWRP
{
    public static class ShaderUtils
    {
        static public class ShaderKeywordGroup<EnumType> where EnumType : System.Enum
        {
            public static EnumType[] s_enumValues;
            public static int[] s_intValues;
            public static Dictionary<EnumType, int> s_enumToInt;
            public static string[] s_keywords; // must be set before use
            static ShaderKeywordGroup()
            {
                var values = System.Enum.GetValues(typeof(EnumType));
                int arraySize = values.Length;
                s_enumValues = new EnumType[arraySize];
                s_intValues = new int[arraySize];
                s_enumToInt = new Dictionary<EnumType, int>();
                for (int i = 0; i < arraySize; ++i)
                {
                    object value = values.GetValue(i);
                    s_intValues[i] = (int)value;
                    s_enumValues[i] = (EnumType)value;
                    s_enumToInt.Add(s_enumValues[i], s_intValues[i]);
                }
            }
            private static bool IsEnumValueEqual(EnumType value1, int value2)
            {
                // Equals invokes boxing. Do not use.
                // return value1.Equals(value2);
                return s_enumToInt[value1] == value2;
            }
            public static void SetGlobalKeyword(CommandBuffer cmd, EnumType type)
            {
                for (int i = 0, count = s_enumValues.Length; i < count; ++i)
                {
                    int value = s_intValues[i];
                    string keyword = s_keywords[value];
                    if (string.IsNullOrEmpty(keyword))
                    {
                        // default value can be null or empty string
                        continue;
                    }
                    if (IsEnumValueEqual(type, value))
                    {
                        cmd.EnableShaderKeyword(keyword);
                    }
                    else
                    {
                        cmd.DisableShaderKeyword(keyword);
                    }
                }
            }
            public static void SetGlobalKeywordFlag(CommandBuffer cmd, bool enable)
            {
                Debug.Assert(s_keywords.Length == 2);
                Debug.Assert(string.IsNullOrEmpty(s_keywords[0]) && !string.IsNullOrEmpty(s_keywords[1]));
                if (enable)
                {
                    cmd.EnableShaderKeyword(s_keywords[1]);
                }
                else
                {
                    cmd.DisableShaderKeyword(s_keywords[1]);
                }
            }
            public static void SetMaterialKeyword(Material material, EnumType type)
            {
                for (int i = 0, count = s_enumValues.Length; i < count; ++i)
                {
                    int value = s_intValues[i];
                    string keyword = s_keywords[value];
                    if (string.IsNullOrEmpty(keyword))
                    {
                        // default value can be null or empty string
                        continue;
                    }
                    if (IsEnumValueEqual(type, value))
                    {
                        material.EnableKeyword(keyword);
                    }
                    else
                    {
                        material.DisableKeyword(keyword);
                    }
                }
            }
            public static void SetMaterialKeywordFlag(Material material, bool enable)
            {
                // for keyword groups which have null and one keyword only.
                Debug.Assert(s_keywords.Length == 2);
                Debug.Assert(string.IsNullOrEmpty(s_keywords[0]) && !string.IsNullOrEmpty(s_keywords[1]));
                if (enable)
                {
                    material.EnableKeyword(s_keywords[1]);
                }
                else
                {
                    material.DisableKeyword(s_keywords[1]);
                }
            }
            public static EnumType GetMaterialKeyword(Material material)
            {
                EnumType defaultValue = s_enumValues[0];
                for (int i = 0, count = s_enumValues.Length; i < count; ++i)
                {
                    int value = s_intValues[i];
                    string keyword = s_keywords[value];
                    if (string.IsNullOrEmpty(keyword))
                    {
                        // default value.
                        defaultValue = s_enumValues[i];
                        continue;
                    }
                    if (material.IsKeywordEnabled(keyword))
                    {
                        return s_enumValues[i];
                    }
                }
                return defaultValue;
            }
            public static bool IsMaterialKeywordFlagOn(Material material)
            {
                // for keyword groups which have null and one keyword only.
                Debug.Assert(s_keywords.Length == 2);
                Debug.Assert(string.IsNullOrEmpty(s_keywords[0]) && !string.IsNullOrEmpty(s_keywords[1]));
                return material.IsKeywordEnabled(s_keywords[1]);
            }
        }

        private static void CheckKeywordEnumType<EnumType>() where EnumType : System.Enum
        {
#if DEBUG
            // check before calling assert to avoid unncessary GC Alloc for string concatenation.
            if (ShaderKeywordGroup<EnumType>.s_keywords == null)
            {
                Debug.Assert(ShaderKeywordGroup<EnumType>.s_keywords != null, "ShaderKeywordGroup<" + typeof(EnumType).Name + "> is not initialized.");
            }
#endif
        }
        public static void SetGlobalKeyword<EnumType>(CommandBuffer cmd, EnumType type) where EnumType : System.Enum
        {
            CheckKeywordEnumType<EnumType>();
            ShaderKeywordGroup<EnumType>.SetGlobalKeyword(cmd, type);
        }
        public static void SetGlobalKeywordFlag<EnumType>(CommandBuffer cmd, bool enable) where EnumType : System.Enum
        {
            CheckKeywordEnumType<EnumType>();
            ShaderKeywordGroup<EnumType>.SetGlobalKeywordFlag(cmd, enable);
        }
        public static void SetMaterialKeyword<EnumType>(Material material, EnumType type) where EnumType : System.Enum
        {
            CheckKeywordEnumType<EnumType>();
            ShaderKeywordGroup<EnumType>.SetMaterialKeyword(material, type);
        }
        public static void SetMaterialKeywordFlag<EnumType>(Material material, bool enable) where EnumType : System.Enum
        {
            CheckKeywordEnumType<EnumType>();
            ShaderKeywordGroup<EnumType>.SetMaterialKeywordFlag(material, enable);
        }
        public static EnumType GetMaterialKeyword<EnumType>(Material material) where EnumType : System.Enum
        {
            CheckKeywordEnumType<EnumType>();
            return ShaderKeywordGroup<EnumType>.GetMaterialKeyword(material);
        }
        public static void GetMaterialKeyword<EnumType>(Material material, out EnumType type) where EnumType : System.Enum
        {
            CheckKeywordEnumType<EnumType>();
            type = ShaderKeywordGroup<EnumType>.GetMaterialKeyword(material);
        }
        public static bool IsMaterialKeywordFlagEnabled<EnumType>(Material material) where EnumType : System.Enum
        {
            CheckKeywordEnumType<EnumType>();
            return ShaderKeywordGroup<EnumType>.IsMaterialKeywordFlagOn(material);
        }

        public class ShaderProperty<Type, Name>
        {
            public static int id = Shader.PropertyToID(typeof(Name).Name);
            public static void Set(Material material, Type value)
            {
#if DEBUG
                if (ShaderPropertyFunctionsSelector<Type>.Functions == null) return;
#endif
                ShaderPropertyFunctionsSelector<Type>.Functions.Set(material, id, value);
            }
            public static void Set(MaterialPropertyBlock propertyBlock, Type value)
            {
#if DEBUG
                if (ShaderPropertyFunctionsSelector<Type>.Functions == null) return;
#endif
                ShaderPropertyFunctionsSelector<Type>.Functions.Set(propertyBlock, id, value);
            }
            public static void SetGlobal(CommandBuffer cmd, Type value)
            {
#if DEBUG
                if (ShaderPropertyFunctionsSelector<Type>.Functions == null) return;
#endif
                ShaderPropertyFunctionsSelector<Type>.Functions.SetGlobal(cmd, id, value);
            }
            static ShaderProperty()
            {
#if DEBUG
                if (ShaderPropertyFunctionsSelector<Type>.Functions == null)
                {
                    Debug.Assert(false, typeof(Name).Name + " shader property has invalid type " + typeof(Type).Name);
                }
#endif
            }
        }

        private interface IShaderPropertyFunctions<T>
        {
            void Set(Material material, int id, T value);
            void Set(MaterialPropertyBlock propertyBlock, int id, T value);
            void SetGlobal(CommandBuffer cmd, int id, T value);
        }
        private class ShaderPropertyFunctionsSelector<T>
        {
            static public IShaderPropertyFunctions<T> Functions;
            static ShaderPropertyFunctionsSelector()
            {
                ShaderPropertyFunctions.Activate();
            }
        }
        private class ShaderPropertyFunctions
            : IShaderPropertyFunctions<int>,
              IShaderPropertyFunctions<float>,
              IShaderPropertyFunctions<Color>,
              IShaderPropertyFunctions<Vector4>,
              IShaderPropertyFunctions<Matrix4x4>,
              IShaderPropertyFunctions<Texture>,
              IShaderPropertyFunctions<float[]>,
              IShaderPropertyFunctions<Vector4[]>,
              IShaderPropertyFunctions<Matrix4x4[]>
        {
            public static void Activate()
            {
                // call static constructor
            }
            static ShaderPropertyFunctions()
            {
                ShaderPropertyFunctions instance = new ShaderPropertyFunctions();
                ShaderPropertyFunctionsSelector<int>.Functions = instance;
                ShaderPropertyFunctionsSelector<float>.Functions = instance;
                ShaderPropertyFunctionsSelector<Color>.Functions = instance;
                ShaderPropertyFunctionsSelector<Vector4>.Functions = instance;
                ShaderPropertyFunctionsSelector<Matrix4x4>.Functions = instance;
                ShaderPropertyFunctionsSelector<Texture>.Functions = instance;
                ShaderPropertyFunctionsSelector<float[]>.Functions = instance;
                ShaderPropertyFunctionsSelector<Vector4[]>.Functions = instance;
                ShaderPropertyFunctionsSelector<Matrix4x4[]>.Functions = instance;
            }
            // float
            public void Set(Material material, int id, float value)
            {
                material.SetFloat(id, value);
            }
            public void Set(MaterialPropertyBlock propertyBlock, int id, float value)
            {
                propertyBlock.SetFloat(id, value);
            }
            public void SetGlobal(CommandBuffer cmd, int id, float value)
            {
                cmd.SetGlobalFloat(id, value);
            }
            // int
            public void Set(Material material, int id, int value)
            {
                material.SetInt(id, value);
            }
            public void Set(MaterialPropertyBlock propertyBlock, int id, int value)
            {
                propertyBlock.SetInt(id, value);
            }
            public void SetGlobal(CommandBuffer cmd, int id, int value)
            {
                cmd.SetGlobalInt(id, value);
            }
            // Color
            public void Set(Material material, int id, Color value)
            {
                material.SetColor(id, value);
            }
            public void Set(MaterialPropertyBlock propertyBlock, int id, Color value)
            {
                propertyBlock.SetColor(id, value);
            }
            public void SetGlobal(CommandBuffer cmd, int id, Color value)
            {
                cmd.SetGlobalColor(id, value);
            }
            // Vector4
            public void Set(Material material, int id, Vector4 value)
            {
                material.SetVector(id, value);
            }
            public void Set(MaterialPropertyBlock propertyBlock, int id, Vector4 value)
            {
                propertyBlock.SetVector(id, value);
            }
            public void SetGlobal(CommandBuffer cmd, int id, Vector4 value)
            {
                cmd.SetGlobalVector(id, value);
            }
            // Matrix4x4
            public void Set(Material material, int id, Matrix4x4 value)
            {
                material.SetMatrix(id, value);
            }
            public void Set(MaterialPropertyBlock propertyBlock, int id, Matrix4x4 value)
            {
                propertyBlock.SetMatrix(id, value);
            }
            public void SetGlobal(CommandBuffer cmd, int id, Matrix4x4 value)
            {
                cmd.SetGlobalMatrix(id, value);
            }
            // float[]
            public void Set(Material material, int id, float[] value)
            {
                material.SetFloatArray(id, value);
            }
            public void Set(MaterialPropertyBlock propertyBlock, int id, float[] value)
            {
                propertyBlock.SetFloatArray(id, value);
            }
            public void SetGlobal(CommandBuffer cmd, int id, float[] value)
            {
                cmd.SetGlobalFloatArray(id, value);
            }
            // Vector4[]
            public void Set(Material material, int id, Vector4[] value)
            {
                material.SetVectorArray(id, value);
            }
            public void Set(MaterialPropertyBlock propertyBlock, int id, Vector4[] value)
            {
                propertyBlock.SetVectorArray(id, value);
            }
            public void SetGlobal(CommandBuffer cmd, int id, Vector4[] value)
            {
                cmd.SetGlobalVectorArray(id, value);
            }
            // Matrix4x4[]
            public void Set(Material material, int id, Matrix4x4[] value)
            {
                material.SetMatrixArray(id, value);
            }
            public void Set(MaterialPropertyBlock propertyBlock, int id, Matrix4x4[] value)
            {
                propertyBlock.SetMatrixArray(id, value);
            }
            public void SetGlobal(CommandBuffer cmd, int id, Matrix4x4[] value)
            {
                cmd.SetGlobalMatrixArray(id, value);
            }
            // Texture
            public void Set(Material material, int id, Texture value)
            {
                material.SetTexture(id, value);
            }
            public void Set(MaterialPropertyBlock propertyBlock, int id, Texture value)
            {
                propertyBlock.SetTexture(id, value);
            }
            public void SetGlobal(CommandBuffer cmd, int id, Texture value)
            {
                cmd.SetGlobalTexture(id, value);
            }
        }
    }

    // temporarily define here. should be defined by Unity.
    [System.Flags]
    public enum RenderingLayerMask
    {
        Nothing = 0,
        Everything = -1,
        Layer1 = (1 << 0),
        Layer2 = (1 << 1),
        Layer3 = (1 << 2),
        Layer4 = (1 << 3),
        Layer5 = (1 << 4),
        Layer6 = (1 << 5),
        Layer7 = (1 << 6),
        Layer8 = (1 << 7),
        Layer9 = (1 << 8),
        Layer10 = (1 << 9),
        Layer11 = (1 << 10),
        Layer12 = (1 << 11),
        Layer13 = (1 << 12),
        Layer14 = (1 << 13),
        Layer15 = (1 << 14),
        Layer16 = (1 << 15),
        Layer17 = (1 << 16),
        Layer18 = (1 << 17),
        Layer19 = (1 << 18),
        Layer20 = (1 << 19),
        Layer21 = (1 << 20),
        Layer22 = (1 << 21),
        Layer23 = (1 << 22),
        Layer24 = (1 << 23),
        Layer25 = (1 << 24),
        Layer26 = (1 << 25),
        Layer27 = (1 << 26),
        Layer28 = (1 << 27),
        Layer29 = (1 << 28),
        Layer30 = (1 << 29),
        Layer31 = (1 << 30),
        Layer32 = (1 << 31),
    }
}
