using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System;

using Aws.GameLift.Server;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Amazon.CognitoIdentity;
using Amazon;

[System.Serializable]
public class InnerObject
{
    public string IpAddress;
    public string Port;
}

[System.Serializable]
public class ConnectionObject
{
    public InnerObject GameSessionConnectionInfo;
}

public class GameNetworkManager : NetworkManager
{
    public GameManager gameManager;
    public UIController uiController;

    private bool isHeadlessServer = false;
    private bool isGameliftServer = false;
    private System.Timers.Timer timer = new System.Timers.Timer(120000);

    private static int LISTEN_PORT = 7777;

    private static int MAX_PLAYERS = 80;

    private void Awake()
    {
    }

    void Start()
    {
        // required to initialize the AWS Mobile SDK
        UnityInitializer.AttachToGameObject(this.gameObject);

        // tells UNET we want more than the default 8 players
        maxConnections = MAX_PLAYERS;

        // detect headless server mode
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            Debug.Log("** SERVER MODE **");
            isHeadlessServer = true;
            SetupServerAndGamelift();
        }
        else
        {
            SetupClient();
        }
    }

    void Update()
    {
    }

    private void OnApplicationQuit()
    {
        if (isHeadlessServer)
        {
            TerminateSession();
        }
    }

    // This is called on the server when a client disconnects
    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);
        // Notify the game controller that a player disconnected
        foreach (PlayerController player in conn.playerControllers)
        {
            GamePlayerController playerController = player.gameObject.GetComponent<GamePlayerController>();
            if (playerController)
            {
                gameManager.RemovePlayer(playerController);
            }
        }
        QuitIfNoPlayers();
    }

    private void QuitIfNoPlayers()
    {
        // if no players are playing the game now terminate the server process
        if (numPlayers <= 0 && isHeadlessServer)
        {
            TerminateSession();
        }
    }

    // should be called when the server determines the game is over
    // and needs to signal Gamelift to terminate this instance
    public void TerminateSession()
    {
        Debug.Log("** TerminateSession Requested **");
        if (isGameliftServer)
        {
            GameLiftServerAPI.TerminateGameSession();
            GameLiftServerAPI.ProcessEnding();
        }
        Debug.Log("** Process Exit **");
        Application.Quit();
    }

    private void SetupClient()
    {
        // in debug mode don't attempt to match with GameLift
        if (PlayerPrefs.GetInt("ShowUnityHUD", 1) == 0)
        {
            // TODO should set text in UIController while finding match
            FindMatch();
        }
    }

    private void FindMatch()
    {
        Debug.Log("Reaching out to client service Lambda function");

        AWSConfigs.AWSRegion = "us-east-1"; // Your region here
        AWSConfigs.HttpClient = AWSConfigs.HttpClientOption.UnityWebRequest;
        // paste this in from the Amazon Cognito Identity Pool console
        CognitoAWSCredentials credentials = new CognitoAWSCredentials(
            "us-east-1:a70f5010-a4c4-45a7-ba01-8e3d0cc08a9d", // Your identity pool ID here
            RegionEndpoint.USEast1 // Your region here
        );

        // This is a bit of a hack building the JSON by hand as Unity doesn't serialize dictionaries to JSON currently
        // Also as the demo has only one region, we hard code a ping time we get connected to the region
        // In production code this should build a map of AWS region names and their ping times so the matchmaker
        // can find the best region for your customer. 
        // Also the player skill is hard coded, to make the sample more clear. It would be better to maintain
        // a database of player info that includes the player skill level that the client service reads
        // directly
        string matchParams = "{\"latencyMap\":{\"us-east-1\":60}, \"playerSkill\":10}";

        AmazonLambdaClient client = new AmazonLambdaClient(credentials, RegionEndpoint.USEast1);
        InvokeRequest request = new InvokeRequest
        {
            FunctionName = "ConnectUltraFrogRoyaleClient",
            InvocationType = InvocationType.RequestResponse,
            Payload = matchParams
        };

        uiController.ShowStatusText();
        uiController.SetStatusText("Finding match, please wait");

        client.InvokeAsync(request,
            (response) =>
            {
                if (response.Exception == null)
                {
                    if (response.Response.StatusCode == 200)
                    {
                        var payload = Encoding.ASCII.GetString(response.Response.Payload.ToArray()) + "\n";
                        var connectionObj = JsonUtility.FromJson<ConnectionObject>(payload);

                        if (connectionObj.GameSessionConnectionInfo.Port == null)
                        {
                            Debug.Log($"Error in Lambda assume matchmaking failed: {payload}");
                            uiController.SetStatusText("Matchmaking failed");
                        }
                        else
                        {
                            uiController.HideStatusText();
                            Debug.Log($"Connecting! IP Address: {connectionObj.GameSessionConnectionInfo.IpAddress} Port: {connectionObj.GameSessionConnectionInfo.Port}");
                            networkAddress = connectionObj.GameSessionConnectionInfo.IpAddress;
                            networkPort = Int32.Parse(connectionObj.GameSessionConnectionInfo.Port);
                            StartClient();
                        }
                    }
                }
                else
                {
                    Debug.LogError(response.Exception);
                    uiController.SetStatusText($"Client service failed: {response.Exception}");
                }
            });
    }

    private void CheckPlayersJoined(System.Object source, System.Timers.ElapsedEventArgs e)
    {
        QuitIfNoPlayers();
    }

    private void SetupServerAndGamelift()
    {
        // start the unet server
        networkPort = LISTEN_PORT;
        StartServer();
        print($"Server listening on port {networkPort}");

        // initialize GameLift
        print("Starting GameLift initialization.");
        var initSDKOutcome = GameLiftServerAPI.InitSDK();
        if(initSDKOutcome.Success)
        {
            isGameliftServer = true;
            var processParams = new ProcessParameters(
                (gameSession) =>
                {
                    // onStartGameSession callback
                    GameLiftServerAPI.ActivateGameSession();
                    // quit if no player joined within two minutes
                    timer.Elapsed += this.CheckPlayersJoined;
                    timer.AutoReset = false;
                    timer.Start();
                },
                (updateGameSession) =>
                {

                },
                () =>
                {
                    // onProcessTerminate callback
                    TerminateSession();
                },
                () =>
                {
                    // healthCheck callback
                    return true;
                },
                LISTEN_PORT,
                new LogParameters(new List<string>()
                {
                    "/local/game/logs/myserver.log"
                })
            );
            var processReadyOutcome = GameLiftServerAPI.ProcessReady(processParams);
            if(processReadyOutcome.Success)
            {
                print("GameLift process ready.");
            }
            else
            {
                print($"GameLift: Process ready failure - {processReadyOutcome.Error.ToString()}.");
            }
        }
        else
        {
            print($"GameLift: InitSDK failure - {initSDKOutcome.Error.ToString()}.");
        }
    }
}

