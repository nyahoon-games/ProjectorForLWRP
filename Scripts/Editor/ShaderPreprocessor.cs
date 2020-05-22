using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using UnityEditor.Build;
namespace ProjectorForLWRP {
    public class ShaderPreprocessor : IPreprocessShaders
    {
        private ShaderKeyword PROJECTOR_SHADER_KEYWORD = new ShaderKeyword("FSR_PROJECTOR_FOR_LWRP");
        public int callbackOrder { get { return 0; } }
        public void OnProcessShader(Shader shader, ShaderSnippetData snippetData, IList<ShaderCompilerData> compilerDataList)
        {
            if (!IsProjectorShader(shader))
            {
                return;
            }
            // force enable/disable FSR_PROJECTOR_FOR_LWRP keyword
            bool isLWRP = GraphicsSettings.renderPipelineAsset is UnityEngine.Rendering.LWRP.LightweightRenderPipelineAsset;
            //Debug.Log("Preprocessing Shader: " + shader.name);
            for (int i = 0; i < compilerDataList.Count; ++i)
            {
                ShaderCompilerData data = compilerDataList[i];
                if (isLWRP)
                {
                    if (data.shaderKeywordSet.IsEnabled(PROJECTOR_SHADER_KEYWORD)) {
                        continue;
                    }
                    //Debug.Log("FSR_PROJECTOR_FOR_LWRP keyword enabled");
                    data.shaderKeywordSet.Enable(PROJECTOR_SHADER_KEYWORD);
                }
                else
                {
                    if (!data.shaderKeywordSet.IsEnabled(PROJECTOR_SHADER_KEYWORD))
                    {
                        continue;
                    }
                    //Debug.Log("FSR_PROJECTOR_FOR_LWRP keyword disabled");
                    data.shaderKeywordSet.Disable(PROJECTOR_SHADER_KEYWORD);
                }
                // check if the list already has the same keyword set.
                ShaderKeyword[] keywords = data.shaderKeywordSet.GetShaderKeywords();
                bool found = false;
                for (int j = 0; j < compilerDataList.Count && !found; ++j)
                {
                    if (keywords.Length != compilerDataList[j].shaderKeywordSet.GetShaderKeywords().Length)
                    {
                        continue;
                    }
                    found = true;
                    foreach (ShaderKeyword keyword in keywords)
                    {
                        if (!compilerDataList[j].shaderKeywordSet.IsEnabled(keyword))
                        {
                            found = false;
                            break;
                        }
                    }
                }
                if (found)
                {
                    compilerDataList.RemoveAt(i);
                    --i;
                }
                else
                {
                    compilerDataList[i] = data;
                }
            }
        }
        private bool IsProjectorShader(Shader shader)
        {
            if (shader.name == "Hidden/ProjectorForLWRP/StencilPass")
            {
                return false;
            }
            if (shader.name.Contains("DynamicShadowProjector/"))
            {
                if (shader.name.Contains("/Projector/"))
                {
                    return true;
                }
            }
            else if (shader.name.Contains("Projector"))
            {
                return true;
            }
            return false;
        }
    }
}
