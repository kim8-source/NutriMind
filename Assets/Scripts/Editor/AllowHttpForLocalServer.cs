using UnityEditor;
using UnityEngine;
using NutriMind.Network;

// Unity 6 blocks plain http:// requests by default ("Insecure connection not allowed").
// Laravel's local server (php artisan serve) is http://127.0.0.1:8000, so while ApiConfig.BaseUrl starts with
// "http://", this script turns on Player Settings > Other Settings > "Allow downloads over HTTP" = Always allowed
// every time the Editor loads. That setting also lets Android builds reach a PC on the same Wi-Fi.
//
// When the game points to a hosted https:// server, this script does nothing (and can be deleted). Then set
// "Allow downloads over HTTP" back to "Not allowed" for release builds.
[InitializeOnLoad]
static class AllowHttpForLocalServer
{
    static AllowHttpForLocalServer()
    {
        if (!ApiConfig.BaseUrl.StartsWith("http://")) return;
        if (PlayerSettings.insecureHttpOption == InsecureHttpOption.AlwaysAllowed) return;

        PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
        Debug.Log("NutriMind: allowed plain http:// in Player Settings so the game can reach " + ApiConfig.BaseUrl +
                  " (see Assets/Scripts/Editor/AllowHttpForLocalServer.cs).");
    }
}
