using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

namespace ProjectorForLWRP
{
	public class PostProcessBuildCheck : IPreprocessBuildWithReport, IProcessSceneWithReport, IPostprocessBuildWithReport
	{
		public int callbackOrder => 0;

		static bool s_checked = false;
		static bool s_checkOK;
		static RenderPipelineAsset s_currentRenderPipelineAsset;
		public void OnPreprocessBuild(BuildReport report)
		{
			DoCheck();
		}

		public void OnProcessScene(Scene scene, BuildReport report)
		{
			DoCheck();
			if (s_checkOK)
			{
				return;
			}
			foreach (GameObject go in scene.GetRootGameObjects())
			{
				if (IsObjectUsingProjector(go))
				{
					Debug.LogError(scene.name + " scene contains Projector For LWRP components but the current render pipeline asset does not have ProjectorRendererFeature.", s_currentRenderPipelineAsset);
					return;
				}
			}
		}

		public void OnPostprocessBuild(BuildReport report)
		{
			if (s_checkOK)
			{
				return;
			}
			HashSet<GUID> checkedAssets = new HashSet<GUID>();
#if UNITY_2020_1_OR_NEWER
			ScenesUsingAssets[] scenesAssetsLiset = report.scenesUsingAssets;
			foreach (ScenesUsingAssets scnesAssets in scenesAssetsLiset)
			{
				ScenesUsingAsset[] assets = scnesAssets.list;
				foreach (ScenesUsingAsset asset in assets)
				{
					GUID guid = AssetDatabase.GUIDFromAssetPath(asset.assetPath);
					if (checkedAssets.Contains(guid))
					{
						continue;
					}
					checkedAssets.Add(guid);
					CheckPrefabAtAsset(asset.assetPath);
				}
			}
#endif
#if UNITY_2019_3_OR_NEWER
			PackedAssets[] packedAssetsList = report.packedAssets;
			foreach (PackedAssets packedAssets in packedAssetsList)
			{
				PackedAssetInfo[] contents = packedAssets.contents;
				foreach (PackedAssetInfo assetInfo in contents)
				{
					if (checkedAssets.Contains(assetInfo.sourceAssetGUID))
					{
						continue;
					}
					checkedAssets.Add(assetInfo.sourceAssetGUID);
					CheckPrefabAtAsset(assetInfo.sourceAssetPath);
				}
			}
#endif
		}

		private static void DoCheck()
		{
			if (s_checked)
			{
				return;
			}
			RenderPipelineAsset renderPipelineAsset = QualitySettings.renderPipeline;
			if (renderPipelineAsset == null)
			{
#if UNITY_2019_3_OR_NEWER
				renderPipelineAsset = GraphicsSettings.defaultRenderPipeline;
#else
				renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
#endif
			}
			// check if the render pipeline asset has ProjectorRendnererFeature
			s_checkOK = renderPipelineAsset == null || ProjectorRendererFeature.GetProjectorRendererFeatureInRenderPipelineAsset(renderPipelineAsset) != null;
			s_currentRenderPipelineAsset = renderPipelineAsset;
			s_checked = true;
		}
		private bool IsObjectUsingProjector(GameObject gameObject)
		{
			return gameObject.GetComponentInChildren<ProjectorForLWRP>() != null;
		}

		private bool IsPrefabUsingProjector(Object prefab)
		{
			GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
			if (go == null)
			{
				return false;
			}
			bool result = IsObjectUsingProjector(go);
			Object.DestroyImmediate(go);
			return result;
		}

		private void CheckPrefabAtAsset(string assetPath)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
			if (prefab != null && IsPrefabUsingProjector(prefab))
			{
				Debug.LogError("This build contains " + prefab.name + " prefab asset which uses Projector For LWRP component.", prefab);
				Debug.LogError("Please make sure that the current render target pipeline has ProjectorRendererFeature.", s_currentRenderPipelineAsset);
			}
		}
	}
}
