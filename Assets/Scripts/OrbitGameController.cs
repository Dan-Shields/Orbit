using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class OrbitGameController : MonoBehaviour
{
    public GameObject shipPrefab;

    public GameObject homingEnemyPrefab;
    public GameObject movingEnemyPrefab;
    public GameObject staticEnemyPrefab;

    public GameObject triangleOrbPrefab;
    public GameObject squareOrbPrefab;
    public GameObject circleOrbPrefab;

    public GameObject playerScorePrefab;

    public GameObject countdown;
    public GameObject origin;

    public GameObject canvas;

    public Color[] shipColors = new Color[4];
    public KeyCode[] shipControls = new KeyCode[4];

    public int playerCount = 2;

    public float gameTime = 300.0f;
    private float _timeRemaining;
    private string _stringTimeRemaining;
    private GameState _gameState = GameState.InMenu;
    private TextMeshProUGUI _countdownText;
    private bool _showCountdown = false;

    private GameObject[] _movingEnemies;
    private GameObject[] _staticEnemies;

    private List<Player> _players = new List<Player>(4);

    // Start is called before the first frame update
    void Start()
    {
        _countdownText = countdown.GetComponent<TextMeshProUGUI>();

        StartPregame();
    }

    // Update is called once per frame
    void Update()
    {
        switch (_gameState)
        {
            case GameState.Pregame:
                _timeRemaining = Mathf.Max(_timeRemaining - Time.deltaTime, 0.0f);

                if (_timeRemaining == 0)
                {
                    StartGame();
                }
                break;

            case GameState.Running:
                _timeRemaining = Mathf.Max(_timeRemaining - Time.deltaTime, 0.0f);

                if (_timeRemaining == 0)
                {
                    EndGame();
                }
                break;

            case GameState.Paused:
                break;

            case GameState.Ended:
                break;
        }       

        _stringTimeRemaining = Mathf.Ceil(_timeRemaining).ToString();
        _countdownText.text = _showCountdown ? _stringTimeRemaining : "";
    }

    void StartPregame ()
    {
        _gameState = GameState.Pregame;

        _timeRemaining = 3;
        _showCountdown = true;

        Vector2 originPosition = new Vector2(origin.transform.position.x, origin.transform.position.y);

        for (int i = 0; i < playerCount; i++)
        {
            GameObject ship = Instantiate(shipPrefab, new Vector3(0, 0, -1.0f), Quaternion.identity);

            GameObject playerScore = Instantiate(playerScorePrefab, new Vector3(0,0,0), Quaternion.identity);

            playerScore.transform.SetParent(canvas.transform, false);

            _players.Add(new Player(i, ship, shipColors, shipControls, originPosition, playerScore));
        }
    }

    void StartGame()
    {
        _timeRemaining = gameTime;
        //_showCountdown = false;
        _gameState = GameState.Running;

        foreach (Player player in _players)
        {
            player.shipControl.Enable();
        }
    }

    void EndGame()
    {
        _gameState = GameState.Ended;
    }

    void OnGUI()
    {
        GUI.contentColor = Color.white;
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.black;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        foreach (Player player in _players)
        {
            //GUI.Label(new Rect(10, 25 * player.id, 0, 0), player.score.ToString());
        }
    }
}

public enum GameState
{
    InMenu,
    Pregame,
    Running,
    Paused,
    Ended
}

public class Player
{
    public int id;
    public int position;
    public GameObject ship;
    public ShipControl shipControl;

    public GameObject playerScore;

    public Player(int id, GameObject ship, Color[] colors, KeyCode[] keyCodes, Vector2 originPosition, GameObject playerScore)
    {
        // TODO: shuffle position on start maybe?
        this.id = this.position = id;
        this.ship = ship;

        ship.layer = (int)Layers.Player1 + id;

        shipControl = ship.GetComponent<ShipControl>();

        shipControl.color = colors[this.position];
        shipControl.controlKey = keyCodes[this.position];
        shipControl.ID = id;

        shipControl.Origin = originPosition;

        float spawnPosX = -2.7f * (position % 2 == 0 ? 1.0f : -1.0f);
        float spawnPosY = 2.7f * (position > 1 ? -1.0f : 1.0f);

        shipControl.SetSpawnPoint(new Vector2(spawnPosX, spawnPosY));
        shipControl.Spawn();

        RectTransform playerScoreTransform = playerScore.GetComponent<RectTransform>();

        if (!playerScoreTransform) return;

        playerScoreTransform.anchoredPosition = new Vector2(950.0f * (position % 2 == 0 ? 1.0f : -1.0f), 550.0f * (position > 1 ? -1.0f : 1.0f));

        TextMeshProUGUI playerScoreText = playerScore.GetComponent<TextMeshProUGUI>();
        playerScoreText.color = colors[this.position];

        shipControl.OnScoreUpdated += newScore => playerScoreText.text = newScore.ToString();

        this.playerScore = playerScore;
    }
}
