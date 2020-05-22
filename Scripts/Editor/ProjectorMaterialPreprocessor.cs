using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace ProjectorForLWRP
{
	public class ProjectorMaterialPreprocessor : IProcessSceneWithReport
	{
		public int callbackOrder { get { return 0; } }
		private void ProcessGameObject(GameObject gameObject)
		{
			Projector projector;
			ProjectorForLWRP projectorForLWRP;
			if (gameObject.TryGetComponent<Projector>(out projector) && gameObject.TryGetComponent<ProjectorForLWRP>(out projectorForLWRP))
			{
				if (projector.material != null)
				{
					projector.material.EnableKeyword("FSR_PROJECTOR_FOR_LWRP");
				}
			}
			for (int i = 0; i < gameObject.transform.childCount; ++i)
			{
				ProcessGameObject(gameObject.transform.GetChild(i).gameObject);
			}
		}
		public void OnProcessScene(UnityEngine.SceneManagement.Scene scene, BuildReport report)
		{
			var rootObjects = scene.GetRootGameObjects();
			foreach (var rootObj in rootObjects)
			{
				ProcessGameObject(rootObj);
			}
		}
	}
}

