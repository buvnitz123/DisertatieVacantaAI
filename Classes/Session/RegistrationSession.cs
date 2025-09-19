namespace MauiAppDisertatieVacantaAI.Classes.Session;

public class RegistrationDraft
{
    public string Nume { get; set; }
    public string Prenume { get; set; }
    public string Email { get; set; }
    public string Parola { get; set; }
    public DateTime? DataNastere { get; set; }
}

public static class RegistrationSession
{
    public static RegistrationDraft Draft { get; private set; }

    public static void EnsureDraft()
    {
        if (Draft == null)
            Draft = new RegistrationDraft();
    }

    public static void SetDraft(RegistrationDraft draft) => Draft = draft;

    public static void SetName(string nume, string prenume)
    {
        EnsureDraft();
        Draft.Nume = nume;
        Draft.Prenume = prenume;
    }

    public static void SetContact(string email, string telefon, string parola)
    {
        EnsureDraft();
        Draft.Email = email;
        Draft.Parola = parola;
    }

    public static void SetBirthDate(DateTime date)
    {
        EnsureDraft();
        Draft.DataNastere = date;
    }

    public static void Clear() => Draft = null;
}