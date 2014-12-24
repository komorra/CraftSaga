using System.Linq;
using UnityEngine;
using System.Collections;

public class DebugGUI : MonoBehaviour {

    void OnGUI()
    {
        GUILayout.Label("Working threads: " + Threader.Active.WorkingCount);
        GUILayout.Label("Last async gen time: " + Threader.Active.LastGenerationTime);
        GUILayout.Label("CPS: " + Threader.Active.CPS);
        GUILayout.Label("Min times processed: " + VoxelContainer.Containers.Values.Min(o => o.TimesProcessed));
        GUILayout.Label("Max times processed: " + VoxelContainer.Containers.Values.Max(o => o.TimesProcessed));
    }
}
