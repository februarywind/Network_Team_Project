using Firebase.Database;
using Firebase.Extensions;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class LoginPanel1 : MonoBehaviour
{
    [SerializeField] TMP_InputField emaillInput;
    [SerializeField] TMP_InputField passwordInput;
    [SerializeField] VerifyPanel1 verifyPanel;

    [SerializeField] AudioClip titleBgm;

    [SerializeField] Transform winnerEventCameraPosition;

    private void OnEnable()
    {
        // 저장된 이메일 불러오기
        // 저장된 파일이 없다면 기본값(string.Empty)
        GameManager.UserSetting.LoadSetting();
        emaillInput.text = GameManager.UserSetting.Data.email;

        GameManager.Sound.PlayBGM(titleBgm, 0.2f);

        Camera.main.GetComponent<CameraController2>().enabled = false;
        Camera.main.transform.position = winnerEventCameraPosition.position;
        Camera.main.transform.rotation = winnerEventCameraPosition.rotation;
    }

    public void Login()
    {
        PopUp1.Instance.PopUpOpen(true, "로그인 중");

        // 내부 테스트용: 인증 대신 닉네임 사용
        PhotonNetwork.NickName = emaillInput.text;
        GameManager.Backend.UserInitAndDeviceCheck();
        PhotonNetwork.ConnectUsingSettings();
    }

    static 

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(); // 어플리케이션 종료
#endif
    }
}
