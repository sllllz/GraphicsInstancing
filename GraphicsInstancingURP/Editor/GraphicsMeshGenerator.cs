using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;


namespace Extend.GraphicsInstancing
{
	class AnimationBakeInfo
	{
		public SkinnedMeshRenderer[] meshRender;
		public Animator animator;
		public int workingFrame;
		public float length;
		public int layer;
		public AnimationInfo info;
	}

	class GenerateObjectInfo
	{
		public Matrix4x4 worldMatrix;
		public int nameCode;
		public float animationTime;
		public int stateName;
		public int frameIndex;
		public int boneListIndex = -1;
		public Matrix4x4[] boneMatrix;
	}

	class VertexCache
	{
		public int nameCode;
		public Matrix4x4[] bindPose;
		public Transform[] bonePose;
	}

	public class GraphicsMeshGenerator : EditorWindow
	{
		private static GraphicsMeshGenerator s_window;
		private Vector2 scrollPosition;
		private Vector2 scrollPosition2;
		private GameObject generatedObject;
		[SerializeField]
		private GameObject generatedPrefab;
		[SerializeField]
		private List<AnimationClip> customClips = new List<AnimationClip>();
		private Dictionary<string, bool> generateAnims = new Dictionary<string, bool>();
		
		private List<AnimationInfo> aniInfo = new List<AnimationInfo>();
		private int aniFps = 30;
	   
		private Dictionary<int, VertexCache> generateVertexCachePool;
		private Dictionary<int, List<GenerateObjectInfo>> generateMatrixDataPool;
		private GenerateObjectInfo[] generateObjectData;
		private List<AnimationBakeInfo> generateInfo;
		private int currentDataIndex;
		private int generateCount;
		private AnimationBakeInfo workingInfo;
		private int totalFrame;
		private Dictionary<UnityEditor.Animations.AnimatorState, UnityEditor.Animations.AnimatorStateTransition[]> cacheTransition;
		private Dictionary<AnimationClip, UnityEngine.AnimationEvent[]> cacheAnimationEvent;
		private Transform[] boneTransform;
		private int boneCount = 20;
		private const int BakeFrameCount = 10000;
		private int textureBlockWidth = 4;
		private int textureBlockHeight = 10;
		private int[] standardTextureSize = { 64, 128, 256, 512, 1024 };
		private int bakedTextureIndex;
		private Texture2D[] bakedBoneTexture = null;
		private int pixelX = 0, pixelY = 0;

		// Use this for initialization
		private void OnEnable()
		{
			generateInfo = new List<AnimationBakeInfo>();
			cacheTransition = new Dictionary<UnityEditor.Animations.AnimatorState, UnityEditor.Animations.AnimatorStateTransition[]>();
			cacheAnimationEvent = new Dictionary<AnimationClip, UnityEngine.AnimationEvent[]>();
			generatedPrefab = null;
			generateVertexCachePool = new Dictionary<int, VertexCache>();
			generateMatrixDataPool = new Dictionary<int, List<GenerateObjectInfo>>();
			generateObjectData = new GenerateObjectInfo[BakeFrameCount];
			for (int i = 0; i != generateObjectData.Length; ++i)
			{
				generateObjectData[i] = new GenerateObjectInfo();
			}
			EditorApplication.update += GenerateAnimation;
		}

		private void OnDisable()
		{
			EditorApplication.update -= GenerateAnimation;
		}
		private void Reset()
		{
			pixelX = 0;
			pixelY = 0;
			bakedTextureIndex = 0;
			if (generateVertexCachePool != null)
				generateVertexCachePool.Clear();
			if (generateMatrixDataPool != null)
				generateMatrixDataPool.Clear();
			currentDataIndex = 0;
		}

