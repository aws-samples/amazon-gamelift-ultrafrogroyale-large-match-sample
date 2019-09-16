using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    public Text txtStatus;
    public Image leaderboardBackground;
    public Text txtLeaderboard;

    public GameNetworkManager gameNetworkManager;

    // Start is called before the first frame update
    void Start()
    {
//        HideStatusText();
        HideLeaderboard();

        var netHUD = gameNetworkManager.GetComponent<NetworkManagerHUD>();
        // default to 1 so if the scene is started in editor, debug HUD shows
        netHUD.showGUI = PlayerPrefs.GetInt("ShowUnityHUD", 1) == 1;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnDisable()
    {
        var netHUD = gameNetworkManager.GetComponent<NetworkManagerHUD>();
        netHUD.showGUI = false;
    }

    public void ShowStatusText()
    {
        txtStatus.gameObject.SetActive(true);
    }

    public void HideStatusText()
    {
        txtStatus.gameObject.SetActive(false);
    }

    public void SetStatusText(string newText)
    {
        txtStatus.text = newText;
    }

    public void ShowLeaderboard()
    {
        leaderboardBackground.gameObject.SetActive(true);
    }

    public void HideLeaderboard()
    {
        leaderboardBackground.gameObject.SetActive(false);
    }

    public void SetLeaderboardText(string newText)
    {
        txtLeaderboard.text = newText;
    }

    public void QuitToMainMenu()
    {
        gameNetworkManager.StopClient();
        
        StartCoroutine(LoadMenuScene());
    }

    private IEnumerator LoadMenuScene()
    {
        var asyncload = SceneManager.LoadSceneAsync("MainMenu");
        while (!asyncload.isDone)
        {
            yield return null;
        }
    }
}
