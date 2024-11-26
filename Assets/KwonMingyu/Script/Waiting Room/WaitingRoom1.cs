using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

public class WaitingRoom1 : MonoBehaviourPunCallbacks
{
    [SerializeField] PlayerCard1[] playerCards;
    [SerializeField] TMP_Text countdownText;

    private Coroutine GameStartCounterCoroutine;

    // 게임 승리 점수 설정 버튼 호스트만
    [SerializeField] GameObject roomSettingButtons;

    [SerializeField] Transform porce;
    [SerializeField] float power;
    [SerializeField] TMP_Text winnerText;

    private bool winnerEvent;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Player localPlayer = PhotonNetwork.LocalPlayer;
            localPlayer.SetReady(!localPlayer.GetReady());
        }
    }

    // 플레이어의 정보가 업데이트 될 때 (플레이어의 Room number가 지정될 때)
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // 룸 넘버가 할당되지 않았다면 리턴
        if (targetPlayer.GetPlayerNumber() == -1) return;

        // 플레이어의 컬러가 기본값이라면 PlayerNuber를 부여
        if (targetPlayer.GetColorNumber() == -1)
        {
            targetPlayer.SetColorNumber(targetPlayer.GetPlayerNumber());
            return;
        }

        // 플레이어 카드 업데이트
        if (!winnerEvent)
        {
            UpdatePlayerCards();
        }

        roomSettingButtons.SetActive(PhotonNetwork.LocalPlayer.IsMasterClient);

        // 모든 플레이어가 Ready && 플레이어가 2명 이상일 때 게임 시작 카운트다운
        if (CheckAllReady() && PhotonNetwork.CurrentRoom.Players.Count > 1)
        {
            if (GameStartCounterCoroutine != null) return;
            GameStartCounterCoroutine = StartCoroutine(StartCountDownCoroutine());
        }
    }

    // 플레이어가 나가면 카드 업데이트
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerCards();
    }
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        roomSettingButtons.SetActive(PhotonNetwork.LocalPlayer.IsMasterClient);
    }
    public void UpdatePlayerCards()
    {
        // 방 최대 인원수 만큼 카드를 활성화 후 리셋
        for (int i = 0; i < PhotonNetwork.CurrentRoom.MaxPlayers; i++)
        {
            playerCards[i].gameObject.SetActive(true);
            playerCards[i].CardInfoReset();
        }

        // 플레이어 룸 넘버의 카드에 정보를 입력
        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            playerCards[player.GetPlayerNumber()].CardInfoCanger(player);
        }

        // 색갈 업데이트
        PlayerColorSet();
    }
    private void PlayerColorSet()
    {
        // 모든 포톤뷰를 순회 하면서 소유자의 색갈로 변경
        foreach (PhotonView photonView in FindObjectsOfType<PhotonView>())
        {
            Renderer renderer = photonView.gameObject.GetComponentInChildren<Renderer>();
            if (renderer == null) continue;
            renderer.material.color = photonView.Owner.GetNumberColor();
        }
        // 카드의 아웃라인 색갈을 플레이어 색갈로 변경
        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            playerCards[player.GetPlayerNumber()].CardOutLineSet(player.GetNumberColor());
        }

    }
    IEnumerator StartCountDownCoroutine()
    {
        YieldInstruction waitCountDown = new WaitForSeconds(1f);
        countdownText.gameObject.SetActive(true);
        for (int i = 0; i < 5; i++)
        {
            // 플레이어가 레디를 풀거나 인원이 2명보다 적으면 카운트다운 중지
            if (!CheckAllReady() || PhotonNetwork.CurrentRoom.Players.Count < 2)
            {
                countdownText.gameObject.SetActive(false);
                StopCoroutine(GameStartCounterCoroutine);
                GameStartCounterCoroutine = null;
            }
            countdownText.text = (5 - i).ToString();
            yield return waitCountDown;
        }
        countdownText.gameObject.SetActive(false);
        GameStart();
    }

    private bool CheckAllReady()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
            if (!player.GetReady()) return false;
        return true;
    }
    public void GameStart()
    {
        Debug.Log("GameStart");

        // 플레이어 프로퍼티 초기화
        PhotonNetwork.LocalPlayer.SetReady(false);
        PhotonNetwork.LocalPlayer.SetWinningPoint(0);

        if (!PhotonNetwork.LocalPlayer.IsMasterClient) return;

        PhotonNetwork.LoadLevel(4);
    }

    public IEnumerator WinnerEventCoroutine()
    {
        GameObject winner = null;
        Color winnerColor = Color.white;
        winnerEvent = true;

        // 플레이어를 생성하고 Load 완료
        GameObject instance = PhotonNetwork.Instantiate("Character2", new Vector3(PhotonNetwork.LocalPlayer.GetPlayerNumber(), 1, 0), Quaternion.identity);
        instance.GetComponent<PlayerCharacterControl2>().enabled = false;

        PhotonNetwork.LocalPlayer.SetLoad(true);

        // 모든 플레이어 Load 대기
        while (!AllLoad())
            yield return null;

        // 플레이어 카드 안보이게 하기
        foreach (var item in playerCards)
            item.gameObject.SetActive(false);

        yield return new WaitForSeconds(1f);

        // 플레이어 캐릭터를 순회
        foreach (PhotonView photonView in FindObjectsOfType<PhotonView>())
        {
            // 플레이어의 점수가 우승 점수가 아니라면 창 밖으로 날려버림
            if (photonView.Owner.GetWinningPoint() != PhotonNetwork.CurrentRoom.GetGoalPoint())
            {
                photonView.GetComponent<Rigidbody>().velocity = (porce.position - photonView.transform.position).normalized * power;
                yield return new WaitForSeconds(0.3f);
                continue;
            }
            // 우승자 정보를 저장
            winner = photonView.gameObject;
            winnerColor = photonView.Owner.GetNumberColor();
            winnerText.text = $"우승자\n{photonView.Owner.NickName}";
            BackendManager1.Instance.PlayerLevelUp();
            photonView.gameObject.GetComponent<PlayerCharacterControl2>().enabled = true;
        }
        // 날라가는거 구경시간
        yield return new WaitForSeconds(2f);

        // 메인 카메라의 타겟을 우승자로
        Camera.main.GetComponent<CameraController2>().enabled = true;
        Camera.main.GetComponent<CameraController2>().Target = winner.transform;

        // 우승 텍스트 출력
        winnerText.color = winnerColor;
        winnerText.gameObject.SetActive(true);

        // 우승자 구경 시간
        yield return new WaitForSeconds(5f);
        winnerText.gameObject.SetActive(false);

        // 날라간 플레이어 돌아와
        instance.GetComponent<Rigidbody>().velocity = Vector3.zero;
        instance.transform.position = Vector3.up;
        Camera.main.GetComponent<CameraController2>().Target = instance.transform;
        instance.GetComponent<PlayerCharacterControl2>().enabled = true;

        winnerEvent = false;

        // 플레이어 카드 업데이트
        UpdatePlayerCards();
    }
    private bool AllLoad()
    {
        foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (!player.GetLoad()) return false;
        }
        return true;
    }
}