		private void GenerateAnimation()
		{
			if (generateInfo.Count > 0 && workingInfo == null)
			{
				workingInfo = generateInfo[0];
				generateInfo.RemoveAt(0);

				workingInfo.animator.gameObject.SetActive(true);
				workingInfo.animator.Update(0);
				workingInfo.animator.Play(workingInfo.info.NameHash);
				workingInfo.animator.Update(0);
				workingInfo.workingFrame = 0;
				return;
			}
			if (workingInfo != null)
			{
				for (int j = 0; j != workingInfo.meshRender.Length; ++j)
				{
					GenerateBoneMatrix(workingInfo.meshRender[j].name.GetHashCode(),
											workingInfo.info.NameHash,
											workingInfo.workingFrame,
											Matrix4x4.identity,
											false);
				}
				float totalFrame = (float)workingInfo.info.TotalFrame;
				EditorUtility.DisplayProgressBar("Mesh Animator", "Generate " + workingInfo.info.Name, workingInfo.workingFrame / totalFrame);

				if (++workingInfo.workingFrame >= workingInfo.info.TotalFrame)
				{
					aniInfo.Add(workingInfo.info);
					if (generateInfo.Count == 0)
					{
						foreach (var obj in cacheTransition)
						{
							obj.Key.transitions = obj.Value;
						}
						cacheTransition.Clear();
						foreach (var obj in cacheAnimationEvent)
						{
							UnityEditor.AnimationUtility.SetAnimationEvents(obj.Key, obj.Value);
						}
						cacheAnimationEvent.Clear();
						PrepareBoneTexture(aniInfo);
						SetupAnimationTexture(aniInfo);
						SaveAnimationInfo(generatedPrefab.name);
						DestroyImmediate(workingInfo.animator.gameObject);
						EditorUtility.ClearProgressBar();
						EditorUtility.DisplayDialog("Mesh Animator", "生成成功: " + generatedPrefab.name, "OK");
					}

					if (workingInfo.animator != null)
					{
						workingInfo.animator.gameObject.transform.position = Vector3.zero;
						workingInfo.animator.gameObject.transform.rotation = Quaternion.identity;
					}
					workingInfo = null;
					return;
				}
				
				float deltaTime = workingInfo.length / (workingInfo.info.TotalFrame - 1);
				workingInfo.animator.Update(deltaTime);
			}
		}

		[MenuItem("Tools/Graphics Mesh Generator", false)]
		private static void MakeWindow()
		{
			s_window = GetWindow(typeof(GraphicsMeshGenerator)) as GraphicsMeshGenerator;
		}

		private void OnGUI()
		{
			GUI.skin.label.richText = true;
			GUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();
			}
			GUILayout.EndHorizontal();

			GameObject prefab = EditorGUILayout.ObjectField("Asset to Generate", generatedPrefab, typeof(GameObject), true) as GameObject;
			if (prefab != generatedPrefab)
			{
				generateAnims.Clear();
				customClips.Clear();
				generatedPrefab = prefab;
			}

			bool error = false;
			bool bakeMesh = false;
			bool bakeSkinedMesh = false;
			if (generatedPrefab)
			{
				SkinnedMeshRenderer[] meshRender = generatedPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();
				if(meshRender.Length > 0)
				{
					bakeSkinedMesh = true;
					List<Matrix4x4> bindPose = new List<Matrix4x4>(150);
					boneTransform = UtilityHelper.MergeBone(meshRender, bindPose);
					if(boneTransform.Length == 0)
					{
						bakeSkinedMesh = false;
						DrawError("Error: The prefab should have Bone.");
						return;
					}
					Animator animator = generatedPrefab.GetComponentInChildren<Animator>();
					if (animator == null)
					{
						bakeSkinedMesh = false;
						DrawError("Error: The prefab should have a Animator Component.");
						return;
					}
					if (animator.runtimeAnimatorController == null)
					{
						bakeSkinedMesh = false;
						DrawError("Error: The prefab's Animator should have a Animator Controller.");
						return;
					}
					var clips = GetClips(animator);
					string[] clipNames = generateAnims.Keys.ToArray();
					int totalFrames = 0;
					List<int> frames = new List<int>();
					foreach (var clipName in clipNames)
					{
						if (!generateAnims[clipName])
							continue;

						AnimationClip clip = clips.Find(delegate(AnimationClip c) {
							if (c != null)
								return c.name == clipName;
							return false;
						});
						int framesToBake = clip ? (int)(clip.length * aniFps / 1.0f) : 1;
						framesToBake = Mathf.Clamp(framesToBake, 1, framesToBake);
						totalFrames += framesToBake;
						frames.Add(framesToBake);
					}

					int textureCount = 1;
					int textureWidth = CalculateTextureSize(out textureCount, frames.ToArray(), boneTransform);
					bakeSkinedMesh = textureCount > 0;
					if (textureCount == 0)
					{
						DrawError("Error: There is certain animation's frames which is larger than a whole texture.");
						return;
					}
					else if (textureCount == 1)
						EditorGUILayout.LabelField(string.Format("Animation Texture will be one {0} X {1} texture", textureWidth, textureWidth));
					else
						EditorGUILayout.LabelField(string.Format("Animation Texture will be {2} {3} X {4} and one {0} X {1} textures", textureWidth, textureWidth, textureCount - 1, standardTextureSize[standardTextureSize.Length - 1], standardTextureSize[standardTextureSize.Length - 1]));

					aniFps = EditorGUILayout.IntSlider("FPS", aniFps, 1, 120);
					scrollPosition = GUILayout.BeginScrollView(scrollPosition);
					foreach (var clipName in clipNames)
					{
						AnimationClip clip = clips.Find(delegate(AnimationClip c) {
							if (c != null)
								return c.name == clipName;
							return false;
						});
						int framesToBake = clip ? (int)(clip.length * aniFps / 1.0f) : 1;
						framesToBake = Mathf.Clamp(framesToBake, 1, framesToBake);
						GUILayout.BeginHorizontal();
						{
							generateAnims[clipName] = EditorGUILayout.Toggle(string.Format("({0}) {1} ", framesToBake, clipName), generateAnims[clipName]);
							GUI.enabled = generateAnims[clipName];
							GUI.enabled = true;
						}
						GUILayout.EndHorizontal();
						if (framesToBake > 5000)
						{
							GUI.skin.label.richText = true;
							EditorGUILayout.LabelField("<color=red>Long animations degrade performance, consider using a higher frame skip value.</color>", GUI.skin.label);
						}
					}
					GUILayout.EndScrollView();
				}
				else
				{
					MeshRenderer render = generatedPrefab.GetComponentInChildren<MeshRenderer>();
					MeshFilter mesh = generatedPrefab.GetComponentInChildren<MeshFilter>();
					if(render != null && mesh != null)
					{
						bakeMesh = true;
					}
				}
			}

