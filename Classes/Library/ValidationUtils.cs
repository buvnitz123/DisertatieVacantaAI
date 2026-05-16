using System.Text.RegularExpressions;

namespace MauiAppDisertatieVacantaAI.Classes.Library
{
    public static class ValidationUtils
    {
        #region Email Validation

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return emailRegex.IsMatch(email.Trim());
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Name Validation

        public static bool IsValidName(string name, int minLength = 2, int maxLength = 50)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var trimmedName = name.Trim();
            return trimmedName.Length >= minLength && trimmedName.Length <= maxLength;
        }

        public static bool IsValidNume(string nume)
        {
            return IsValidName(nume);
        }


        public static bool IsValidPrenume(string prenume)
        {
            return IsValidName(prenume);
        }

        #endregion

        #region Password Validation

        public static bool IsValidPassword(string password)
        {
            return !string.IsNullOrWhiteSpace(password);
        }

        public static bool DoPasswordsMatch(string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
                return false;

            return password == confirmPassword;
        }

        public static int CalculatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return 0;

            int score = 0;

            // Length check
            if (password.Length >= 6) score++;
            if (password.Length >= 8) score++;

            // Character variety checks
            if (Regex.IsMatch(password, @"[a-z]") && Regex.IsMatch(password, @"[A-Z]")) score++;
            if (Regex.IsMatch(password, @"[0-9]")) score++;
            if (Regex.IsMatch(password, @"[^a-zA-Z0-9]")) score++;

            return Math.Min(score, 4);
        }

        public static string GetPasswordStrengthDescription(int strength)
        {
            return strength switch
            {
                0 => "Introdu parola",
                1 => "Slabă",
                2 => "Mediocră",
                3 => "Bună",
                4 => "Excelentă",
                _ => "Necunoscută"
            };
        }

        #endregion

        #region Date Validation (simplified)

        public static bool IsValidBirthDate(DateTime birthDate)
        {
            // Nu poate fi în viitor
            return birthDate <= DateTime.Today;
        }

        #endregion

        #region Form Validation Helpers

        public static bool IsValidNameForm(string nume, string prenume)
        {
            return IsValidNume(nume) && IsValidPrenume(prenume);
        }

        public static bool IsValidContactForm(string email, string parola, string confirmParola)
        {
            return IsValidEmail(email) &&
                   IsValidPassword(parola) &&
                   DoPasswordsMatch(parola, confirmParola);
        }

        public static bool IsValidEditProfileForm(string nume, string prenume, string email, DateTime birthDate)
        {
            return IsValidNume(nume) &&
                   IsValidPrenume(prenume) &&
                   IsValidEmail(email) &&
                   IsValidBirthDate(birthDate);
        }

        #endregion

        #region Price Range Validation

        public static bool IsValidPriceRange(decimal minPrice, decimal maxPrice)
        {
            return minPrice >= 0 && maxPrice > minPrice;
        }

        public static bool IsValidPrice(decimal price)
        {
            return price >= 0;
        }

        public static string GetPriceRangeValidationMessage(decimal minPrice, decimal maxPrice)
        {
            if (minPrice < 0)
                return "Prețul minim nu poate fi negativ.";

            if (maxPrice < 0)
                return "Prețul maxim nu poate fi negativ.";

            if (maxPrice <= minPrice)
                return $"Prețul maxim ({maxPrice:0.00}€) trebuie să fie mai mare decât prețul minim ({minPrice:0.00}€).";

            return "Intervalul de preț este valid.";
        }

        #endregion

        #region Validation Messages

        public static string GetNameValidationMessage(string fieldName)
        {
            return $"Te rog introdu {fieldName}";
        }

        public static string GetNameLengthValidationMessage(string fieldName, int minLength = 2)
        {
            return $"{fieldName} trebuie să aibă cel puțin {minLength} caractere";
        }

        public static string GetEmailValidationMessage()
        {
            return "Te rog introdu o adresă de email validă";
        }

        public static string GetPasswordValidationMessage()
        {
            return "Te rog introdu o parolă";
        }

        public static string GetPasswordMismatchMessage()
        {
            return "Parolele nu se potrivesc";
        }

        public static string GetFutureDateValidationMessage()
        {
            return "Data nașterii nu poate fi în viitor";
        }

        #endregion
    }
}