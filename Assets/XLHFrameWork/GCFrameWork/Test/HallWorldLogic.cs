using UnityEngine;
using XLHFramework.GCFrameWork.Base;


namespace XGC.Hall
{
    public class HallWorldLogic : ILogicBehaviour
    {
        public void OnCreate()
        {
            Debug.Log("HallWorldLogic OnCreate");
        }

        public void OnDestroy()
        {
            Debug.Log("HallWorldLogic OnDestroy");
        }
    }

}
