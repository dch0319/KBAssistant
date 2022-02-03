using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace KBAssistant
{
    [BepInPlugin("KBAssistantUI", "KB挂机助手UI", "1.0.0.0")]
    public class KBAssistantUI : BaseUnityPlugin
    {
        public static float timeScale = 1;
        public static ConfigEntry<float> userTtimeScale;
        void Start()
        {
            userTtimeScale = Config.AddSetting("动画速度","值:", timeScale, new ConfigDescription("范围0.01-4", new AcceptableValueRange<float>((float)0.01, 4)));
        }

        void Update()
        {
            if (timeScale != userTtimeScale.Value)
            {
                TimeScaleMgr.Get().SetGameTimeScale(userTtimeScale.Value);
                Debug.Log("当前动画速度：" + userTtimeScale.Value);
                timeScale = userTtimeScale.Value;
            }
        }
    }
}