			if(generatedPrefab && !error)
			{
				if(bakeSkinedMesh)
				{
					EditorGUI.BeginDisabledGroup(workingInfo != null);
					if (GUILayout.Button(string.Format("Generate")))
					{
						BakeWithAnimator();
					}
					EditorGUI.EndDisabledGroup();
				}
				else if(bakeMesh)
				{
					if (GUILayout.Button(string.Format("Generate")))
					{
						BakeMesh();
					}
				}
			}
		}

		private void BakeMesh()
		{
			MeshRenderer render = generatedPrefab.GetComponentInChildren<MeshRenderer>();
			MeshFilter meshFilter = generatedPrefab.GetComponentInChildren<MeshFilter>();

			Mesh mesh = meshFilter.sharedMesh;
			Material material = render.sharedMaterial;
			string fileName = generatedPrefab.name;
			// GraphicsMeshInfo meshAnim = CreateMeshInfo(fileName, out var folder, out var create);
			bool create = false;
			string folder = string.Format("Assets/GraphicsInstancing/Resources/{0}/{1}", "GraphicsMesh", fileName);
			string infoPath = string.Format("{0}/{1}_config.asset", folder, fileName);
			string path = Application.dataPath.Replace("Assets", "") + folder;
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
			GraphicsMeshInfo meshAnim = AssetDatabase.LoadAssetAtPath(infoPath, typeof(GraphicsMeshInfo)) as GraphicsMeshInfo;
			if (meshAnim == null)
			{
				create = true;
				meshAnim = ScriptableObject.CreateInstance<GraphicsMeshInfo>();
			}

			string meshPath = string.Format("{0}/{1}_mesh.mesh", folder, fileName);
			Mesh newMesh = AssetDatabase.LoadAssetAtPath(meshPath, typeof(Mesh)) as Mesh;
			bool createMesh = false;
			if(newMesh == null)
			{
				createMesh = true;
				newMesh = new Mesh();
				newMesh.vertices = mesh.vertices;
				newMesh.triangles = mesh.triangles;
				newMesh.normals = mesh.normals;
				newMesh.uv = mesh.uv;
				newMesh.tangents = mesh.tangents;
				newMesh.RecalculateBounds();
			}
			if(createMesh)
				AssetDatabase.CreateAsset(newMesh, meshPath);
			meshAnim.Mesh = newMesh;
			// MoveMaterialToPath(material, folder);
			meshAnim.Material = material;
			EditorUtility.SetDirty(newMesh);
			if (create)
				AssetDatabase.CreateAsset(meshAnim, infoPath);
			EditorUtility.SetDirty(meshAnim);
			AssetDatabase.SaveAssets();
		}

		void MoveMaterialToPath(Material material, string folder)
		{
			string path = AssetDatabase.GetAssetPath(material);
			string name = path.Substring(path.LastIndexOf('/'));
			AssetDatabase.MoveAsset(path, folder + name);
			string[] deps = AssetDatabase.GetDependencies(path);
			foreach (var tempPath in deps)
			{
				Texture t = AssetDatabase.LoadAssetAtPath<Texture>(tempPath);
				if(t != null)
				{
					string tempName = tempPath.Substring(tempPath.LastIndexOf('/'));
					AssetDatabase.MoveAsset(tempPath, folder + tempName);
				}
			}
		}

		GraphicsMeshInfo CreateMeshInfo(string fileName, out string folder, out bool create)
		{
			create = false;
			folder = string.Format("Assets/GraphicsInstancing/Resources/{0}/{1}", "GraphicsMesh", fileName);
			string infoPath = string.Format("{0}/{1}_ani.asset", folder, fileName);
			string path = Application.dataPath.Replace("Assets", "") + folder;
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
			GraphicsMeshInfo meshAnim = AssetDatabase.LoadAssetAtPath(infoPath, typeof(GraphicsMeshInfo)) as GraphicsMeshInfo;
			if (meshAnim == null)
			{
				create = true;
				meshAnim = ScriptableObject.CreateInstance<GraphicsMeshInfo>();
			}

			return meshAnim;
		}

