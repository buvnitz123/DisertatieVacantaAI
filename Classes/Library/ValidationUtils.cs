using System.Text.RegularExpressions;

namespace MauiAppDisertatieVacantaAI.Classes.Library
{
    /// <summary>
    /// Clas? utilitar pentru valid?ri comune în aplica?ie
    /// </summary>
    public static class ValidationUtils
    {
        #region Email Validation

        /// <summary>
        /// Valideaz? formatul unei adrese de email
        /// </summary>
        /// <param name="email">Adresa de email de validat</param>
        /// <returns>True dac? email-ul este valid, false altfel</returns>
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

        /// <summary>
        /// Valideaz? un nume (nume de familie sau prenume)
        /// </summary>
        /// <param name="name">Numele de validat</param>
        /// <param name="minLength">Lungimea minim? (default: 2)</param>
        /// <param name="maxLength">Lungimea maxim? (default: 50)</param>
        /// <returns>True dac? numele este valid, false altfel</returns>
        public static bool IsValidName(string name, int minLength = 2, int maxLength = 50)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var trimmedName = name.Trim();
            return trimmedName.Length >= minLength && trimmedName.Length <= maxLength;
        }

        /// <summary>
        /// Valideaz? numele de familie
        /// </summary>
        /// <param name="nume">Numele de familie</param>
        /// <returns>True dac? numele este valid, false altfel</returns>
        public static bool IsValidNume(string nume)
        {
            return IsValidName(nume);
        }

        /// <summary>
        /// Valideaz? prenumele
        /// </summary>
        /// <param name="prenume">Prenumele</param>
        /// <returns>True dac? prenumele este valid, false altfel</returns>
        public static bool IsValidPrenume(string prenume)
        {
            return IsValidName(prenume);
        }

        #endregion

        #region Password Validation

        /// <summary>
        /// Verific? dac? parola nu este goal? (f?r? alte restric?ii)
        /// </summary>
        /// <param name="password">Parola de validat</param>
        /// <returns>True dac? parola nu este goal?, false altfel</returns>
        public static bool IsValidPassword(string password)
        {
            return !string.IsNullOrWhiteSpace(password);
        }

        /// <summary>
        /// Verific? dac? dou? parole se potrivesc
        /// </summary>
        /// <param name="password">Parola original?</param>
        /// <param name="confirmPassword">Confirmarea parolei</param>
        /// <returns>True dac? parolele se potrivesc, false altfel</returns>
        public static bool DoPasswordsMatch(string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
                return false;

            return password == confirmPassword;
        }

        /// <summary>
        /// Calculeaz? puterea unei parole pe o scal? de 0-4 (pentru UI, nu pentru validare)
        /// </summary>
        /// <param name="password">Parola de evaluat</param>
        /// <returns>Scorul puterii parolei (0-4)</returns>
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

        /// <summary>
        /// Ob?ine descrierea puterii parolei în român? (pentru UI)
        /// </summary>
        /// <param name="strength">Scorul puterii (0-4)</param>
        /// <returns>Descrierea în român?</returns>
        public static string GetPasswordStrengthDescription(int strength)
        {
            return strength switch
            {
                0 => "Introdu parola",
                1 => "Slab?",
                2 => "Mediocr?",
                3 => "Bun?",
                4 => "Excelent?",
                _ => "Necunoscut?"
            };
        }

        #endregion

        #region Date Validation (simplified)

        /// <summary>
        /// Valideaz? c? data nu este în viitor
        /// </summary>
        /// <param name="birthDate">Data na?terii</param>
        /// <returns>True dac? data nu este în viitor, false altfel</returns>
        public static bool IsValidBirthDate(DateTime birthDate)
        {
            // Nu poate fi în viitor
            return birthDate <= DateTime.Today;
        }

        #endregion

        #region Form Validation Helpers

        /// <summary>
        /// Valideaz? un formular complet de înregistrare (nume + prenume)
        /// </summary>
        /// <param name="nume">Numele de familie</param>
        /// <param name="prenume">Prenumele</param>
        /// <returns>True dac? formularul este valid, false altfel</returns>
        public static bool IsValidNameForm(string nume, string prenume)
        {
            return IsValidNume(nume) && IsValidPrenume(prenume);
        }

        /// <summary>
        /// Valideaz? un formular de contact complet (f?r? telefon)
        /// </summary>
        /// <param name="email">Adresa de email</param>
        /// <param name="parola">Parola</param>
        /// <param name="confirmParola">Confirmarea parolei</param>
        /// <returns>True dac? formularul este valid, false altfel</returns>
        public static bool IsValidContactForm(string email, string parola, string confirmParola)
        {
            return IsValidEmail(email) &&
                   IsValidPassword(parola) &&
                   DoPasswordsMatch(parola, confirmParola);
        }

