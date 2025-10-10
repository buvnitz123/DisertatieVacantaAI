using Microsoft.Extensions.Logging;
using MauiAppDisertatieVacantaAI.Classes.Library.Interfaces;
using MauiAppDisertatieVacantaAI.Classes.Library.Services;

namespace MauiAppDisertatieVacantaAI;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
                fonts.AddFont("RobotoFlex-VariableFont.ttf", "RobotoFlex");
            });

		// Register notification service
		builder.Services.AddSingleton<INotificationService, NotificationService>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