		// Mesh CreateMesh(string fileName, Mesh out string folder, out bool create)
		// {
		//     string meshPath = string.Format("{0}/{1}_mesh.mesh", folder, fileName);
		//     Mesh newMesh = AssetDatabase.LoadAssetAtPath(meshPath, typeof(Mesh)) as Mesh;
		//     bool createMesh = false;
		//     if(newMesh == null)
		//     {
		//         createMesh = true;
		//         newMesh = new Mesh();
		//         newMesh.vertices = mesh.vertices;
		//         newMesh.triangles = mesh.triangles;
		//         newMesh.normals = mesh.normals;
		//         newMesh.uv = mesh.uv;
		//         newMesh.tangents = mesh.tangents;
		//         newMesh.RecalculateBounds();
		//     }
		//     newMesh.colors = weights;
		//     newMesh.SetUVs(2, indexes);

		//     if(createMesh)
		//         AssetDatabase.CreateAsset(newMesh, meshPath);
			
		//     meshAnim.Mesh = newMesh;

		//     EditorUtility.SetDirty(newMesh);
		// }

		private void BakeWithAnimator()
		{
			if (generatedPrefab != null)
			{
				generatedObject = Instantiate(generatedPrefab);
				Selection.activeGameObject = generatedObject;
				generatedObject.transform.position = Vector3.zero;
				generatedObject.transform.rotation = Quaternion.identity;
				Animator animator = generatedObject.GetComponentInChildren<Animator>();
				SkinnedMeshRenderer[] meshRender = generatedObject.GetComponentsInChildren<SkinnedMeshRenderer>();
				List<Matrix4x4> bindPose = new List<Matrix4x4>(150);
				Transform[] boneTransform = UtilityHelper.MergeBone(meshRender, bindPose);
				Reset();
				AddMeshVertex2Generate(meshRender, boneTransform, bindPose.ToArray());
				animator.applyRootMotion = true;
				totalFrame = 0;

				UnityEditor.Animations.AnimatorController controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
				if (controller == null)
				{
					controller = (animator.runtimeAnimatorController as AnimatorOverrideController).runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
				}
				Debug.Assert(controller.layers.Length > 0);
				cacheTransition.Clear();
				cacheAnimationEvent.Clear();
				UnityEditor.Animations.AnimatorControllerLayer layer = controller.layers[0];
				AnalyzeStateMachine(layer.stateMachine, animator, meshRender, 0, aniFps, 0);
				generateCount = generateInfo.Count;

				if(generateCount == 0)
				{
					EditorUtility.DisplayDialog("Mesh Animator", "生成失败 没有可生成的动画: " + generatedPrefab.name, "OK");
					DestroyImmediate(generatedObject);
				}
			}
		}


		private void AnalyzeStateMachine(UnityEditor.Animations.AnimatorStateMachine stateMachine,
			Animator animator,
			SkinnedMeshRenderer[] meshRender,
			int layer,
			int bakeFPS,
			int animationIndex)
		{
			for (int i = 0; i != stateMachine.states.Length; ++i)
			{
				ChildAnimatorState state = stateMachine.states[i];
				AnimationClip clip = state.state.motion as AnimationClip;
				bool needBake = false;
				if (clip == null)
					continue;
				if (!generateAnims.TryGetValue(clip.name, out needBake))
					continue;
				foreach (var obj in generateInfo)
				{
					if (obj.info.Name == clip.name)
					{
						needBake = false;
						break;
					}
				}

				if (!needBake)
					continue;

				AnimationBakeInfo bake = new AnimationBakeInfo();
				bake.length = clip.averageDuration;
				bake.animator = animator;
				bake.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
				bake.meshRender = meshRender;
				bake.layer = layer;
				bake.info = new AnimationInfo();
				bake.info.Name = clip.name;
				bake.info.NameHash = state.state.nameHash;
				bake.info.AnimationIndex = animationIndex;
				bake.info.TotalFrame = (int)(bake.length * bakeFPS + 0.5f) + 1;
				bake.info.TotalFrame = Mathf.Clamp(bake.info.TotalFrame, 1, bake.info.TotalFrame);
				bake.info.Fps = bakeFPS;
				bake.info.Mode = clip.isLooping? WrapMode.Loop: clip.wrapMode;
				generateInfo.Add(bake);
				animationIndex += bake.info.TotalFrame;
				totalFrame += bake.info.TotalFrame;

				var eventList = new List<AnimationEvent>();
				foreach (var evt in clip.events)
				{
					AnimationEvent aniEvent = new AnimationEvent();
					aniEvent.Function = evt.functionName;
					aniEvent.StringParameter = evt.stringParameter;
					aniEvent.Time = evt.time;
					eventList.Add(aniEvent);
				}
				bake.info.Events = eventList.ToArray();

				cacheTransition.Add(state.state, state.state.transitions);
				state.state.transitions = null;
				cacheAnimationEvent.Add(clip, clip.events);
				UnityEngine.AnimationEvent[] tempEvent = new UnityEngine.AnimationEvent[0];
				UnityEditor.AnimationUtility.SetAnimationEvents(clip, tempEvent);
			}
			for (int i = 0; i != stateMachine.stateMachines.Length; ++i)
			{
				AnalyzeStateMachine(stateMachine.stateMachines[i].stateMachine, animator, meshRender, layer, bakeFPS, animationIndex);
			}
		}

