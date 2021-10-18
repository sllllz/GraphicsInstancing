# GraphicsInstancing使用文档

包含有动作的Mesh和无动作的Mesh两种GraphicsInstancing。
    无动作的Mesh    可设置坐标，朝向，缩放。
    有动作的Mesh    可设置坐标，朝向，缩放，播放动画，接收事件。

注意：仅支持包含一个Mesh一个Material的模型，Material需要开启 Enable GPU Instancing。

Editor

```
生成配置步骤：
    1.准备Prefab，无动作Mesh的Prefab必须有MeshFilter和MeshRenderer组件，有动作Mesh必须有SkinnedMeshRenderer和Animator组件。
    2.打开"Tools/GraphicsMeshGenerator"窗口，将Prefab拖入窗口里的"Asset To Generate"选项里。
    3.若有动作，窗口会显示Animator组件上的所有动画，可设置导出帧率，可选择导出的动画列表。
    4.点击"Generate"按钮，会使用Prefab的名字在"Assets/GraphicsInstancing/Resources/GraphicsMesh/"目录下生成一个子目录，配置保存在子目录里。
        xxx_config:使用的模型，材质和动画的序列化文件。（必须有该文件在本目录下）
        xxx_ani:动画的序列化文件。（无动作的mesh没有该文件）
        xxx_mesh:处理后的模型文件。（必须有该文件在本目录下）
        材质球和贴图:该模型渲染需要的材质球和贴图。
    5.打开"Window/GraphicsMeshInfoConfig"窗口，将生成的xxx_config配置在窗口中，并给一个唯一的Name。
```

Runtime

```C#
var service = GraphicsInstancingService.Instance;
// 创建一个name的模型，name为"Window/GraphicsMeshInfoConfig"窗口配置的Name，并设置世界坐标，世界朝向，缩放
GraphicsMesh mesh = service.CreateMesh(name, pos, rotation, scale);
// 设置世界坐标
mesh.Pos = new Vector3(1, 0, 6);
// 设置世界朝向
mesh.Rotation = new Vector3(0, 90, 0);
// 设置缩放
mesh.Scale = Vector3.one;
// 显示模型，创建时默认显示
mesh.Show();
// 隐藏模型
mesh.Hide();
// 销毁模型
mesh.Destroy();

// 若该name对应的模型有动画，会返回 GraphicsAnimatorMesh 类型
GraphicsAnimatorMesh animatorMesh = (GraphicsAnimatorMesh)service.CreateMesh(name, pos, rotation, scale);
// 设置世界坐标
animatorMesh.Pos = new Vector3(1, 0, 6);
// 设置世界朝向
animatorMesh.Rotation = new Vector3(0, 90, 0);
// 设置缩放
animatorMesh.Scale = Vector3.one;
// 暂停动画播放
animatorMesh.Pause = true;
// 恢复动画播放
animatorMesh.Pause = false;
// 动画播放速度设置成0.5倍，默认为1
animatorMesh.Speed = 0.5f;
// 显示模型，创建时默认显示
mesh.Show();
// 隐藏模型
mesh.Hide();
// 销毁模型
mesh.Destroy();
// 播放名字叫Idle的动作，会将Pause设置成false
animatorMesh.Play("Idle")
// 播放名字叫Idle的动作，会将Pause设置成false
animatorMesh.Play(Animator.StringToHash("Idle"))
// 添加动画播放结束回调，循环动画不会触发该回调，animationName为结束的动画名称
animatorMesh.OnAnimationCompleteAction += (animationName) => {Debug.Log(animationName + " Play Complete!");}
// 添加动画事件回调，animationName为动画名称，eventArg为事件参数
animatorMesh.OnAnimationEventAction += (animationName, eventArg) => {Debug.Log(string.Format("Receive name {0} arg {1} event"), animationName, eventArg);}
```
