using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Photon.Pun;
using System.Threading.Tasks;
using UnityEngine;

public enum UserDatas1
{
    name, level
}

public class UserData1
{
    public string name;
    public long level;
}

public class BackendManager1 : MonoBehaviour
{
    public static BackendManager1 Instance { get; private set; }

    private FirebaseApp app;
    public static FirebaseApp App => Instance.app;

    private FirebaseAuth auth;
    public static FirebaseAuth Auth => Instance.auth;

    private FirebaseDatabase database;
    public static FirebaseDatabase Database => Instance.database;

    public DatabaseReference userUidDataRef;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                app = FirebaseApp.DefaultInstance;
                auth = FirebaseAuth.DefaultInstance;
                database = FirebaseDatabase.DefaultInstance;
                Debug.Log("파이어베이스 연결 성공");
            }
            else
            {
                Debug.LogError($"파이어베이스 연결 실패: {task.Result}");
                app = null;
                auth = null;
                database = null;
            }
        });
    }
    /// <summary>
    /// 유저 DB에서 정보를 가져오는 함수
    /// 지금은 포톤 닉네임 설정에 사용된다.
    /// </summary>
    public async Task<UserData1> GetPlayerData()
    {
        UserData1 userData = new();
        await userUidDataRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogWarning("값 가져오기 취소/실패 됨");
                userData = null;
                return;
            }

            if (task.Result.Child("name").Value == null)
            {
                userData.name = $"Player {Random.Range(1000, 9999)}";
                userData.level = 1;

                string json = JsonUtility.ToJson(userData);
                userUidDataRef.SetRawJsonValueAsync(json).ContinueWithOnMainThread(x => x);
            }
            else
            {
                userData.name = task.Result.Child("name").Value.ToString();
                userData.level = (long)task.Result.Child("level").Value;
            }
        });
        return userData;
    }
    /// <summary>
    /// 유저 DB name, Photon NickName 변경 함수.
    /// </summary>
    public async Task<bool> SetPlayerName(string name)
    {
        bool success = true;
        await userUidDataRef.Child("name").SetValueAsync(name).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogWarning("이름 변경 취소/실패 됨");
                success = false;
            }
        });
        if (!success) return false;
        Debug.Log($"닉네임 변경 성공{PhotonNetwork.LocalPlayer.NickName} => {name}");
        PhotonNetwork.LocalPlayer.NickName = name;
        return true;
    }

    // 레벨업 함수, 게임 종료시 우승자에게 호출됨
    public async void PlayerLevelUp()
    {
        await userUidDataRef.Child("level").SetValueAsync(PhotonNetwork.LocalPlayer.GetLevel() + 1).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogWarning("레벨 변경 취소/실패 됨");
            }
        });
        PhotonNetwork.LocalPlayer.SetLevel(PhotonNetwork.LocalPlayer.GetLevel() + 1);
        Debug.Log($"레벨 변경 성공");
    }

    public void UserInitAndDeviceCheck()
    {
        // 내부 테스트용: UID 대신 닉네임 사용
        userUidDataRef = Database.RootReference.Child("UserData").Child(PhotonNetwork.NickName);

        // 현재 로그인한 디바이스를 DB에 등록 후 값 변경시 비교
        DatabaseReference deviceDataRef = GameManager.Backend.userUidDataRef.Child("Device");
        deviceDataRef.SetValueAsync(SystemInfo.deviceUniqueIdentifier);
        deviceDataRef.ValueChanged += DeviceDataValueChanged;
    }

    private void DeviceDataValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.Snapshot.Value != null && SystemInfo.deviceUniqueIdentifier != args.Snapshot.Value.ToString())
        {
            Debug.Log("중복 로그인 감지");
            PhotonNetwork.Disconnect();
            if (PopUp1.Instance != null)
            {
                PopUp1.Instance.PopUpOpen(false, "다른 디바이스에서 중복\n로그인이 감지되었습니다.");
            }
        }
    }
}
