using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyItem : MonoBehaviour
{
    [SerializeField] private TMP_Text _lobbyNameText;
    [SerializeField] private TMP_Text _lobbyPlayersText;

    private LobbiesList _lobbyList;
    private Lobby _lobby;

    public void Initialize(LobbiesList lobbiesList, Lobby lobby)
    {
        _lobbyList = lobbiesList;
        _lobby = lobby;

        _lobbyNameText.text = lobby.Name;
        _lobbyPlayersText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
    }

    public void Join()
    {
        _lobbyList.JoinAsync(_lobby);
    }
}
