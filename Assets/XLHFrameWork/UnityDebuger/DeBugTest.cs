using UnityEngine;
using XLHFramework.UnityDebuger;
using XLHFramework.UnityDebuger.Config;

public class DeBugTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debuger.InitLog();
        Debuger.ColorLog(LogColor.Blue, "测试测试");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
