﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[System.Serializable]
public class Player
{
    public Player()
    {
        score = 0;
    }

    public GameObject gameObject;
    public GameObject camera;
    public GameObject UIMask;
    public int score;
    public int regularKills;
    public int chargerKills;
    public int spitterKills;
}

public class GameObjectManager : MonoBehaviour
{
    static GameObjectManager inst;
    public static GameObjectManager instance
    {
        get
        {
            return inst;
        }
        private set
        {
            inst = value;
        }
    }

    public GameObject cameraPrefab;
    public GameObject playerPrefab;
    public GameObject HUDPrefab;
    public GameObject WinGUIPrefab;
    public GameObject LoseGUIPrefab;

    [HideInInspector] public GameObject endPos;
    [HideInInspector] public GameObject civilianDestination;
    [HideInInspector] public GameObject HUD;
    public List<GameObject> playerStarts;
    public List<Player> players;
    public List<GameObject> enemySpawners;
    public List<GameObject> enemies;
    public List<GameObject> civilianSpawners;
    public List<GameObject> civilians;
    public List<GameObject> barriers;
    public List<GameObject> vitalBarriers;
    public List<GameObject> turrets;
    public List<GameObject> questItemSpawnLocs;

    [Tooltip("This is a percentage")][Range(0, 100)]
    public float civiliansRequired;
    [Tooltip("Time in seconds until certain bulkhead doors open")]
    public float time;

    [HideInInspector] public float timer;
    [HideInInspector] public int initialCivilians;
    [HideInInspector] public int civiliansEscaped;
    [HideInInspector] public int playersEscaped;

    [HideInInspector] public bool playing;

    public bool trySplitScreen = false;

    void Awake()
    {
        playing = false;
    }