		private void SaveAnimationInfo(string fileName)
		{
			var meshRender = generatedPrefab.GetComponentInChildren<SkinnedMeshRenderer>();
			Mesh mesh = meshRender.sharedMesh;
			Material material = meshRender.sharedMaterial;
			int blockWidth = textureBlockWidth;
			int blockHeight = textureBlockHeight;

			string folder = string.Format("Assets/GraphicsInstancing/Resources/{0}/{1}", "GraphicsMesh", fileName);
			string infoPath = string.Format("{0}/{1}_config.asset", folder, fileName);
			string aniPath = string.Format("{0}/{1}_ani.asset", folder, fileName);
			string path = Application.dataPath.Replace("Assets", "") + folder;
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
			GraphicsMeshInfo meshAnim = AssetDatabase.LoadAssetAtPath(infoPath, typeof(GraphicsMeshInfo)) as GraphicsMeshInfo;
			bool create = false;
			if (meshAnim == null)
			{
				create = true;
				meshAnim = ScriptableObject.CreateInstance<GraphicsMeshInfo>();
			}
			GraphicsAnimatorInfo animatorInfo = AssetDatabase.LoadAssetAtPath(aniPath, typeof(GraphicsAnimatorInfo)) as GraphicsAnimatorInfo;
			bool createAni = false;
			if (animatorInfo == null)
			{
				createAni = true;
				animatorInfo = ScriptableObject.CreateInstance<GraphicsAnimatorInfo>();
			}
			animatorInfo.BlockWidth = blockWidth;
			animatorInfo.BlockHeight = blockHeight;
			animatorInfo.AnimationInfos = aniInfo.ToArray();
			animatorInfo.DefaultAnimHash = aniInfo[0].NameHash;
			animatorInfo.AnimationTextures = new AnimationTexture[bakedBoneTexture.Length];
			for (int i = 0; i < bakedBoneTexture.Length; i++)
			{
				var tex = new AnimationTexture();
				tex.Width = bakedBoneTexture[i].width;
				tex.Height = bakedBoneTexture[i].height;
				tex.TexBytes = bakedBoneTexture[i].GetRawTextureData();
				animatorInfo.AnimationTextures[i] = tex;
			}

			BoneWeight[] boneWeights = mesh.boneWeights;
			int vertexCount = mesh.vertexCount;
			Debug.Assert(boneWeights.Length > 0);
			Color[] weights = new Color[vertexCount];
			
			int[] boneIndex = null;
			if (meshRender.bones.Length != boneTransform.Length)
			{
				if (meshRender.bones.Length == 0)
				{
					boneIndex = new int[1];
					int hashRenderParentName = meshRender.transform.parent.name.GetHashCode();
					for (int k = 0; k != boneTransform.Length; ++k)
					{
						if (hashRenderParentName == boneTransform[k].name.GetHashCode())
						{
							boneIndex[0] = k;
							break;
						}
					}
				}
				else
				{
					boneIndex = new int[meshRender.bones.Length];
					for (int j = 0; j != meshRender.bones.Length; ++j)
					{
						boneIndex[j] = -1;
						Transform trans = meshRender.bones[j];
						int hashTransformName = trans.name.GetHashCode();
						for (int k = 0; k != boneTransform.Length; ++k)
						{
							if (hashTransformName == boneTransform[k].name.GetHashCode())
							{
								boneIndex[j] = k;
								break;
							}
						}
					}

					if (boneIndex.Length == 0)
					{
						boneIndex = null;
					}
				}
			}

			List<Vector4> indexes = new List<Vector4>(vertexCount);
			for (int i = 0; i < vertexCount; i++)
			{
				weights[i].r = boneWeights[i].weight0;
				weights[i].g = boneWeights[i].weight1;
				weights[i].b = boneWeights[i].weight2;
				weights[i].a = boneWeights[i].weight3;

				indexes.Add(new Vector4(
					boneIndex == null ? boneWeights[i].boneIndex0 : boneIndex[boneWeights[i].boneIndex0], 
					boneIndex == null ? boneWeights[i].boneIndex1 : boneIndex[boneWeights[i].boneIndex1], 
					boneIndex == null ? boneWeights[i].boneIndex2 : boneIndex[boneWeights[i].boneIndex2],
					boneIndex == null ? boneWeights[i].boneIndex3 : boneIndex[boneWeights[i].boneIndex3]));
			}
			string meshPath = string.Format("{0}/{1}_mesh.mesh", folder, fileName);
			Mesh newMesh = AssetDatabase.LoadAssetAtPath(meshPath, typeof(Mesh)) as Mesh;
			bool createMesh = false;
			if(newMesh == null)
			{
				createMesh = true;
				newMesh = new Mesh();
				newMesh.vertices = mesh.vertices;
				newMesh.triangles = mesh.triangles;
				newMesh.normals = mesh.normals;
				newMesh.uv = mesh.uv;
				newMesh.tangents = mesh.tangents;
				newMesh.RecalculateBounds();
			}
			newMesh.colors = weights;
			newMesh.SetUVs(2, indexes);
			EditorUtility.SetDirty(newMesh);
			if(createMesh)
				AssetDatabase.CreateAsset(newMesh, meshPath);
			EditorUtility.SetDirty(animatorInfo);
			if(createAni)
				AssetDatabase.CreateAsset(animatorInfo, aniPath);
			
			meshAnim.AnimatorInfo = animatorInfo;
			meshAnim.Mesh = newMesh;
			// MoveMaterialToPath(material, folder);
			meshAnim.Material = material;
			EditorUtility.SetDirty(meshAnim);
			if (create)
				AssetDatabase.CreateAsset(meshAnim, infoPath);

			AssetDatabase.SaveAssets();
			aniInfo.Clear();
		}

