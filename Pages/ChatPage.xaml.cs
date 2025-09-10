using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MauiAppDisertatieVacantaAI.Pages;

public class ChatMessage : INotifyPropertyChanged
{
    public string Text { get; set; }
    public bool IsUser { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string TimeString => Timestamp.ToString("HH:mm");
    public string BubbleColor => IsUser ? (Application.Current?.Resources["PrimaryBlue"] as Color)?.ToHex() ?? "#0092ca" : "#444444";
    public LayoutOptions HorizontalAlignment => IsUser ? LayoutOptions.End : LayoutOptions.Start;
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class ChatPage : ContentPage
{
    private readonly ObservableCollection<ChatMessage> _messages = new();

    public ChatPage()
    {
        InitializeComponent();
        MessagesView.BindingContext = _messages;
        _messages.Add(new ChatMessage { Text = "Salut! Spune-mi orice - raspunsurile AI vin in curand.", IsUser = false });
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        var text = MessageEntry.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        MessageEntry.Text = string.Empty;
        AddMessageAnimated(new ChatMessage { Text = text, IsUser = true });

        await Task.Delay(600);
        AddMessageAnimated(new ChatMessage { Text = "Coming soon, stay tuned", IsUser = false });
    }

    private void AddMessageAnimated(ChatMessage msg)
    {
        _messages.Add(msg);
        MessagesView.ScrollTo(msg, position: ScrollToPosition.End, animate: true);
    }
}