    void OnEnable()
    {
        timer = time;
        civiliansEscaped = 0;

        playerStarts = new List<GameObject>(GameObject.FindGameObjectsWithTag("Player Start"));
        SpawnPlayers();
        GetEnemySpawners();
        GetCivilianSpawners();
        GetCivilians();
        GetBarriers();
        SpawnHUD();
        GetTurrets();
        GetCivilianDestination();
        GetQuestItemSpawnLocations();
        GetEndPos();
        inst = this;

        StartCoroutine(GetInitialCivilians());

        playing = true;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            foreach (GameObject barricade in vitalBarriers)
            {
                barricade.GetComponent<BulkheadLogic>().Open();
            }
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] == null)
            {
                enemies.RemoveAt(i);
            }
        }

        int playerCount = 0;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].gameObject != null && !players[i].gameObject.GetComponent<ReviveSystem>().NeedRes) // When player 2 is dead, player 1 doesn't pass the second test
                playerCount++;
        }

        if (playerCount <= 0)
        {
            GameOver();
        }
    }

    IEnumerator GetInitialCivilians()
    {
        yield return new WaitForSeconds(1);
        initialCivilians = civilians.Count;
        foreach (GameObject spawner in civilianSpawners)
        {
            if (spawner.GetComponent<CivilianSource>() != null)
                initialCivilians += spawner.GetComponent<CivilianSource>().civilians;
        }
    }

    public void GetQuestItemSpawnLocations()
    {
        questItemSpawnLocs = new List<GameObject>(GameObject.FindGameObjectsWithTag("QuestItemSpawn"));
    }

    public void SpawnPlayers()
    {
        int playerCount = GameManager.instance.playerCount;

        players = new List<Player>();
                
        // CameraLogic cameraScript = camera.GetComponent<CameraLogic>();
        
        if (playerStarts.Count != 0)
        {
            for (int i = 0; i < playerCount; i++)
            {
                Player newPlayer = new Player();

                int playerStartIndex = i % playerStarts.Count;

                GameObject playerInstance = Instantiate(playerPrefab,
                                                        playerStarts[playerStartIndex].transform.position,
                                                        playerStarts[playerStartIndex].transform.rotation);

                playerInstance.GetComponent<PlayerMovScript>().playerNumber = i + 1;
                playerInstance.GetComponent<PlayerInfoScript>().PlayerNo = i + 1;

                if (i == 0)
                    playerInstance.GetComponent<LineRenderer>().startColor = new Color(0.08f, 0.68f, 0.74f, 1);
                else if (i == 1)
                    playerInstance.GetComponent<LineRenderer>().startColor = new Color(0.74f, 0.08f, 0.36f, 1);
                else if (i == 2)
                    playerInstance.GetComponent<LineRenderer>().startColor = new Color(1, 0, 1, 1);
                else if (i == 3)
                    playerInstance.GetComponent<LineRenderer>().startColor = new Color(0, 1, 1, 1);
                
                newPlayer.gameObject = playerInstance;
                
                GameObject camera = Instantiate(cameraPrefab);

                camera.GetComponent<Camera>().rect = new Rect
                                                     (
                                                         i / (float)playerCount,
                                                         0,
                                                         1 / (float)playerCount,
                                                         1
                                                     );

                if (i > 0)
                {
                    camera.GetComponent<AudioListener>().enabled = false;
                }
                camera.GetComponent<CameraMovement>().player = newPlayer.gameObject;
                newPlayer.gameObject.GetComponent<PlayerMovScript>().camera = camera.GetComponent<Camera>();

                newPlayer.camera = camera;

                newPlayer.gameObject.GetComponent<PlayerMovScript>().useController = !GameManager.instance.UsingKbrd;

                players.Add(newPlayer);
            }
        }
        else
            Debug.Log("No player starts found");
    }

    public void GetEnemySpawners()
    {
        enemySpawners = new List<GameObject>(GameObject.FindGameObjectsWithTag("Enemy Spawner"));
        foreach (GameObject enemySpawner in enemySpawners)
        {
            enemySpawner.GetComponent<EnemySpawner>().gameObjectManager = this;
        }
    }

    public void GetCivilianSpawners()
    {
        List<GameObject> localCivilianSpawners = new List<GameObject>(GameObject.FindGameObjectsWithTag("Civilian Spawner"));
        foreach (GameObject civilianSpawner in localCivilianSpawners)
        {
            if (civilianSpawner.GetComponent<CivilianSource>() != null)
                civilianSpawners.Add(civilianSpawner);
        }
    }

    void GetBarriers()
    {
        barriers = new List<GameObject>(GameObject.FindGameObjectsWithTag("Barricade"));
        vitalBarriers = new List<GameObject>();
        foreach (GameObject barricade in barriers)
        {
            if (barricade.GetComponent<BarrierLogic>() != null)
            {
                if (barricade.GetComponent<BarrierLogic>().vital)
                    vitalBarriers.Add(barricade);
            }
        }
        for (int i = 0; i < barriers.Count; i++)
        {
            if (barriers[i].GetComponent<BarrierLogic>() == null)
            {
                barriers.RemoveAt(i);
                i--;
            }
        }
    }

    void GetCivilians()
    {
        civilians = new List<GameObject>(GameObject.FindGameObjectsWithTag("Civilian"));
    }

    void GetEndPos()
    {
        endPos = GameObject.FindGameObjectWithTag("End Position");
    }

    void GetCivilianDestination()
    {
        civilianDestination = GameObject.FindGameObjectWithTag("CivilianDestination");
    }

    public void SpawnHUD()
    {
        HUD = Instantiate(HUDPrefab);
    }

    public void GetTurrets()
    {
        turrets = new List<GameObject>(GameObject.FindGameObjectsWithTag("Turret"));
    }

    public void CheckCivilianCount()
    {
        int totalCivilians = civiliansEscaped;
        totalCivilians += civilians.Count;
        for (int i = 0; i < civilianSpawners.Count; i++)
            totalCivilians += civilianSpawners[i].GetComponent<CivilianSource>().currentCivilians;

        if (totalCivilians < initialCivilians * (civiliansRequired / 100))
        {
            Debug.Log("Game Over: Too many civilians died, needed " + (initialCivilians * (civiliansRequired / 100)).ToString() + "/" + initialCivilians.ToString());
            Lose();
        }
    }

    public bool GetWin()
    {
        if (playersEscaped < 1)
        {
            Debug.Log("Game Over: No Alive/Escaped Players");
            return false;
        }

        if (civiliansEscaped < initialCivilians * (civiliansRequired / 100))
        {
            Debug.Log("Game Over: Not enough escaped civilians");
            return false;
        }

        return true;
    }

    public void GameOver()
    {
        if (playing)
        {
            if (GetWin())
                Instantiate(WinGUIPrefab);
            else
                Instantiate(LoseGUIPrefab);

            playing = false;
        }
    }

    public void Lose()
    {
        Instantiate(LoseGUIPrefab);
        playing = false;
    }

    public Player GetPlayer(GameObject player)
    {
        for (int i = 0; i < players.Count; i++)
            if (players[i].gameObject == player)
                return players[i];
        return null;
    }
}
