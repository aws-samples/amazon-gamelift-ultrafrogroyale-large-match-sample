using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public Toggle tglNetworkHUD;

    // Start is called before the first frame update
    void Start()
    {
        // For a headless server, immediately load the game scene
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            StartCoroutine(LoadGameScene());
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void JoinMatch()
    {
        PlayerPrefs.SetInt("ShowUnityHUD", tglNetworkHUD.isOn ? 1:0);
        StartCoroutine(LoadGameScene());
    }

    private IEnumerator LoadGameScene()
    {
        var asyncload = SceneManager.LoadSceneAsync("Game");
        while(!asyncload.isDone)
        {
            yield return null;
        }
    }
}
