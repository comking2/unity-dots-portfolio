using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject mEnemy;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (mEnemy != null)
        {
            for (int i = 0; i < 100000; i++)
            {
                Instantiate(mEnemy);
            }
            // for (int i = 0; i < 10000; i++)
            // {
            //     GameObject.CreatePrimitive(PrimitiveType.Cube);
            // }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    //우측상단에 프레임 표시
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 100, 20), "FPS: " + (1.0f / Time.deltaTime).ToString("F2"));
    }
}
