using UnityEngine;
using XGC.Hall;
using XLHFramework.GCFrameWork.World;

public class WorldTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        WorldManager.CreateWorld<HallWorld>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            WorldManager.DestroyWorld<HallWorld>();
        }
    }
}
