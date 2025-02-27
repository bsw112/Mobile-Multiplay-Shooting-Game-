﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System.IO;




public class GameManager : MonoBehaviour
{

    public static GameManager Instance { get; private set; }


    /// <summary>
    /// 로컬 플레이어의 캐릭터 오브젝트
    /// </summary>
    public GameObject localPlayer { get; private set; }
    /// <summary>
    /// 로컬플레이어의 팀을 반환한다. 실패하면 -1을 반환한다.
    /// </summary>
    public int homeTeam = -1;


    //플레이어가 죽을때 이벤트를 초기화하기 위해서 레퍼런스를 가지고 있다.
    [SerializeField] private Joystick moveJoystick;
    [SerializeField] private Joystick attackJoystick;

    [SerializeField]
    private Text countDownText;

    [Header("mathTablePanel 관련")]
    public GameObject matchTablePanel;

    [SerializeField]
    private Sprite[] portraits;

    [SerializeField]
    private GameObject[] A_TeamProfiles;
    private Text[] A_TeamNames = new Text[3];
    private Image[] A_TeamPortraits = new Image[3];

    [SerializeField]
    private GameObject[] B_TeamProfiles;
    private Image[] B_TeamPortraits = new Image[3];
    private Text[] B_TeamNames = new Text[3];

    //A팀과 B팀의 스폰 포인트
    [SerializeField]
    private Transform[] A_spawnPoints;
    [SerializeField]
    private Transform[] B_spawnPoints;


    private PhotonView photonView;
    private ScoreManager scoreMgr;

    private Dictionary<string, Sprite> portraitDic = new Dictionary<string, Sprite>();


    [Header("게임 러닝타임(초)")]
    [SerializeField]
    private float GameTimeLimit;
    public float CurrentGameTime { get; private set; }

    //게임진행 관련 필드
    public int winner { get; private set; }
    public bool isGameEnd { get; private set; }
    public System.Action onGameEnd;
    private bool gotTenCoin;

    //한번만 실행하기 위한 단순한 락
    private bool Lock = false;

    [Header("카운트다운")]
    [SerializeField]
    private float countDownMax = 10;
    private float cached_countDownMax;

    //모든 플레이어가 셋팅되고 게임이 시작되었나?
    public bool isGameStart;



    [Header("AI로 스폰할 캐릭터들")]
    public GameObject[] AICharacters;
    [Header("리스폰 대기시간(초)")]
    public float respawnWait;

    /// <summary>
    /// 플레이어들에 대한 정보
    /// </summary>
    public List<ExitGames.Client.Photon.Hashtable> playerInfos { get; private set; }
    /// <summary>
    /// 플레이어들의 캐릭터 오브젝트에 대한 레퍼런스
    /// </summary>
    public List<GameObject> playerCharacters = new List<GameObject>();
    //인공지능 캐릭터들을 구분하기 위한 아이디(플레이어의 경우 엑터넘버임)
    private int playerID = 100;



    private void Awake()
    {
        if (Instance != null)
        {
            Debug.Log("more than one gameManager");
            return;
        }
        Instance = this;

        playerInfos = new List<ExitGames.Client.Photon.Hashtable>();
    }





    void Start()
    {

        photonView = GetComponent<PhotonView>();
        scoreMgr = ScoreManager.Instance;

        //점수가 바뀔때마다 일어나는 이벤트
        if (scoreMgr != null)
        {
            scoreMgr.onScoreChanged += GotTenCoin;
            scoreMgr.onScoreChanged += CountDown;
        }

        cached_countDownMax = countDownMax;

        //오프라인 테스트용 코드
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            GameObject character = GameObject.FindWithTag("Player");
            localPlayer = character;
        }


        //프로필을 셋팅한다.
        for (int i = 0; i < 3; i++)
        {
            A_TeamPortraits[i] = A_TeamProfiles[i].GetComponentInChildren<Image>();
            A_TeamNames[i] = A_TeamProfiles[i].GetComponentInChildren<Text>();
            B_TeamPortraits[i] = B_TeamProfiles[i].GetComponentInChildren<Image>();
            B_TeamNames[i] = B_TeamProfiles[i].GetComponentInChildren<Text>();
        }


