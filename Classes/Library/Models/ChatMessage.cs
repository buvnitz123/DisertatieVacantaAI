using System.ComponentModel;
using System.Runtime.CompilerServices;
using MauiAppDisertatieVacantaAI.Classes.Library.Utils; // ✅ NOU

namespace MauiAppDisertatieVacantaAI.Classes.Library.Models;

public class ChatMessage : INotifyPropertyChanged
{
    private string _text = string.Empty;
    private bool _isUser;
    private bool _isTyping = false;
    private DateTime _timestamp = DateTime.Now;

    public string Text 
    { 
        get => _text; 
        set 
        { 
            if (_text != value) 
            { 
                _text = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(FormattedText)); // ✅ NOU - notifică FormattedText
            } 
        } 
    }

    // ✅ NOU - Proprietate pentru text formatat cu Markdown
    public FormattedString FormattedText => MarkdownParser.ParseToFormattedString(Text);

    public bool IsUser 
    { 
        get => _isUser; 
        set 
        { 
            if (_isUser != value) 
            { 
                _isUser = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(BubbleColor));
                OnPropertyChanged(nameof(HorizontalAlignment));
            } 
        } 
    }

    public bool IsTyping 
    { 
        get => _isTyping; 
        set 
        { 
            if (_isTyping != value) 
            { 
                _isTyping = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(BubbleColor));
            } 
        } 
    }

    public DateTime Timestamp 
    { 
        get => _timestamp; 
        set 
        { 
            if (_timestamp != value) 
            { 
                _timestamp = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(TimeString));
            } 
        } 
    }

    public string TimeString => Timestamp.ToString("HH:mm:ss");
    public string BubbleColor => IsUser ? (Application.Current?.Resources["PrimaryBlue"] as Color)?.ToHex() ?? "#0092ca" : 
                                 IsTyping ? "#95A5A6" : "#444444";
    public LayoutOptions HorizontalAlignment => IsUser ? LayoutOptions.End : LayoutOptions.Start;

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}