		private List<AnimationClip> GetClips(Animator animator)
		{
			UnityEditor.Animations.AnimatorController controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
			if(controller == null)
			{
				controller = (animator.runtimeAnimatorController as AnimatorOverrideController).runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
			}
			return GetClipsFromStatemachine(controller.layers[0].stateMachine);
		}

		private List<AnimationClip> GetClipsFromStatemachine(UnityEditor.Animations.AnimatorStateMachine stateMachine)
		{
			List<AnimationClip> list = new List<AnimationClip>();
			for (int i = 0; i != stateMachine.states.Length; ++i)
			{
				UnityEditor.Animations.ChildAnimatorState state = stateMachine.states[i];
				if (state.state.motion is UnityEditor.Animations.BlendTree)
				{
					UnityEditor.Animations.BlendTree blendTree = state.state.motion as UnityEditor.Animations.BlendTree;
					ChildMotion[] childMotion = blendTree.children;
					for(int j = 0; j != childMotion.Length; ++j) 
					{
						list.Add(childMotion[j].motion as AnimationClip);
					}
				}
				else if (state.state.motion != null)
					list.Add(state.state.motion as AnimationClip);
			}
			for (int i = 0; i != stateMachine.stateMachines.Length; ++i)
			{
				list.AddRange(GetClipsFromStatemachine(stateMachine.stateMachines[i].stateMachine));
			}

			var distinctClips = list.Select(q => (AnimationClip)q).Distinct().ToList();
			for (int i = 0; i < distinctClips.Count; i++)
			{
				if (distinctClips[i] && generateAnims.ContainsKey(distinctClips[i].name) == false)
					generateAnims.Add(distinctClips[i].name, true);
			}
			return list;
		}

