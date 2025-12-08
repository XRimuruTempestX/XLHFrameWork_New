using UnityEngine;
using World = XLHFramework.GCFrameWork.World.World;

namespace XGC.Hall
{
    public class HallWorld : World
    {
        public override void OnCreate()
        {
            base.OnCreate();
            Debug.Log("hallworld OnCreate");
        }

        public override void OnDestroy()
        {
            Debug.Log(" hallworld OnDestroy");
        }
    }
}


