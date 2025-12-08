using UnityEngine;

namespace XLHFramework.GCFrameWork.World
{
    public class WorldUpdater : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
            WorldManager.Update();

        }
    }
}
