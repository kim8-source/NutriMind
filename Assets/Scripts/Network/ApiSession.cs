using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace NutriMind.Network
{
    // =====================================================================================
    //  Who is signed in to the SERVER, and the token that proves it.
    //
    //  GameSession (Core) still runs the game: it holds the local save file and the unlock rules.
    //  ApiSession only remembers the server side:
    //      Token    -> sent with every API request (ApiClient adds it)
    //      Profile  -> the learner's name, grade and the server's points/level
    //
    //  OFFLINE SIGN-IN
    //  After a learner signs in online once, this device remembers them (PlayerPrefs): their username/LRN,
    //  a salted SHA-256 hash of their password (never the password itself), their token and their profile.
    //  If the server is unreachable later, the same username/LRN + password signs them in offline, and the
    //  progress they make uploads automatically once the server is reachable again (SyncService).
    //  Several learners can share one device; each is remembered separately.
    // =====================================================================================
    public static class ApiSession
    {
        const string PrefsKey = "NutriMind.Api.RememberedAccounts";
        const int MaxRememberedAccounts = 60;   // enough for a shared classroom tablet

        public static string Token { get; private set; }
        public static ProfileData Profile { get; private set; }

        // True when signed in from this device's memory because the server was unreachable.
        public static bool IsOfflineSession { get; private set; }

        public static bool HasToken
        {
            get { return !string.IsNullOrEmpty(Token); }
        }

        // True when GameSession's current learner is the learner this token belongs to.
        // (GameSession.StudentId is the server's user ID as text, e.g. "12".)
        public static bool IsFor(string gameStudentId)
        {
            return HasToken && Profile != null && Profile.id.ToString() == gameStudentId;
        }

        // ---------- Sign in / out ----------

        // Call after a successful online login.
        public static void SignedInOnline(string typedUsernameOrLrn, string password, LoginResponse login)
        {
            Token = login.token;
            Profile = login.data;
            IsOfflineSession = false;

            RememberedAccount previous = FindByStudentId(login.data.id);
            string previousToken = previous != null ? previous.token : null;

            Remember(typedUsernameOrLrn, password, login);

            // The device now uses the new token; revoke the learner's old one so tokens don't pile up.
            if (!string.IsNullOrEmpty(previousToken) && previousToken != login.token)
            {
                SyncService.Run(ApiClient.RevokeToken(previousToken, null));
            }
        }

        // Tries to sign in from this device's memory. Returns false when the learner never signed in online
        // here, or the password does not match.
        public static bool TryOfflineSignIn(string typedUsernameOrLrn, string password, out string gameStudentId, out int grade)
        {
            gameStudentId = null;
            grade = 0;

            RememberedAccount account = FindByIdentifier(typedUsernameOrLrn);
            if (account == null || account.passwordHash != Hash(password, account.salt)) return false;

            ProfileData profile = JsonUtility.FromJson<ProfileData>(account.profileJson);
            if (profile == null) return false;

            Token = account.token;
            Profile = profile;
            IsOfflineSession = true;

            gameStudentId = profile.id.ToString();
            grade = profile.grade == 6 ? 6 : 5;
            return true;
        }

        // Ends the current session on this device. The learner stays remembered for offline sign-in and their
        // token stays valid, so progress made offline can still upload after they sign in again.
        public static void SignOut()
        {
            Token = null;
            Profile = null;
            IsOfflineSession = false;
        }

        // Called when the server answers 401 (token revoked or account deactivated): forget the token everywhere.
        public static void ForgetRejectedToken()
        {
            if (Profile != null)
            {
                RememberedAccountList list = Load();
                list.accounts.RemoveAll(a => a.studentId == Profile.id);
                Store(list);
            }
            Token = null;
            IsOfflineSession = false;
            Debug.LogWarning("NutriMind API: the server rejected the saved sign-in. The learner must sign in online again.");
        }

        // Stores newer profile data from the server (after a sync or a quiz submit).
        public static void UpdateProfile(ProfileData profile)
        {
            if (profile == null || Profile == null || profile.id != Profile.id) return;

            Profile = profile;
            IsOfflineSession = false;   // the server answered, so we are online again

            RememberedAccountList list = Load();
            RememberedAccount account = list.accounts.Find(a => a.studentId == profile.id);
            if (account != null)
            {
                account.profileJson = JsonUtility.ToJson(profile);
                Store(list);
            }
        }

        // ---------- Device memory (PlayerPrefs) ----------

        [Serializable]
        class RememberedAccount
        {
            public int studentId;
            public List<string> identifiers = new List<string>();   // normalized username and/or LRN typed
            public string salt;
            public string passwordHash;
            public string token;
            public string profileJson;
            public long lastSignInTicks;
        }

        [Serializable]
        class RememberedAccountList
        {
            public List<RememberedAccount> accounts = new List<RememberedAccount>();
        }

        static void Remember(string typedUsernameOrLrn, string password, LoginResponse login)
        {
            RememberedAccountList list = Load();
            RememberedAccount account = list.accounts.Find(a => a.studentId == login.data.id);
            if (account == null)
            {
                account = new RememberedAccount { studentId = login.data.id };
                list.accounts.Add(account);
            }

            AddIdentifier(account, typedUsernameOrLrn);
            AddIdentifier(account, login.data.username);
            account.salt = Guid.NewGuid().ToString("N");
            account.passwordHash = Hash(password, account.salt);
            account.token = login.token;
            account.profileJson = JsonUtility.ToJson(login.data);
            account.lastSignInTicks = DateTime.UtcNow.Ticks;

            // Keep only the most recent learners.
            list.accounts.Sort((a, b) => b.lastSignInTicks.CompareTo(a.lastSignInTicks));
            if (list.accounts.Count > MaxRememberedAccounts)
            {
                list.accounts.RemoveRange(MaxRememberedAccounts, list.accounts.Count - MaxRememberedAccounts);
            }

            Store(list);
        }

        static RememberedAccount FindByIdentifier(string typed)
        {
            string normalized = Normalize(typed);
            return Load().accounts.Find(a => a.identifiers.Contains(normalized));
        }

        static RememberedAccount FindByStudentId(int studentId)
        {
            return Load().accounts.Find(a => a.studentId == studentId);
        }

        static void AddIdentifier(RememberedAccount account, string identifier)
        {
            string normalized = Normalize(identifier);
            if (!string.IsNullOrEmpty(normalized) && !account.identifiers.Contains(normalized))
            {
                account.identifiers.Add(normalized);
            }
        }

        // Same rules as the server: usernames ignore letter case, LRNs ignore spaces and dashes.
        static string Normalize(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) return "";
            string trimmed = identifier.Trim().ToLowerInvariant();
            string digits = trimmed.Replace(" ", "").Replace("-", "");
            bool isLrn = digits.Length == 12;
            for (int i = 0; i < digits.Length && isLrn; i++)
            {
                if (!char.IsDigit(digits[i])) isLrn = false;
            }
            return isLrn ? digits : trimmed;
        }

        static string Hash(string password, string salt)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(salt + ":" + password));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes) builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        static RememberedAccountList Load()
        {
            string json = PlayerPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(json)) return new RememberedAccountList();
            try
            {
                return JsonUtility.FromJson<RememberedAccountList>(json) ?? new RememberedAccountList();
            }
            catch (ArgumentException)
            {
                return new RememberedAccountList();
            }
        }

        static void Store(RememberedAccountList list)
        {
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(list));
            PlayerPrefs.Save();
        }
    }
}
