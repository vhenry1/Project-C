using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

/// <summary>
/// Unity Gaming Services glue: sign in anonymously, then host or join a session with a room code.
/// The Multiplayer Services SDK sets up Relay and starts/stops the NetworkManager for us,
/// so players can connect over the internet without port forwarding.
/// Order matters: Initialize -> sign in -> create/join session.
/// </summary>
public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }
    public static bool Ready { get; private set; }

    public ISession Session { get; private set; }
    public string LastError { get; private set; } = "";
    public string JoinCode { get { return Session != null ? Session.Code : ""; } }

    private void Awake()
    {
        Instance = this;
    }

    public async Task<bool> InitAsync()
    {
        if (Ready) return true;
        try
        {
            // Each ParrelSync clone / build gets its own profile so two copies on one PC are different players.
            var options = new InitializationOptions();
            options.SetProfile("p" + (Application.dataPath.GetHashCode() & 0x7fffffff));
            await UnityServices.InitializeAsync(options);

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            Ready = true;
            return true;
        }
        catch (Exception e)
        {
            LastError = e.Message;
            Debug.LogWarning("UGS init failed: " + e.Message);
            return false;
        }
    }

    public async Task<bool> HostAsync(int maxPlayers)
    {
        try
        {
            var options = new SessionOptions { MaxPlayers = maxPlayers }.WithRelayNetwork();
            Session = await MultiplayerService.Instance.CreateSessionAsync(options);
            return true;
        }
        catch (Exception e)
        {
            LastError = e.Message;
            Debug.LogWarning("Host failed: " + e.Message);
            return false;
        }
    }

    public async Task<bool> JoinAsync(string code)
    {
        try
        {
            Session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code.Trim().ToUpperInvariant());
            return true;
        }
        catch (Exception e)
        {
            LastError = e.Message;
            Debug.LogWarning("Join failed: " + e.Message);
            return false;
        }
    }

    public async Task LeaveAsync()
    {
        if (Session == null) return;
        try { await Session.LeaveAsync(); }
        catch (Exception e) { Debug.LogWarning("Leave failed: " + e.Message); }
        Session = null;
    }
}
