using System;
using System.Collections;
using UnityEngine;
using NutriMind.Core;
using NutriMind.Data;

namespace NutriMind.Network
{
    // =====================================================================================
    //  Uploads the learner's progress to the server in the background.
    //
    //  The game still saves everything locally first (SaveSystem), so it works with no internet.
    //  Every save sets StudentSave.needsSync = true. This service checks every few seconds
    //  (ApiConfig.SyncCheckIntervalSeconds) and, when a learner signed in through the server has
    //  unsent progress, POSTs the whole save file to /api/v1/game/sync. When the server accepts it,
    //  needsSync is set back to false. If the upload fails (offline), it simply tries again later.
    //
    //  The server keeps the HIGHEST progress it has seen, so uploading the same or an older save twice
    //  is harmless.
    //
    //  This object creates itself when the game starts; nothing needs to be added to any scene.
    //  Other scripts can call SyncService.Instance.SyncNow() (e.g. right after a mission ends) and listen to
    //  SyncService.ProfileUpdated to refresh points shown on screen.
    // =====================================================================================
    public class SyncService : MonoBehaviour
    {
        public static SyncService Instance { get; private set; }

        // Raised on the main thread whenever the server sends a newer profile (points, level, rewards).
        public static event Action<ProfileData> ProfileUpdated;

        public bool IsSyncing { get; private set; }
        public string LastError { get; private set; }

        float nextCheckAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            new GameObject("SyncService").AddComponent<SyncService>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Runs a request coroutine that should finish even if the current scene changes (e.g. revoking a token).
        public static void Run(IEnumerator routine)
        {
            if (Instance == null) Bootstrap();
            Instance.StartCoroutine(routine);
        }

        // Asks for an upload on the next frame instead of waiting for the next check.
        public void SyncNow()
        {
            nextCheckAt = 0f;
        }

        // Lets other scripts pass on a newer profile they received (e.g. the online quiz result).
        public static void PublishProfile(ProfileData profile)
        {
            ApiSession.UpdateProfile(profile);
            if (profile != null && ProfileUpdated != null) ProfileUpdated(profile);
        }

        void Update()
        {
            if (Time.unscaledTime < nextCheckAt) return;
            nextCheckAt = Time.unscaledTime + ApiConfig.SyncCheckIntervalSeconds;

            GameSession session = GameSession.Instance;
            if (IsSyncing || session == null || !session.IsSignedIn || !session.Save.needsSync) return;

            // Only upload when the current learner signed in through the server (not a test/debug account),
            // otherwise the server would refuse the save as "belongs to another learner".
            if (!ApiSession.IsFor(session.StudentId)) return;

            StartCoroutine(Upload(session.Save));
        }

        IEnumerator Upload(StudentSave save)
        {
            IsSyncing = true;

            // If the learner keeps playing while the upload is on its way, the save changes again and must stay
            // marked as unsent; comparing the save time tells us whether that happened.
            string savedAtWhenSent = save.lastSavedAt;
            string json = JsonUtility.ToJson(save);

            ApiResult result = null;
            yield return ApiClient.SyncProgress(json, r => result = r);

            if (result.Success)
            {
                LastError = null;
                SyncResponse response = result.Parse<SyncResponse>();

                if (save.lastSavedAt == savedAtWhenSent) SaveSystem.MarkSynced(save);
                PublishProfile(response.data);

                if (response.ignored != null && (response.ignored.missions.Count > 0 || response.ignored.rewards.Count > 0))
                {
                    // Not an error for the learner: an admin still has to set these up on the web.
                    Debug.LogWarning("NutriMind sync: the server has no match for missions [" +
                                     string.Join(", ", response.ignored.missions) + "] and rewards [" +
                                     string.Join(", ", response.ignored.rewards) +
                                     "]. Set the mission's \"Game mission ID\" / the reward's name on the web.");
                }
            }
            else
            {
                LastError = result.ErrorMessage;
                nextCheckAt = Time.unscaledTime + ApiConfig.SyncRetryAfterFailureSeconds;

                if (!result.IsServerUnreachable)
                {
                    // 422 = the server refused the save (see the message); 401 = must sign in online again.
                    Debug.LogWarning("NutriMind sync refused (" + result.StatusCode + "): " + result.ErrorMessage + "\n" + result.Body);
                }
            }

            IsSyncing = false;
        }
    }
}
