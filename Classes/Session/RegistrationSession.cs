namespace MauiAppDisertatieVacantaAI.Classes.Session;

public class RegistrationDraft
{
    public string Nume { get; set; }
    public string Prenume { get; set; }
    public string Email { get; set; }
    public string Parola { get; set; }
    public DateTime DataNastere { get; set; }
    public string Telefon { get; set; }
}

public static class RegistrationSession
{
    public static RegistrationDraft Draft { get; private set; }

    public static void SetDraft(RegistrationDraft draft) => Draft = draft;

    public static void Clear() => Draft = null;
}