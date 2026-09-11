using System.Collections;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// B1 LAN bootstrap — the single networking authority in the test scene.
/// Owns the one NetworkManager + UnityTransport (UDP, port 7777) and provides
/// the minimal Host / Client controls plus a debug status line.
/// Host listens on 0.0.0.0 so any device on the same LAN can connect;
/// the client connects directly to the host's LAN IP. No relay/lobby/services.
///
/// Manual control: on-screen buttons or hotkeys
///   F9  = auto (MPPM role, see MppmAutoStart)
///   F10 = Start Host
///   F11 = Start Client
///   F12 = Stop
/// MPPM auto-start: when Multiplayer Play Mode is active, instances whose
/// player name contains "Host" start as host and "Client" as client
/// automatically at play start — no clicks needed.
/// </summary>
[RequireComponent(typeof(NetworkManager))]
[RequireComponent(typeof(UnityTransport))]
public class LanBootstrap : MonoBehaviour
{
    private const ushort DefaultPort = 7777;

    [SerializeField] private string defaultClientIp = "127.0.0.1";
    [SerializeField] private ushort port = DefaultPort;
    [Tooltip("At play start, auto-start Host/Client based on the MPPM instance name (Host/Client).")]
    [SerializeField] private bool autoStartFromMppmTag = true;

    private string _clientIp;
    private string _status = "Disconnected";

    private void Awake()
    {
        _clientIp = defaultClientIp;
    }

    private void Start()
    {
        var nm = NetworkManager.Singleton;
        nm.OnClientConnectedCallback += OnClientConnected;
        nm.OnClientDisconnectCallback += OnClientDisconnected;
        nm.OnServerStopped += _ => { _status = "Disconnected (server stopped)"; };
        nm.OnClientStopped += _ => { _status = "Disconnected (client stopped)"; };

        EnsureTransportAssigned();

        if (autoStartFromMppmTag)
        {
            StartCoroutine(AutoStartWhenTagged());
        }
    }

