using UnityEngine;
using XLHFramework.GCFrameWork.Base;

namespace XGC.Hall
{
    public class HallWorldPlayerLogic : ILogicBehaviour
    {
        public void OnCreate()
        {
            Debug.Log("HallWorldPlayerLogic OnCreate");
        }

        public void OnDestroy()
        {
            Debug.Log(" HallWorldPlayerLogic OnDestroy");
        }
    }
}