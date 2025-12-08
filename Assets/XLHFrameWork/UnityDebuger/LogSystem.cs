using UnityEngine;
using XLHFramework.UnityDebuger.Config;

namespace XLHFramework.UnityDebuger
{
    public class LogSystem : MonoBehaviour
    {
        void Awake()
        {

#if OPEN_LOG
        Debuger.InitLog(new LogConfig
        {
            openLog = true,
            openTime = true,
            showThreadID = true,
            showColorName = true,
            logSave = true,
            showFPS = true,
        });
        Debuger.Log("Log");
        Debuger.LogWarning("LogWarning");
        Debuger.LogError("LogError");
        Debuger.ColorLog(LogColor.Red, "ColorLog");
        Debuger.LogGreen("LogGreen");
        Debuger.LogYellow("LogYellow");
#else
            //Debug.unityLogger.logEnabled = false;
#endif
        }
    }
}
