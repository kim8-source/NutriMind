namespace NutriMind.Network
{
    // =====================================================================================
    //  NUTRIMIND SERVER CONNECTION SETTINGS   (this is the ONLY file you need to edit)
    // =====================================================================================
    //
    //  The game talks to the Laravel web app (web/NutriMind) through its game API:
    //      <BaseUrl>/api/v1/game/login     sign in with username or LRN + password
    //      <BaseUrl>/api/v1/game/me        the learner's profile, points, level, rewards
    //      <BaseUrl>/api/v1/game/quizzes   the teachers' quizzes (online quiz scene)
    //      <BaseUrl>/api/v1/game/sync      uploads the local save file (missions, rewards)
    //
    //  HOW TO RUN THE SERVER ON YOUR PC
    //      cd web/NutriMind
    //      php artisan serve                         -> http://127.0.0.1:8000  (Unity Editor on the same PC)
    //      php artisan serve --host=0.0.0.0          -> also reachable from a phone on the same Wi-Fi
    //
    //  WHICH BaseUrl TO USE
    //      Unity Editor on the same PC as Laravel ... "http://127.0.0.1:8000"   (default below)
    //      Android emulator ......................... "http://10.0.2.2:8000"    (the emulator's name for your PC)
    //      Real phone on the same Wi-Fi ............. "http://192.168.x.x:8000" (your PC's IPv4 from `ipconfig`)
    //      Hosted server ............................ "https://your-domain.com" (always use https when hosted)
    //
    //  Plain http:// is allowed by Assets/Scripts/Editor/AllowHttpForLocalServer.cs. When the game moves to
    //  an https server, that file can be deleted.
    //
    //  Do NOT end BaseUrl with a slash.
    // =====================================================================================
    public static class ApiConfig
    {
        // ---- Where the Laravel server is ------------------------------------------------

        // Laravel's default `php artisan serve` address.
        public const string BaseUrl = "http://127.0.0.1:8000";

        // The game API's path inside the Laravel app (routes/api.php). Change only if the backend changes it.
        public const string ApiPath = "/api/v1/game";

        // ---- Timing ---------------------------------------------------------------------

        // How long to wait for the server before treating it as "offline".
        public const int TimeoutSeconds = 10;

        // How often (seconds) the game checks whether the save has unsent progress and uploads it.
        public const float SyncCheckIntervalSeconds = 5f;

        // After a failed upload (no internet, server down), wait this long before trying again.
        public const float SyncRetryAfterFailureSeconds = 60f;

        // ---- Offline behaviour ----------------------------------------------------------

        // When the server cannot be reached, a learner who signed in online on THIS device before can still
        // sign in with the same username/LRN and password, play offline, and their progress uploads later.
        public static readonly bool AllowOfflineSignIn = true;

        // The two hard-coded test accounts in MenuNavigation.TestAccounts ("05-9875661" / "student05",
        // "06-1667392" / "student06") still work when the server cannot be reached, for testing without
        // Laravel running. Their progress is never uploaded. Set to false before giving the game to schools.
        public static readonly bool AllowTestAccounts = true;

        // ---- Misc -----------------------------------------------------------------------

        // Shown to teachers/admins as the token's device name (helps tell a learner's devices apart).
        public const string DeviceName = "NutriMind Unity game";

        // Writes every request and response status to the Unity Console. Turn off for release builds.
        public static readonly bool LogRequests = true;

        // Builds a full URL from an API path, e.g. Url("/login") -> "http://127.0.0.1:8000/api/v1/game/login".
        public static string Url(string path)
        {
            return BaseUrl + ApiPath + path;
        }
    }
}
