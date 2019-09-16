using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : NetworkBehaviour
{
    public GameNetworkManager networkManager;

    private class GameOverStat
    {
        public string playerName;
        public float size;
        public int place;
    }
    private List<GameOverStat> stats = new List<GameOverStat>();

    private List<GamePlayerController> playerList = new List<GamePlayerController>();

    private List<string> adjectives = new List<string>
    {
        "Groovy",
        "Crazy-legs",
        "Very Tactful",
        "Creepy",
        "Fluffy",
        "Zippy",
        "Fiercely Loyal",
        "Magical",
        "Metal",
        "Offensive",
        "Slippery",
        "Arrogant",
        "Angry",
        "Adamant",
        "Bellicose",
        "Caustic",
        "Endemic",
        "Hubristic",
        "Incendiary",
        "Loquacious",
        "Noxious",
        "Petulant",
        "Taciturn",
        "Turgid",
        "Voracious",
        "Zealous",
        "Angelic",
        "Crafty",
        "Mad Dog",
        "Admiral",
        "Cannonball",
        "Captain",
        "Jolly",
        "Pork Chop",
        "Spud",
        "Funky"
    };

    private List<string> names = new List<string>
    {
        "Alejandro",
        "Ana",
        "Arnav",
        "Carlos",
        "Diego",
        "Jane",
        "John",
        "Jorge",
        "Li Juan",
        "Liu Jie",
        "Márcia",
        "María",
        "Martha",
        "Mary",
        "Mateo",
        "Nikhil",
        "Paulo",
        "Richard",
        "Saanvi",
        "Shirley",
        "Sofía",
        "Xiulan",
        "Zhang"
     };

    private int currentAdjective = 0;
    private int currentName = 0;

    // Start is called before the first frame update
    void Start()
    {
        Random.InitState((int)System.DateTime.Now.Ticks);
        currentAdjective = Random.Range(0, adjectives.Count - 1);
        currentName = Random.Range(0, names.Count - 1);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddPlayer(GamePlayerController playerController)
    {
        playerList.Add(playerController);
    }

    public int RemovePlayer(GamePlayerController playerController)
    {
        var stat = new GameOverStat();
        stat.playerName = playerController.GetPlayerName();
        stat.size = playerController.GetPlayerSize();
        stat.place = playerList.Count;
        stats.Add(stat);
        playerList.Remove(playerController);
        // Since the leaderboard is only updated when a player is removed
        // detect when the last player is standing and add to the leaderboard
        if (playerList.Count == 1)
        {
            var winner = playerList[0];
            stat = new GameOverStat();
            stat.playerName = winner.GetPlayerName();
            stat.size = winner.GetPlayerSize();
            stat.place = 1;
            stats.Add(stat);
            // last player standing means the game is won and the session should terminate
            networkManager.TerminateSession();
        }
        return playerList.Count;
    }

    public string GetName()
    {
        string name = adjectives[currentAdjective] + " " + names[currentName];
        ++currentAdjective;
        if(currentAdjective >= adjectives.Count)
        {
            currentAdjective = 0;
        }
        ++currentName;
        if(currentName >= names.Count)
        {
            currentName = 0;
        }
        return name;
    }

    public string GetLeaderboard()
    {
        string lb = "";

        int wrap = 0;
        foreach (GameOverStat stat in stats)
        {
            lb += $"Place: {stat.place}\tName: {stat.playerName}\tSize: {stat.size}";
            ++wrap;
            if(wrap > 3)
            {
                lb += "\n";
            }
            else
            {
                lb += "\t\t\t";
            }
        }

        return lb;
    }

}