		private void GenerateBoneMatrix(int nameCode,
			int stateName,
			float stateTime,
			Matrix4x4 rootMatrix1stFrame,
			bool rootMotion)
		{
			UnityEngine.Profiling.Profiler.BeginSample("AddBoneMatrix()");
			VertexCache vertexCache = null;
			bool find = generateVertexCachePool.TryGetValue(nameCode, out vertexCache);
			if (!find)
				return;

			GenerateObjectInfo matrixData = generateObjectData[currentDataIndex++];
			matrixData.nameCode = nameCode;
			matrixData.stateName = stateName;
			matrixData.animationTime = stateTime;
			matrixData.worldMatrix = Matrix4x4.identity;
			matrixData.frameIndex = -1;
			matrixData.boneListIndex = -1;

			UnityEngine.Profiling.Profiler.BeginSample("AddBoneMatrix:update the matrix");
			if (generateMatrixDataPool.ContainsKey(stateName))
			{
				List<GenerateObjectInfo> list = generateMatrixDataPool[stateName];
				matrixData.boneMatrix = UtilityHelper.CalculateSkinMatrix(
						vertexCache.bonePose,
						vertexCache.bindPose,
						rootMatrix1stFrame,
						rootMotion);

				GenerateObjectInfo data = new GenerateObjectInfo();
				UtilityHelper.CopyMatrixData(data, matrixData);
				list.Add(data);
			}
			else
			{
				UnityEngine.Profiling.Profiler.BeginSample("AddBoneMatrix:ContainsKey");
				matrixData.boneMatrix = UtilityHelper.CalculateSkinMatrix(
					vertexCache.bonePose,
					vertexCache.bindPose,
					rootMatrix1stFrame,
					rootMotion);

				List<GenerateObjectInfo> list = new List<GenerateObjectInfo>();
				GenerateObjectInfo data = new GenerateObjectInfo();
				UtilityHelper.CopyMatrixData(data, matrixData);
				list.Add(data);
				generateMatrixDataPool[stateName] = list;

				UnityEngine.Profiling.Profiler.EndSample();
			}
			UnityEngine.Profiling.Profiler.EndSample();

			UnityEngine.Profiling.Profiler.EndSample();
		}

		private void AddMeshVertex2Generate(SkinnedMeshRenderer[] meshRender,
			Transform[] boneTransform,
			Matrix4x4[] bindPose)
		{
			boneCount = boneTransform.Length;
			textureBlockWidth = 4;
			textureBlockHeight = boneCount;
			for (int i = 0; i != meshRender.Length; ++i)
			{
				Mesh m = meshRender[i].sharedMesh;
				if (m == null)
					continue;

				int nameCode = meshRender[i].name.GetHashCode();
				if (generateVertexCachePool.ContainsKey(nameCode))
					continue;

				VertexCache vertexCache = new VertexCache();
				generateVertexCachePool[nameCode] = vertexCache;
				vertexCache.nameCode = nameCode;
				vertexCache.bonePose = boneTransform;
				vertexCache.bindPose = bindPose;
				break;
			}
		}

		private void PrepareBoneTexture(List<AnimationInfo> infoList)
		{
			int count = 1;
			int[] frames = new int[infoList.Count];
			for (int i = 0; i != infoList.Count; ++i)
			{
				frames[i] = infoList[i].TotalFrame;
			}
			int textureWidth = CalculateTextureSize(out count, frames);
			Debug.Assert(textureWidth > 0);

			bakedBoneTexture = new Texture2D[count];
			TextureFormat format = TextureFormat.RGBAHalf;
			for (int i = 0; i != count; ++i)
			{
				int width = count > 1 && i < count ? standardTextureSize[standardTextureSize.Length - 1] : textureWidth;
				bakedBoneTexture[i] = new Texture2D(width, width, format, false);
				bakedBoneTexture[i].filterMode = FilterMode.Point;
			}
		}

		public int CalculateTextureSize(out int textureCount, int[] frames, Transform[] bone = null)
		{
			int textureWidth = standardTextureSize[0];
			int blockWidth = 0;
			int blockHeight = 0;
			if (bone != null)
			{
				boneCount = bone.Length;
				blockWidth = 4;
				blockHeight = boneCount;
			}
			else
			{
				blockWidth = textureBlockWidth;
				blockHeight = textureBlockHeight;
			}

			int count = 1;
			for (int i = standardTextureSize.Length - 1; i >= 0; --i)
			{
				int size = standardTextureSize[i];
				int blockCountEachLine = size / blockWidth;
				int x = 0, y = 0;
				int k = 0;
				for (int j = 0; j != frames.Length; ++j)
				{
					int frame = frames[j];
					int currentLineEmptyBlockCount = (size - x) / blockWidth % blockCountEachLine;
					bool check = x == 0 && y == 0;
					x = (x + frame % blockCountEachLine * blockWidth) % size;
					if (frame > currentLineEmptyBlockCount)
					{
						y += (frame - currentLineEmptyBlockCount) / blockCountEachLine * blockHeight;
						y += currentLineEmptyBlockCount > 0 ? blockHeight : 0;
					}

					if (y + blockHeight > size)
					{
						x = y = 0;
						++count;
						k = j--;
						if (check)
						{
							if (i == standardTextureSize.Length - 1)
							{
								textureCount = 0;
								return -1;
							}
							else
								break;
						}
					}
				}

				bool suitable = false;
				if (count > 1 && i == standardTextureSize.Length - 1)
				{
					for (int m = 0; m != standardTextureSize.Length; ++m)
					{
						size = standardTextureSize[m];
						x = y = 0;
						for (int n = k; n < frames.Length; ++n)
						{
							int frame = frames[n];
							int currentLineEmptyBlockCount = (size - x) / blockWidth % blockCountEachLine;
							x = (x + frame % blockCountEachLine * blockWidth) % size;
							if (frame > currentLineEmptyBlockCount)
							{
								y += (frame - currentLineEmptyBlockCount) / blockCountEachLine * blockHeight;
								y += currentLineEmptyBlockCount > 0 ? blockHeight : 0;
							}
							if (y + blockHeight <= size)
							{
								suitable = true;
								break;
							}
						}
						if (suitable)
						{
							textureWidth = size;
							break;
						}
					}
				}
				else if (count > 1)
				{
					textureWidth = standardTextureSize[i + 1];
					count = 1;
					suitable = true;
				}

				if (suitable)
				{
					break;
				}
			}
			textureCount = count;
			return textureWidth;
		}