        //캐릭터들을 배치한다.
        InitGame();

    }

    

    [PunRPC]
    void AddPlayerInfoRPC(ExitGames.Client.Photon.Hashtable info)
    {
        playerInfos.Add(info);
    }



    private void Update()
    {


        CurrentGameTime += Time.deltaTime;

        if (CurrentGameTime >= GameTimeLimit)
        {
            isGameEnd = true;
        }

        //테스트용으로 바로 게임클리어하게 하는 장치
        if (Input.GetKeyDown(KeyCode.K))
        {
            isGameEnd = true;
        }


        if (isGameEnd)
        {
            EndGame();
        }

    }

    private void EndGame()
    {
        if (isGameEnd)
        {
            Debug.Log("게임 끝");
            onGameEnd?.Invoke();
            PhotonNetwork.LoadLevel("GameResult");

        }
    }

    private void CountDown()
    {
        //한쪽이라도 10개 이상의 코인을 가지고 있으면 카운트다운.
        if (gotTenCoin)
        {
            StartCoroutine(CountDownCorutine());
            Debug.Log("카운트다운 시작");
        }
    }

    private IEnumerator CountDownCorutine()
    {

        //카운트다운 하는 동안 코인 뺏겨서 10개 이하되면 중단.
        while (countDownMax > 0 && gotTenCoin)
        {
            countDownMax -= Time.deltaTime;
            float minute = Mathf.Round(countDownMax);
            countDownText.text = minute.ToString();

            yield return null;
        }

        //카운트다운이 0보다 작아지면 게임 끝.
        if (countDownMax <= 0)
        {
            if (scoreMgr.ATeamScore > scoreMgr.BTeamScore)
            {
                winner = 0;
            }
            else
                winner = 1;

            Debug.Log("카운트다운 0");
            isGameEnd = true;
        }

        countDownMax = cached_countDownMax;

    }

    private void GotTenCoin()
    {
        if ((scoreMgr.ATeamScore >= 10 || scoreMgr.BTeamScore >= 10) && !(scoreMgr.ATeamScore == scoreMgr.BTeamScore))
        {
            Debug.Log("10개의 이상의 코인 겟");
            gotTenCoin = true;

        }
        else
            gotTenCoin = false;

    }

    private void InitGame()
    {

        Debug.Log("initGame");

        foreach (var portrait in portraits)
        {
            portraitDic.Add(portrait.name, portrait);
        }

        if (PhotonNetwork.IsConnectedAndReady)
        {
            //로컬 플레이어의 커스텀프로퍼티 가져온다
            ExitGames.Client.Photon.Hashtable props = PhotonNetwork.LocalPlayer.CustomProperties;
            int team = (int)props["team"];
            string character = (string)props["character"];
            int spawnIndex = (int)props["spawnIndex"];

            Vector3 spawnPos = team == 0 ? A_spawnPoints[spawnIndex].position : B_spawnPoints[spawnIndex].position;


            //로컬플레이어 인스턴스화
            GameObject go = PhotonNetwork.Instantiate(character, spawnPos, Quaternion.identity);
            //플레이어 아이디 할당
            go.GetComponent<CharacterSetup>().playerID = PhotonNetwork.LocalPlayer.ActorNumber;
            //모든 인스턴스에 팀 정하기
            go.GetComponent<CharacterSetup>().SetTeamRPC(team);
            localPlayer = go;
            homeTeam = team;

            
            ExitGames.Client.Photon.Hashtable info = new ExitGames.Client.Photon.Hashtable() { { "playerID", PhotonNetwork.LocalPlayer.ActorNumber }, { "team", team }, { "character", character },
                                                                                                   { "spawnPos", spawnPos }, { "nickname", PhotonNetwork.LocalPlayer.NickName } };

            //로컬플레이어 정보 저장
            photonView.RPC("AddPlayerInfoRPC", RpcTarget.AllBuffered, info);






            //마스터클라이언트에서 AI를 인스턴스화한다.
            if (PhotonNetwork.IsMasterClient)
            {
                //AI를 인스턴스화한다.

                //AI가 스폰할 트랜스폼을 정할 인덱스
                int AI_A_spawnIndex = 0;
                int AI_B_spawnIndex = 0;

                //AI의 스폰포인트를 잡기 위해 현재 사용된 스폰포인트를 뛰어넘는다.
                foreach (Player player in PhotonNetwork.PlayerList)
                {
                    ExitGames.Client.Photon.Hashtable prop = player.CustomProperties;
                    if ((int)prop["team"] == 0)
                        AI_A_spawnIndex++;
                    else
                        AI_B_spawnIndex++;
                }


                //인스턴스화할 AI 개수
                int AICount = PhotonNetwork.CurrentRoom.MaxPlayers - PhotonNetwork.CurrentRoom.PlayerCount;
                //처음으로 인스턴스할 AI의 팀
                int AITeam = (PhotonNetwork.CurrentRoom.PlayerCount % 2) == 0 ? 0 : 1;


                for (int i = 0; i < AICount; i++)
                {
                    if (AITeam > 1)
                        AITeam = 0;

                    Vector3 AISpawnPos = AITeam == 0 ? A_spawnPoints[AI_A_spawnIndex++].position : B_spawnPoints[AI_B_spawnIndex++].position;

                    int random = Random.Range(0, AICharacters.Length);
                    GameObject AIgo = PhotonNetwork.Instantiate(AICharacters[random].name, AISpawnPos, Quaternion.identity);
                    AIgo.GetComponent<CharacterSetup>().playerID = playerID;
                    AIgo.GetComponent<CharacterSetup>().SetTeamRPC(AITeam);
                    ExitGames.Client.Photon.Hashtable AIInfo = new ExitGames.Client.Photon.Hashtable() { { "playerID", playerID++ }, { "team", AITeam }, { "character", AICharacters[random].name },
                                                                                                   { "spawnPos", AISpawnPos }, { "nickname", "AI" + i } };

                    photonView.RPC("AddPlayerInfoRPC", RpcTarget.AllBuffered, AIInfo);


                    AITeam++;
                }
            }


            SetProfile();
        }
        else
        {
            Debug.Log("not ready - offline");
        }

        isGameStart = true;

        //모든 플레이어에 대한 레퍼런스를 모은다.
        GameObject[] characters = GameObject.FindGameObjectsWithTag("Player");
        foreach (var character in characters)
        {
            playerCharacters.Add(character);
            CharacterStats stats = character.GetComponent<CharacterStats>();
            //캐릭터가 죽을때 캐릭터 목록에서 삭제하도록한다.
            stats.onPlayerDie += () => { playerCharacters.Remove(character); };
            //죽을때 respawnWait만큼 기다리고 리스폰하도록 한다.
            stats.onPlayerDie += () => Respawn(character.GetComponent<CharacterSetup>().playerID, respawnWait);

        }

        //3초후 게임시작하는 걸 가정함.
        Destroy(matchTablePanel, 3f);



    }



    //매치테이블을 셋팅
    private void SetProfile()
    {

        //프로필의 텍스트와 이미지를 지정할 인덱스
        int index = 0;

        foreach (var info in playerInfos)
        {

            string character = (string)info["character"];
            int team = (int)info["team"];

            //프로필 슬롯이 팀당 3개므로 인덱스가 2를 넘어가면 안됨
            if (index > 2)
            {
                index = 0;
            }

            Text[] nameTexts = team == 0 ? A_TeamNames : B_TeamNames;
            Image[] portraits = team == 0 ? A_TeamPortraits : B_TeamPortraits;

            nameTexts[index].text = (string)info["nicknam"];
            portraits[index].sprite = portraitDic[character.Replace("AI", "")];
            index++;
        }






    }

    /// <summary>
    /// 리스폰한다.
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="delay">초단위</param>
    public void Respawn(int playerID, float delay = 0f)
    {
        StartCoroutine(RespawnCorutine(playerID, delay));
    }

    private IEnumerator RespawnCorutine(int playerID, float delay)
    {

        int order = 0;
        foreach(var info in playerInfos)
        {
            Debug.Log($"playerinfos{order++}: " + info.ToString());
        }

        yield return new WaitForSeconds(delay);


        //리스폰 되는 것이 로컬플레이어면 조이스틱의 이벤트를 초기화시켜준다.
        if(playerID == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            moveJoystick.onPointerDown = null;
            moveJoystick.onPointerUp = null;
            attackJoystick.onPointerUp = null;
            attackJoystick.onPointerDown = null;
        }



        foreach (var info in playerInfos)
        {

            

            int ID = (int)info["playerID"];
            string character = (string)info["character"];
            Vector3 spawnPos = (Vector3)info["spawnPos"];
            int team = (int)info["team"];

            if (ID == playerID)
            {
                Debug.Log(character + "를 리스폰합니다.");
                GameObject newCharacter = PhotonNetwork.Instantiate(character, spawnPos, Quaternion.identity);
                //캐릭터 목록에 추가한다.
                playerCharacters.Add(newCharacter);
                //캐릭터가 죽을때 캐릭터목록에서 삭제하도록한다.
                CharacterStats stats = newCharacter.GetComponent<CharacterStats>();
                stats.onPlayerDie += () => { playerCharacters.Remove(newCharacter); };
                //캐릭터의 플레이어 ID를 유지한다.
                //캐릭터의 팀도 유지한다.
                CharacterSetup setup = newCharacter.GetComponent<CharacterSetup>();
                setup.playerID = playerID;
                setup.Team = team;
                //죽으면 리스폰
                stats.onPlayerDie += () => Respawn(newCharacter.GetComponent<CharacterSetup>().playerID, respawnWait);

                yield break;



            }
        }



    }

}



