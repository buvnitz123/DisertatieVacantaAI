using MauiAppDisertatieVacantaAI.Classes.Database.Repositories;
using MauiAppDisertatieVacantaAI.Classes.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace MauiAppDisertatieVacantaAI.Classes.Session
{
    public class UserSession
    {
        public const string SessionKeyUserId = "UserId";
        public const string SessionKeyUserEmail = "UserEmail";
        public const string SessionKeyUserNume = "UserNume";
        public const string SessionKeyUserPrenume = "UserPrenume";
        public const string SessionKeyIsRemembered = "IsRemembered";
        public const string SessionKeyIsLoggedIn = "IsLoggedIn";

        // In-memory session for current app lifecycle
        public static string? CurrentUserId { get; set; }
        public static string? CurrentUserEmail { get; set; }
        public static string? CurrentUserNume { get; set; }
        public static string? CurrentUserPrenume { get; set; }

        // Legacy static properties (keep for compatibility)
        public static string? UserId { get; set; }
        public static string? UserEmail { get; set; }
        public static string? UserNume { get; set; }
        public static string? UserPrenume { get; set; }
        public static string? IsRemembered { get; set; }

        public static void ClearSession()
        {
            SecureStorage.Remove(SessionKeyUserId);
            SecureStorage.Remove(SessionKeyUserEmail);
            SecureStorage.Remove(SessionKeyUserNume);
            SecureStorage.Remove(SessionKeyUserPrenume);
            SecureStorage.Remove(SessionKeyIsRemembered);
            SecureStorage.Remove(SessionKeyIsLoggedIn);
            
            // Clear in-memory session
            CurrentUserId = null;
            CurrentUserEmail = null;
            CurrentUserNume = null;
            CurrentUserPrenume = null;
        }

        public static string GetFullName()
        {
            return $"{CurrentUserNume ?? UserNume} {CurrentUserPrenume ?? UserPrenume}";
        }

        // Set current session (always in memory, optionally persist)
        public static void SetCurrentUser(string id, string email, string nume, string prenume, bool persist = false)
        {
            CurrentUserId = id;
            CurrentUserEmail = email;
            CurrentUserNume = nume;
            CurrentUserPrenume = prenume;

            if (persist)
            {
                SetSecureStorage(SessionKeyUserId, id);
                SetSecureStorage(SessionKeyUserEmail, email);
                SetSecureStorage(SessionKeyUserNume, nume);
                SetSecureStorage(SessionKeyUserPrenume, prenume);
                SetSecureStorage(SessionKeyIsRemembered, "true");
            }
        }

        // Legacy method for compatibility
        public static void SetUser(string id, string email, string nume, string prenume, string isRemembered)
        {
            SetSecureStorage(SessionKeyUserId, id);
            SetSecureStorage(SessionKeyUserEmail, email);
            SetSecureStorage(SessionKeyUserNume, nume);
            SetSecureStorage(SessionKeyUserPrenume, prenume);
            SetSecureStorage(SessionKeyIsRemembered, isRemembered);
            
            // Also set current session
            CurrentUserId = id;
            CurrentUserEmail = email;
            CurrentUserNume = nume;
            CurrentUserPrenume = prenume;
        }

        public static Task SetLoggedInAsync(bool loggedIn)
            => SecureStorage.SetAsync(SessionKeyIsLoggedIn, loggedIn ? "true" : "false");

        public static async Task<bool> IsLoggedInAsync()
        {
            try
            {
                var value = await SecureStorage.GetAsync(SessionKeyIsLoggedIn);
                return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static void SetSecureStorage(string key, string value)
        {
            SecureStorage.SetAsync(key, value);
        }

        // Prefer current session, fallback to persistent storage
        public static async Task<string?> GetUserIdAsync()
        {
            if (!string.IsNullOrEmpty(CurrentUserId))
                return CurrentUserId;
            return await SecureStorage.GetAsync(SessionKeyUserId);
        }

        public static async Task<string?> GetUserEmailAsync()
        {
            if (!string.IsNullOrEmpty(CurrentUserEmail))
                return CurrentUserEmail;
            return await SecureStorage.GetAsync(SessionKeyUserEmail);
        }

        public static async Task<string?> GetUserNumeAsync()
        {
            if (!string.IsNullOrEmpty(CurrentUserNume))
                return CurrentUserNume;
            return await SecureStorage.GetAsync(SessionKeyUserNume);
        }

        public static async Task<string?> GetUserPrenumeAsync()
        {
            if (!string.IsNullOrEmpty(CurrentUserPrenume))
                return CurrentUserPrenume;
            return await SecureStorage.GetAsync(SessionKeyUserPrenume);
        }

        public static async Task<string?> GetUserNameAsync()
        {
            try
            {
                var nume = await GetUserNumeAsync();
                var prenume = await GetUserPrenumeAsync();
                if (!string.IsNullOrWhiteSpace(nume) || !string.IsNullOrWhiteSpace(prenume))
                {
                    return ($"{nume} {prenume}").Trim();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        // Synchronous helper (blocks); prefer the async version below in UI code
        public static Utilizator? GetUserFromSession()
        {
            try
            {
                var idStr = GetUserIdAsync().Result;
                if (string.IsNullOrWhiteSpace(idStr)) return null;
                if (!int.TryParse(idStr, out int id)) return null;

                var utilizatorRepository = new UtilizatorRepository();
                return utilizatorRepository.GetById(id);
            }
            catch
            {
                return null;
            }
        }

        // Async-friendly version to avoid deadlocks on the UI thread
        public static async Task<Utilizator?> GetUserFromSessionAsync()
        {
            try
            {
                var idStr = await GetUserIdAsync();
                if (string.IsNullOrWhiteSpace(idStr)) return null;
                if (!int.TryParse(idStr, out int id)) return null;

                var utilizatorRepository = new UtilizatorRepository();
                return utilizatorRepository.GetById(id);
            }
            catch
            {
                return null;
            }
        }
    }
}