    /// <summary>
    /// Safety net: if NetworkConfig.NetworkTransport is not assigned (e.g. the
    /// bootstrap GameObject was built with AddComponent before this was wired),
    /// assign the UnityTransport on the same GameObject. Without this,
    /// StartHost/StartClient fail with "No transport has been selected!".
    /// </summary>
    private static void EnsureTransportAssigned()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            return;
        }
        if (nm.NetworkConfig == null || nm.NetworkConfig.NetworkTransport != null)
        {
            return;
        }
        var utp = nm.GetComponent<UnityTransport>();
        if (utp != null)
        {
            nm.NetworkConfig.NetworkTransport = utp;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        _status = NetworkManager.Singleton.IsServer
            ? $"Host — Client-{clientId} connected"
            : $"Client — connected (id {clientId})";
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer && clientId != NetworkManager.Singleton.LocalClientId)
        {
            _status = $"Host — Client-{clientId} disconnected";
        }
        // Local client disconnect is reported via OnClientStopped.
    }

    /// <summary>
    /// MPPM applies player names right at play start; poll briefly for a role,
    /// then start once. Exits early if the user started something manually.
    /// </summary>
    private IEnumerator AutoStartWhenTagged()
    {
        for (int i = 0; i < 40; i++)
        {
            if (IsRunning())
            {
                yield break;
            }
            if (MppmAutoStart.TryGetRole(out var role))
            {
                if (role == MppmAutoStart.MppmRole.Host)
                {
                    StartHost();
                }
                else if (role == MppmAutoStart.MppmRole.Client)
                {
                    StartClient();
                }
                yield break;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null)
        {
            return;
        }
        if (kb.f9Key.wasPressedThisFrame)
        {
            TryAutoStart();
        }
        if (kb.f10Key.wasPressedThisFrame)
        {
            StartHost();
        }
        if (kb.f11Key.wasPressedThisFrame)
        {
            StartClient();
        }
        if (kb.f12Key.wasPressedThisFrame && IsRunning())
        {
            _status = "Stopping…";
            NetworkManager.Singleton.Shutdown();
        }
    }

    private void TryAutoStart()
    {
        if (IsRunning())
        {
            _status = "Already running — press F12 to stop first";
            return;
        }
        if (MppmAutoStart.TryGetRole(out var role))
        {
            if (role == MppmAutoStart.MppmRole.Host)
            {
                StartHost();
            }
            else
            {
                StartClient();
            }
        }
        else
        {
            _status = "No MPPM role — use F10 (Host) / F11 (Client)";
        }
    }

    private bool IsRunning()
    {
        var nm = NetworkManager.Singleton;
        return nm != null && (nm.IsListening || nm.IsConnectedClient);
    }

    private void StartHost()
    {
        if (IsRunning())
        {
            _status = "Already running — press F12 to stop first";
            return;
        }
        // Listen on all interfaces so other LAN devices can connect.
        NetworkManager.Singleton.GetComponent<UnityTransport>()
            .SetConnectionData("0.0.0.0", port);
        _status = NetworkManager.Singleton.StartHost()
            ? $"Host started (port {port})"
            : "Host failed to start";
    }

    private void StartClient()
    {
        if (IsRunning())
        {
            _status = "Already running — press F12 to stop first";
            return;
        }
        if (string.IsNullOrWhiteSpace(_clientIp))
        {
            _status = "Client — enter host LAN IP first";
            return;
        }
        NetworkManager.Singleton.GetComponent<UnityTransport>()
            .SetConnectionData(_clientIp.Trim(), port);
        _status = $"Client — connecting to {_clientIp.Trim()}:{port}…";
        NetworkManager.Singleton.StartClient();
    }

    private void OnGUI()
    {
        var nm = NetworkManager.Singleton;
        GUILayout.BeginArea(new Rect(10, 10, 340, 300), GUI.skin.box);

        GUILayout.Label("<b>JANITOR × SERIAL KILLER — B1 network test</b>", new GUIStyle(GUI.skin.label) { richText = true });
        GUILayout.Label($"Status: {_status}");

        if (nm == null)
        {
            GUILayout.Label("ERROR: no NetworkManager.Singleton in scene!");
            GUILayout.EndArea();
            return;
        }

        GUILayout.Label($"Players connected: {nm.ConnectedClientsIds.Count}");
        var mppmName = MppmAutoStart.GetPlayerName();
        if (!string.IsNullOrEmpty(mppmName))
        {
            GUILayout.Label($"MPPM instance: {mppmName}");
        }
        GUILayout.Space(6);

        if (!nm.IsListening && !nm.IsConnectedClient)
        {
            GUILayout.Label("Host LAN IP(s): " + GetLanIps());
            GUILayout.Label("Client IP:");
            _clientIp = GUILayout.TextField(_clientIp);

            if (GUILayout.Button("Start Host  (F10)", GUILayout.Height(30)))
            {
                StartHost();
            }
            if (GUILayout.Button("Start Client  (F11)", GUILayout.Height(30)))
            {
                StartClient();
            }
        }
        else if (GUILayout.Button("Stop  (F12)", GUILayout.Height(30)))
        {
            _status = "Stopping…";
            nm.Shutdown();
        }
        GUILayout.Label("F9 auto · F10 host · F11 client · F12 stop");
        GUILayout.EndArea();
    }

    private static string GetLanIps()
    {
        try
        {
            var host = Dns.GetHostName();
            var ips = Dns.GetHostAddresses(host);
            var lan = new System.Collections.Generic.List<string>();
            foreach (var ip in ips)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    lan.Add(ip.ToString());
                }
            }
            return lan.Count > 0 ? string.Join(", ", lan) : "(none found)";
        }
        catch (SocketException)
        {
            return "(lookup failed)";
        }
    }
}