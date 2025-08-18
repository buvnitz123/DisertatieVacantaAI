using System.Diagnostics;

namespace MauiAppDisertatieVacantaAI
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                Debug.WriteLine($"Clicked {count} time");
            else
                Debug.WriteLine($"Clicked {count} times");

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }

}
