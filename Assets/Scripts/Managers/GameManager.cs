using Unity.Burst;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject mEnemy;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10f, 100f, 220f, 140f), GUI.skin.box);
        
        bool useJobs = VATRuntimeSettings.UseJobs;
        bool toggleJobs = GUILayout.Toggle(useJobs, "Use Jobs");
        if (toggleJobs != useJobs)
        {
            VATRuntimeSettings.UseJobs = toggleJobs;
        }

        bool burstEnabled = BurstCompiler.Options.EnableBurstCompilation;
        bool forcedBurst = VATRuntimeSettings.UseBurst;
        bool toggleBurst = GUILayout.Toggle(forcedBurst, "Use Burst");
        if (toggleBurst != forcedBurst)
        {
            VATRuntimeSettings.UseBurst = toggleBurst;
            BurstCompiler.Options.EnableBurstCompilation = toggleBurst;
        }

        GUILayout.EndArea();
    }
}
