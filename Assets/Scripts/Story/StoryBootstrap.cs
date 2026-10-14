using UnityEngine;

namespace TimeOfFragments.Story
{
    /// <summary>
    /// 入口。用 RuntimeInitializeOnLoadMethod 在**任何场景**加载后自动起游戏:
    /// 这样就算打开的是一个空场景(甚至没有挂任何脚本的场景), 也能直接 Play。
    /// 场景里如果已经手动摆了 StoryBootstrap, 这里就不会重复创建。
    /// </summary>
    public class StoryBootstrap : MonoBehaviour
    {
        static StoryBootstrap instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (instance != null) return;
            if (FindObjectOfType<StoryDirector>() != null) return;
            Boot();
        }

        public static StoryDirector Boot()
        {
            EnsureCamera();

            GameObject go = new GameObject("TimeOfFragments");
            StoryBootstrap bootstrap = go.AddComponent<StoryBootstrap>();
            instance = bootstrap;

            VNView view = VNView.Create(go.transform);
            StoryDirector director = go.AddComponent<StoryDirector>();
            director.Initialize(view);

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            return director;
        }

        static void EnsureCamera()
        {
            if (Camera.main != null) return;
            GameObject camGo = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            Camera cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.035f, 0.05f, 1f);
            cam.orthographic = true;
        }
    }
}