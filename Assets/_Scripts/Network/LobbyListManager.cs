using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyListManager : MonoBehaviour
{
    public static LobbyListManager instance;

    [Header("References")]
    [SerializeField] private GameObject lobbyListItemPrefab;
    [SerializeField] private TMP_InputField nameFilterInputField;
    [SerializeField] private TMP_Dropdown regionDropDown;
    [SerializeField] private Slider slotsAvaliableSlider;
    [SerializeField] private TMP_Text slotsAvaliableText;
    [SerializeField] private Transform content;

    //Regions
    private List<string> regions = new List<string>()
    {
        "World",
        "US-East",
        "US-West",
        "SouthAmerica",
        "Europe",
        "Asia",
        "Australia",
        "MiddleEast",
        "Africa"
    };

    //Callbacks
    protected Callback<LobbyDataUpdate_t> lobbyData;
    protected Callback<LobbyMatchList_t> lobbyList;

    private readonly List<GameObject> lobbyListItems = new();
    private readonly List<CSteamID> lobbyIDs = new();

    private void Awake()
    {
        //Check if initialized
        if (!SteamManager.Initialized) { return; }

        //Instance Create
        if (instance == null) { instance = this; }

        //Create Callbacks For Lobby
        lobbyData = Callback<LobbyDataUpdate_t>.Create(OnGetLobbyData);
        lobbyList = Callback<LobbyMatchList_t>.Create(OnGetLobbiesList);

        SceneManager.activeSceneChanged += SceneChanged;
    }

    private void OnEnable()
    {
        InvokeRepeating(nameof(GetListOfLobbies), 0, 1f);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void SceneChanged(Scene current, Scene next)
    {
        SceneManager.activeSceneChanged -= SceneChanged;

        lobbyData.Dispose();
        lobbyList.Dispose();
    }

    public void UpdateText()
    {
        slotsAvaliableText.text = "Slots Avaliable: " + (int)slotsAvaliableSlider.value;
    }

    private void GetListOfLobbies()
    {
        if (lobbyIDs.Count > 0) { lobbyIDs.Clear(); }

        //Filters
        SteamMatchmaking.AddRequestLobbyListStringFilter("region", regions[regionDropDown.value], ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListStringFilter("active", "true", ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable((int)slotsAvaliableSlider.value);
        SteamMatchmaking.AddRequestLobbyListResultCountFilter(100);

        //Request List
        SteamMatchmaking.RequestLobbyList();
    }

    private void OnGetLobbiesList(LobbyMatchList_t result)
    {
        if (lobbyListItems.Count > 0) { DestroyOldLobbies(); }

        for (int i=0; i < result.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);

            if (!string.IsNullOrEmpty(nameFilterInputField.text))
            {
                if (!SteamMatchmaking.GetLobbyData(lobbyID, "name").ToLower().Contains(nameFilterInputField.text.ToLower()))
                {
                    continue;
                }
            }

            if (SteamMatchmaking.GetLobbyData(lobbyID, "active") == "true")
            {
                lobbyIDs.Add(lobbyID);
                SteamMatchmaking.RequestLobbyData(lobbyID);
            }
        }
    }

    private void OnGetLobbyData(LobbyDataUpdate_t result)
    {
        DisplayLobbies(lobbyIDs, result);
    }

    public void DestroyOldLobbies()
    {
        foreach (GameObject lobbyItem in lobbyListItems)
        {     
            Destroy(lobbyItem);
        }

        lobbyListItems.Clear();
    }

    private void DisplayLobbies(List<CSteamID> lobbyIDs, LobbyDataUpdate_t result)
    {
        for (int i=0; i < lobbyIDs.Count; i++)
        {
            if (lobbyIDs[i].m_SteamID == result.m_ulSteamIDLobby)
            {
                GameObject lobbyListItem = Instantiate(lobbyListItemPrefab);
                LobbyListItem lobbyListItemScript = lobbyListItem.GetComponent<LobbyListItem>();

                lobbyListItemScript.lobbyId = (CSteamID)lobbyIDs[i].m_SteamID;
                lobbyListItemScript.lobbyName = SteamMatchmaking.GetLobbyData((CSteamID)lobbyIDs[i].m_SteamID, "name");
                lobbyListItemScript.lobbyStatus = SteamMatchmaking.GetLobbyData((CSteamID)lobbyIDs[i].m_SteamID, "status");
                lobbyListItemScript.numberOfPlayers = SteamMatchmaking.GetNumLobbyMembers((CSteamID)lobbyIDs[i].m_SteamID);
                lobbyListItemScript.maxNumberOfPlayers = SteamMatchmaking.GetLobbyMemberLimit((CSteamID)lobbyIDs[i].m_SteamID);
                lobbyListItemScript.SetLobbyItemValues();

                lobbyListItem.transform.SetParent(content);
                lobbyListItem.transform.localScale = Vector3.one;

                lobbyListItems.Add(lobbyListItem);

                return;
            }
        }

    }
}
