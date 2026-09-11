using System;

/// <summary>
/// Multiplayer Play Mode (MPPM) helpers.
/// Works with the MPPM that ships built-in with Unity 6.6+ (Play Mode Scenarios).
/// Detection is done via the documented `-name` launch argument:
///   main editor            -> "Player1"
///   additional/virtual     -> "Player2..4" (or any custom name you configure)
/// This approach requires no assembly reference to the (editor-built-in) Play Mode
/// package, which keeps this code valid in standalone player builds too.
/// Role convention used by this project:
///   instance name containing "Host"   -> starts as Host
///   instance name containing "Client" -> starts as Client
///   otherwise fallback: Player1 (main editor) -> Host, any other -> Client.
/// Set instance names in the MPPM Play Mode Scenario window.
/// </summary>
public static class MppmAutoStart
{
    public enum MppmRole { None, Host, Client }

    private const string HostTag = "Host";
    private const string ClientTag = "Client";
    private const string MainEditorName = "Player1";

    private static string[] _args;

    /// <summary>
    /// Returns the role for this instance based on its MPPM `-name` argument.
    /// Returns false when not running under MPPM (or name is empty).
    /// </summary>
    public static bool TryGetRole(out MppmRole role)
    {
        role = MppmRole.None;
        string name = GetPlayerName();
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        // Explicit role in the name wins.
        if (name.IndexOf(HostTag, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            role = MppmRole.Host;
            return true;
        }
        if (name.IndexOf(ClientTag, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            role = MppmRole.Client;
            return true;
        }

        // Fallback: the default MPPM names map to host/client.
        role = string.Equals(name, MainEditorName, StringComparison.OrdinalIgnoreCase)
            ? MppmRole.Host
            : MppmRole.Client;
        return true;
    }

    /// <summary>
    /// The MPPM instance name from the `-name` launch argument, or empty.
    /// </summary>
    public static string GetPlayerName()
    {
        if (_args == null)
        {
            _args = Environment.GetCommandLineArgs();
        }
        for (int i = 0; i < _args.Length - 1; i++)
        {
            if (string.Equals(_args[i], "-name", StringComparison.OrdinalIgnoreCase))
            {
                return _args[i + 1];
            }
        }
        return string.Empty;
    }
}
