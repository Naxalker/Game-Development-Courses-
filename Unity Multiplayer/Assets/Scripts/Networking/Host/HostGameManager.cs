using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HostGameManager : IDisposable
{
    private const int MaxConnections = 20;
    private const string GameSceneName = "Game";

    public NetworkServer NetworkServer { get; private set; }

    private NetworkObject _playerPrefab;

    private Allocation _allocation;
    private string _joinCode;
    private string _lobbyId;

    public HostGameManager(NetworkObject playerPrefab)
    {
        _playerPrefab = playerPrefab;
    }

    public async Task StartHostAsync()
    {
        try
        {
            _allocation = await Relay.Instance.CreateAllocationAsync(MaxConnections);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex);
            return;
        }

        try
        {
            _joinCode = await Relay.Instance.GetJoinCodeAsync(_allocation.AllocationId);
            Debug.Log(_joinCode);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex);
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        RelayServerData relayServerData = new RelayServerData(_allocation, "udp");
        transport.SetRelayServerData(relayServerData);

        try
        {
            CreateLobbyOptions lobbyOpptions = new CreateLobbyOptions();
            lobbyOpptions.IsPrivate = false;
            lobbyOpptions.Data = new Dictionary<string, DataObject>()
            {
                {
                    "JoinCode", new DataObject(
                        visibility: DataObject.VisibilityOptions.Member,
                        value: _joinCode
                    )
                }
            };

            string playerName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Unknown");
            Lobby lobby = await Lobbies.Instance.CreateLobbyAsync(
                    $"{playerName}'s Lobby", MaxConnections, lobbyOpptions);
            _lobbyId = lobby.Id;

            HostSingleton.Instance.StartCoroutine(HeartbeatLobby(15));
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogWarning(ex);
            return;
        }

        NetworkServer = new NetworkServer(NetworkManager.Singleton, _playerPrefab);

        UserData userData = new UserData
        {
            UserAuthId = AuthenticationService.Instance.PlayerId,
            UserName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Missing Name")
        };
        string payload = JsonUtility.ToJson(userData);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;

        NetworkManager.Singleton.StartHost();

        NetworkServer.OnClientLeft += HandleClientLeft;

        NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
    }

    public void Dispose()
    {
        Shutdown();
    }

    public async void Shutdown()
    {
        if (string.IsNullOrEmpty(_lobbyId)) { return; }

        HostSingleton.Instance.StopCoroutine(nameof(HeartbeatLobby));

        try
        {
            await Lobbies.Instance.DeleteLobbyAsync(_lobbyId);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogWarning(ex);
        }

        _lobbyId = string.Empty;

        NetworkServer.OnClientLeft -= HandleClientLeft;

        NetworkServer?.Dispose();
    }

    private IEnumerator HeartbeatLobby(float waitTimeSeconds)
    {
        WaitForSecondsRealtime delay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (true)
        {
            Lobbies.Instance.SendHeartbeatPingAsync(_lobbyId);
            yield return delay;
        }
    }

    private async void HandleClientLeft(string authId)
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(_lobbyId, authId);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);
        }
    }
}