        /// <summary>
        /// Valideaz? un formular complet de editare profil (f?r? telefon ?i f?r? restric?ii de vârst?)
        /// </summary>
        /// <param name="nume">Numele de familie</param>
        /// <param name="prenume">Prenumele</param>
        /// <param name="email">Adresa de email</param>
        /// <param name="birthDate">Data na?terii</param>
        /// <returns>True dac? formularul este valid, false altfel</returns>
        public static bool IsValidEditProfileForm(string nume, string prenume, string email, DateTime birthDate)
        {
            return IsValidNume(nume) &&
                   IsValidPrenume(prenume) &&
                   IsValidEmail(email) &&
                   IsValidBirthDate(birthDate);
        }

        #endregion

        #region Price Range Validation

        /// <summary>
        /// Valideaz? un interval de pre?
        /// </summary>
        /// <param name="minPrice">Pre?ul minim</param>
        /// <param name="maxPrice">Pre?ul maxim</param>
        /// <returns>True dac? intervalul este valid, false altfel</returns>
        public static bool IsValidPriceRange(decimal minPrice, decimal maxPrice)
        {
            return minPrice >= 0 && maxPrice > minPrice;
        }

        /// <summary>
        /// Valideaz? c? un pre? este valid (non-negativ)
        /// </summary>
        /// <param name="price">Pre?ul de validat</param>
        /// <returns>True dac? pre?ul este valid, false altfel</returns>
        public static bool IsValidPrice(decimal price)
        {
            return price >= 0;
        }

        /// <summary>
        /// Ob?ine mesajul de eroare pentru interval de pre? invalid
        /// </summary>
        /// <param name="minPrice">Pre?ul minim</param>
        /// <param name="maxPrice">Pre?ul maxim</param>
        /// <returns>Mesajul de eroare</returns>
        public static string GetPriceRangeValidationMessage(decimal minPrice, decimal maxPrice)
        {
            if (minPrice < 0)
                return "Pre?ul minim nu poate fi negativ.";
            
            if (maxPrice < 0)
                return "Pre?ul maxim nu poate fi negativ.";
                
            if (maxPrice <= minPrice)
                return $"Pre?ul maxim ({maxPrice:0.00}€) trebuie s? fie mai mare decât pre?ul minim ({minPrice:0.00}€).";
                
            return "Intervalul de pre? este valid.";
        }

        #endregion

        #region Validation Messages

        /// <summary>
        /// Ob?ine mesajul de eroare pentru validarea numelui
        /// </summary>
        /// <param name="fieldName">Numele câmpului (ex: "numele", "prenumele")</param>
        /// <returns>Mesajul de eroare</returns>
        public static string GetNameValidationMessage(string fieldName)
        {
            return $"Te rog introdu {fieldName}";
        }

        /// <summary>
        /// Ob?ine mesajul de eroare pentru lungimea minim? a numelui
        /// </summary>
        /// <param name="fieldName">Numele câmpului</param>
        /// <param name="minLength">Lungimea minim?</param>
        /// <returns>Mesajul de eroare</returns>
        public static string GetNameLengthValidationMessage(string fieldName, int minLength = 2)
        {
            return $"{fieldName} trebuie s? aib? cel pu?in {minLength} caractere";
        }

        /// <summary>
        /// Ob?ine mesajul de eroare pentru email invalid
        /// </summary>
        /// <returns>Mesajul de eroare</returns>
        public static string GetEmailValidationMessage()
        {
            return "Te rog introdu o adres? de email valid?";
        }

        /// <summary>
        /// Ob?ine mesajul de eroare pentru parol? goal?
        /// </summary>
        /// <returns>Mesajul de eroare</returns>
        public static string GetPasswordValidationMessage()
        {
            return "Te rog introdu o parol?";
        }

        /// <summary>
        /// Ob?ine mesajul de eroare pentru parolele care nu se potrivesc
        /// </summary>
        /// <returns>Mesajul de eroare</returns>
        public static string GetPasswordMismatchMessage()
        {
            return "Parolele nu se potrivesc";
        }

        /// <summary>
        /// Ob?ine mesajul de eroare pentru data în viitor
        /// </summary>
        /// <returns>Mesajul de eroare</returns>
        public static string GetFutureDateValidationMessage()
        {
            return "Data na?terii nu poate fi în viitor";
        }

        #endregion
    }
}