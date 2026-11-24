using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace NutriMind.Network
{
    // The result of one request to the Laravel server.
    public class ApiResult
    {
        // True for a 2xx answer.
        public bool Success;

        // True when the server could not be reached or broke (no internet, server not running, timeout, 5xx).
        // Callers treat this as "offline" and try again later. A wrong password is NOT unreachable.
        public bool IsServerUnreachable;

        // HTTP status (0 when nothing came back). 401 = token missing/expired, 403 = forbidden,
        // 404 = not found (e.g. a quiz the learner may not take), 422 = validation error.
        public long StatusCode;

        // The raw JSON body.
        public string Body;

        // A message that can be shown to the learner as-is.
        public string ErrorMessage;

        // Turns the JSON body into one of the classes in ApiModels.cs.
        public T Parse<T>()
        {
            return JsonUtility.FromJson<T>(Body);
        }
    }

    // =====================================================================================
    //  Sends requests to the Laravel game API. Every endpoint the game uses is listed here,
    //  so the paths live in one place. The server address comes from ApiConfig.cs.
    //
    //  These are coroutines. Call them from a MonoBehaviour:
    //      StartCoroutine(ApiClient.GetProfile(result => {
    //          if (result.Success) { ProfileData me = result.Parse<ProfileResponse>().data; }
    //          else Debug.Log(result.ErrorMessage);
    //      }));
    //
    //  The token from login (ApiSession.Token) is added automatically as "Authorization: Bearer ...".
    // =====================================================================================
    public static class ApiClient
    {
        // ---------- Endpoints ----------

        // POST /login : username OR LRN + password. Success body: LoginResponse.
        public static IEnumerator Login(string usernameOrLrn, string password, Action<ApiResult> done)
        {
            var body = new LoginRequest
            {
                username = usernameOrLrn,
                password = password,
                device_name = ApiConfig.DeviceName + " (" + SystemInfo.deviceModel + ")"
            };
            return Send("POST", "/login", JsonUtility.ToJson(body), done, null);
        }

        // POST /logout : revokes a token on the server (used to clean up a learner's old token).
        public static IEnumerator RevokeToken(string token, Action<ApiResult> done)
        {
            return Send("POST", "/logout", null, done, token);
        }

        // GET /me : the signed-in learner's profile. Success body: ProfileResponse.
        public static IEnumerator GetProfile(Action<ApiResult> done)
        {
            return Send("GET", "/me", null, done, ApiSession.Token);
        }

        // GET /quizzes : the teachers' quizzes the learner may take (no questions). Success body: QuizListResponse.
        public static IEnumerator GetQuizzes(Action<ApiResult> done)
        {
            return Send("GET", "/quizzes", null, done, ApiSession.Token);
        }

        // GET /quizzes/{id} : one quiz with its questions (no correct answers). Success body: QuizDetailResponse.
        public static IEnumerator GetQuiz(int quizId, Action<ApiResult> done)
        {
            return Send("GET", "/quizzes/" + quizId, null, done, ApiSession.Token);
        }

        // POST /quizzes/{id}/attempts : the server grades the answers. Success (201) body: QuizAttemptResponse.
        public static IEnumerator SubmitQuizAttempt(int quizId, QuizAttemptRequest attempt, Action<ApiResult> done)
        {
            return Send("POST", "/quizzes/" + quizId + "/attempts", JsonUtility.ToJson(attempt), done, ApiSession.Token);
        }

        // POST /sync : uploads the whole local save file (StudentSave as JSON). Success body: SyncResponse.
        public static IEnumerator SyncProgress(string studentSaveJson, Action<ApiResult> done)
        {
            return Send("POST", "/sync", studentSaveJson, done, ApiSession.Token);
        }

        // ---------- The one place requests are actually sent ----------

        static IEnumerator Send(string method, string path, string json, Action<ApiResult> done, string token)
        {
            string url = ApiConfig.Url(path);
            var result = new ApiResult();

            using (var request = new UnityWebRequest(url, method))
            {
                if (json != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    request.SetRequestHeader("Content-Type", "application/json");
                }
                request.downloadHandler = new DownloadHandlerBuffer();

                // Without this header Laravel would answer errors with HTML/redirects instead of JSON.
                request.SetRequestHeader("Accept", "application/json");
                if (!string.IsNullOrEmpty(token)) request.SetRequestHeader("Authorization", "Bearer " + token);
                request.timeout = ApiConfig.TimeoutSeconds;

                UnityWebRequestAsyncOperation operation = null;
                try
                {
                    operation = request.SendWebRequest();
                }
                catch (InvalidOperationException e)
                {
                    // Unity refuses plain http:// unless Player Settings allow it (see AllowHttpForLocalServer.cs).
                    result.IsServerUnreachable = true;
                    result.ErrorMessage = "Can't reach the NutriMind server.";
                    Debug.LogError("NutriMind API: request to " + url + " was blocked: " + e.Message +
                                   "\nPlain http:// must be allowed (Assets/Scripts/Editor/AllowHttpForLocalServer.cs).");
                }

                if (operation != null)
                {
                    yield return operation;
                    Fill(result, request);
                }
            }

            if (ApiConfig.LogRequests)
            {
                Debug.Log("NutriMind API: " + method + " " + url + " -> " +
                          (result.StatusCode > 0 ? result.StatusCode.ToString() : "no response") +
                          (result.Success ? "" : " (" + result.ErrorMessage + ")"));
            }

            // The server no longer accepts the token (revoked, or the account was deactivated).
            if (result.StatusCode == 401 && !string.IsNullOrEmpty(token) && token == ApiSession.Token)
            {
                ApiSession.ForgetRejectedToken();
            }

            if (done != null) done(result);
        }

        static void Fill(ApiResult result, UnityWebRequest request)
        {
            result.StatusCode = request.responseCode;
            result.Body = request.downloadHandler != null ? request.downloadHandler.text : "";
            result.Success = request.result == UnityWebRequest.Result.Success && result.StatusCode >= 200 && result.StatusCode < 300;
            result.IsServerUnreachable = request.result == UnityWebRequest.Result.ConnectionError || result.StatusCode >= 500 || result.StatusCode == 0;

            if (result.Success) return;

            if (result.IsServerUnreachable)
            {
                result.ErrorMessage = "Can't reach the NutriMind server. Check your internet connection and try again.";
                return;
            }

            // Laravel errors always carry a readable "message".
            string serverMessage = null;
            try
            {
                ApiErrorResponse error = JsonUtility.FromJson<ApiErrorResponse>(result.Body);
                if (error != null) serverMessage = error.message;
            }
            catch (ArgumentException)
            {
                // Not JSON (e.g. a proxy's HTML page); fall back to a generic message below.
            }

            if (result.StatusCode == 401) result.ErrorMessage = "Your sign-in has expired. Please sign in again.";
            else if (!string.IsNullOrEmpty(serverMessage)) result.ErrorMessage = serverMessage;
            else result.ErrorMessage = "Something went wrong (error " + result.StatusCode + ").";
        }
    }
}
