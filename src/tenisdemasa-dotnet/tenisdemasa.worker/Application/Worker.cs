namespace TenisDeMasa.Worker.Application;

using TenisDeMasa.Worker.Application.Discord;
using TenisDeMasa.Worker.Application.TenisDeMasaForumScrapper;

public class Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
    : IHostedService, IDisposable
{
    public async Task StartAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Incepe executia: {time}", DateTimeOffset.Now);

            try
            {
                // Worker is a singleton class
                using var scope = serviceProvider.CreateScope();
                var scraper = scope.ServiceProvider.GetRequiredService<Scraper>();
                var discord = scope.ServiceProvider.GetRequiredService<DiscordGuild>();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var turnee = await scraper.ScrapeAndExtractInfo();

                var turneeNoiDeAdaugat = new List<Tournament>();
                foreach (var turneu in turnee)
                {
                    Tournament? existing = await dbContext.Tournaments.FirstOrDefaultAsync(x => x.Url == turneu.Url, cancellationToken: stoppingToken);
                    if (existing is null)
                    {
                        turneeNoiDeAdaugat.Add(turneu);
                        logger.LogInformation("Turneu nou: {@turneu}", turneu);

                        await discord.SendMessageAsync(turneu.Url, turneu.Location);
                    }
                    else
                    {
                        if (string.Equals(turneu.Url, existing.Url, StringComparison.InvariantCultureIgnoreCase)
                            && string.Equals(turneu.Date, existing.Date, StringComparison.InvariantCultureIgnoreCase)
                            && string.Equals(turneu.Category, existing.Category, StringComparison.InvariantCultureIgnoreCase)
                            && string.Equals(turneu.Time, existing.Time, StringComparison.InvariantCultureIgnoreCase))
                        {
                            logger.LogInformation("Turneu existent {url}", turneu.Url);
                        }
                        else
                        {
                            existing.LastPostBy = turneu.LastPostBy;
                            existing.Date = turneu.Date;
                            existing.Category = turneu.Category;

                            logger.LogInformation("Turneu existent, noi schimbari: {@turneu}", existing);

                            await discord.SendMessageAsync(turneu.Url, "");
                        }
                    }
                }

                await dbContext.AddRangeAsync(turneeNoiDeAdaugat, stoppingToken);
                await dbContext.SaveChangesAsync(stoppingToken);

                logger.LogInformation("Salvez schimbarile");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Timed Hosted Service is stopping.");


        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}