		public void SetupAnimationTexture(List<AnimationInfo> infoList)
		{
			int preNameCode = generateObjectData[0].stateName;
			for (int i = 0; i != currentDataIndex; ++i)
			{
				GenerateObjectInfo matrixData = generateObjectData[i];
				if (matrixData.boneMatrix == null)
					continue;
				if (preNameCode != matrixData.stateName)
				{
					preNameCode = matrixData.stateName;
					int totalFrames = currentDataIndex - i;
					for (int j = i; j != currentDataIndex; ++j)
					{
						if (preNameCode != generateObjectData[j].stateName)
						{
							totalFrames = j - i;
							break;
						}
					}

					int width = bakedBoneTexture[bakedTextureIndex].width;
					int height = bakedBoneTexture[bakedTextureIndex].height;
					int y = pixelY;
					int currentLineBlockCount = (width - pixelX) / textureBlockWidth % (width / textureBlockWidth);
					totalFrames -= currentLineBlockCount;
					if (totalFrames > 0)
					{
						int framesEachLine = width / textureBlockWidth;
						y += (totalFrames / framesEachLine) * textureBlockHeight;
						y += currentLineBlockCount > 0 ? textureBlockHeight : 0;
						if (height < y + textureBlockHeight)
						{
							++bakedTextureIndex;
							pixelX = 0;
							pixelY = 0;
							Debug.Assert(bakedTextureIndex < bakedBoneTexture.Length);
						}
					}

					foreach (var obj in infoList)
					{
						AnimationInfo info = obj;
						if (info.NameHash == matrixData.stateName)
						{
							info.AnimationIndex = pixelX / textureBlockWidth + pixelY / textureBlockHeight * bakedBoneTexture[bakedTextureIndex].width / textureBlockWidth;
							info.TextureIndex = bakedTextureIndex;
						}
					}
				}
				if (matrixData.boneMatrix != null)
				{
					Debug.Assert(pixelY + textureBlockHeight <= bakedBoneTexture[bakedTextureIndex].height);
					Color[] color = UtilityHelper.Convert2Color(matrixData.boneMatrix);
					bakedBoneTexture[bakedTextureIndex].SetPixels(pixelX, pixelY, textureBlockWidth, textureBlockHeight, color);
					matrixData.frameIndex = pixelX / textureBlockWidth + pixelY / textureBlockHeight * bakedBoneTexture[bakedTextureIndex].width / textureBlockWidth;
					pixelX += textureBlockWidth;
					if (pixelX + textureBlockWidth > bakedBoneTexture[bakedTextureIndex].width)
					{
						pixelX = 0;
						pixelY += textureBlockHeight;
					}
					if (pixelY + textureBlockHeight > bakedBoneTexture[bakedTextureIndex].height)
					{
						Debug.Assert(generateObjectData[i + 1].stateName != matrixData.stateName);
						++bakedTextureIndex;
						pixelX = 0;
						pixelY = 0;
						Debug.Assert(bakedTextureIndex < bakedBoneTexture.Length);
					}
				}
				else
				{
					Debug.Assert(false);
					List<GenerateObjectInfo> list = generateMatrixDataPool[matrixData.stateName];
					GenerateObjectInfo originalData = list[matrixData.boneListIndex] as GenerateObjectInfo;
					matrixData.frameIndex = originalData.frameIndex;

				}
			}
			currentDataIndex = 0;
		}

		private void DrawError(string text)
		{
			int w = (int)Mathf.Lerp(300, 900, text.Length / 200f);
			using (new EditorGUILayout.HorizontalScope(GUILayout.MinHeight(30)))
			{
				var style = new GUIStyle(GUI.skin.FindStyle("CN EntryErrorIcon"));
				style.margin = new RectOffset();
				style.contentOffset = new Vector2();
				GUILayout.Box("", style, GUILayout.Width(15), GUILayout.Height(15));
				var textStyle = new GUIStyle(GUI.skin.label);
				textStyle.contentOffset = new Vector2(10, s_window.position.width < w ? 0 : 5);
				GUILayout.Label(text, textStyle);
			}
		}
	}
}