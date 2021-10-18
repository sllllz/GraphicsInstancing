using UnityEngine;
using System.Collections.Generic;

namespace Extend.GraphicsInstancing
{
	public class CrowdSpawner : MonoBehaviour
	{
		private static List<CrowdSpawner> spawners = new List<CrowdSpawner>();

		public string[] options;
		public int sizeOfCrowd = 1000;
		public int selectedOption = 0;
		public int maxSize = 5000;
		public float radius = 100;
		public float slopeStart = 0;
		public float slopeAmount = 1;
		public Vector2 radiusScaler = Vector2.one;
		public bool showGUI = true;

		private string fps;
		private int previousFrame = 0;
		private int previousSelection = 0;
		private List<GraphicsMesh> spawnedObjects = new List<GraphicsMesh>();
		private int guiOffset = 0;

		void Start()
		{
			SpawnCrowd();
			InvokeRepeating("UpdateFPS", 0.0001f, 1f);
			guiOffset = spawners.Count;
			spawners.Add(this);
		}
		private void OnDestroy()
		{
			spawners.Remove(this);
		}
		void UpdateFPS()
		{
			fps = ((Time.frameCount - previousFrame) / 1f).ToString("00.00");
			previousFrame = Time.frameCount;
		}
		void SpawnCrowd()
		{
			int startIndex = spawnedObjects.Count;
			if(startIndex > sizeOfCrowd)
			{
				int toRemove = spawnedObjects.Count - sizeOfCrowd;
				for (int i = startIndex - toRemove; i < startIndex; i++)
				{
					if (spawnedObjects[i] != null) spawnedObjects[i].Destroy();
				}
				spawnedObjects.RemoveRange(startIndex - toRemove, toRemove);
			}
			startIndex = spawnedObjects.Count;
			previousSelection = selectedOption;

			Vector3 center = transform.position;
			for (int i = startIndex; i < sizeOfCrowd; i++)
			{
				Vector3 rand = Random.onUnitSphere * radius;
				Vector3 position = center + new Vector3(rand.x * radiusScaler.x, 0, rand.z * radiusScaler.y);
				float disFromCenter = Vector3.Distance(center, position);
				if (disFromCenter < slopeStart)
				{
				    position.y = center.y;
				}
				else
				{
				    position.y = (disFromCenter - slopeStart) / (radius - slopeStart) * slopeAmount;
				}
				RaycastHit hit;
				if (Physics.Raycast(position + Vector3.up * 10, Vector3.down, out hit, 50, -1))
				{
				    position.y = hit.point.y;
				}
				var g = GraphicsInstancingService.Instance.CreateMesh(options[selectedOption], position, new Vector3(0, 0, 0), Vector3.one);
				if(g == null)
					continue;
				spawnedObjects.Add(g);
			}
		}
		void OnGUI()
		{
			if (!showGUI)
				return;
			GUI.skin.label.richText = true;
			GUILayout.BeginArea(new Rect(Screen.width * 0.025f, Screen.height * 0.025f + (guiOffset * Mathf.Max(150f, Screen.height * 0.15f)) + (10 * guiOffset), Screen.width * 0.3f, Mathf.Max(200f, Screen.height * 0.15f)), GUI.skin.box);
			{
				GUI.color = Color.white;
				if (options.Length > 1)
				{
					GUI.color = selectedOption == 0 ? Color.green : Color.white;
					for (int i = 0; i < options.Length; i++)
					{
						GUI.color = selectedOption == i ? Color.green : Color.white;
						if (GUILayout.Button(options[i]))
						{
							previousSelection = selectedOption;
							selectedOption = i;
							SpawnCrowd();
						}
					}
					GUI.color = Color.white;
				}
				else
				{
					GUILayout.Label("<color=white><size=19><b>" + options[0] + "</b></size></color>");
				}
				int size = sizeOfCrowd;
				GUILayout.Label("<color=white><size=19><b>Crowd Size: " + sizeOfCrowd + "</b></size></color>");
				if (GUILayout.Button("playRun"))
				{
					foreach (var item in spawnedObjects)
					{
						GraphicsAnimatorMesh graphMesh = item as GraphicsAnimatorMesh;
						if(graphMesh != null)
							graphMesh.Play(Animator.StringToHash("run"));
					}
				}
				if (GUILayout.Button("playAttack"))
				{
					foreach (var item in spawnedObjects)
					{
						GraphicsAnimatorMesh graphMesh = item as GraphicsAnimatorMesh;
						if(graphMesh != null)
							graphMesh.Play(Animator.StringToHash("attack"));
					}
				}
				sizeOfCrowd = (int)GUILayout.HorizontalSlider(sizeOfCrowd, 0, maxSize);
				if (size != sizeOfCrowd)
				{
					CancelInvoke("SpawnCrowd");
					Invoke("SpawnCrowd", 1);
				}
				else
				{
					GUILayout.Label("<color=white><size=19><b>FPS: " + fps + "</b></size></color>");
				}
			}
			GUILayout.EndArea();
		}
	}
}