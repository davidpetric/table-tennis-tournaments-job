using TenisDeMasa.Worker.Application;
using TenisDeMasa.Worker.Application.Discord;
using TenisDeMasa.Worker.Application.TenisDeMasaForumScrapper;

var builder = Host.CreateApplicationBuilder(args);

builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions
{
    ValidateOnBuild = true,
    ValidateScopes = true
}));

builder.Services.AddDbContext<ApplicationDbContext>(x => x.UseSqlite("Data Source=My.db"));

builder.Services.AddLogging(builder => builder.AddConsole());

const GatewayIntents intents = GatewayIntents.Guilds |
                               GatewayIntents.GuildMessages |
                               GatewayIntents.GuildMessageReactions |
                               GatewayIntents.GuildMembers |
                               GatewayIntents.MessageContent |
                               GatewayIntents.GuildPresences;

builder.Services.AddSingleton(
    new DiscordRestClient(new DiscordRestConfig()
    {
        LogLevel = LogSeverity.Debug,
        RestClientProvider = DefaultRestClientProvider.Create(true)
    }));

builder.Services.AddHostedService<Worker>();

builder.Services.AddScoped<Scraper>();
builder.Services.AddScoped<DiscordGuild>();


var host = builder.Build();
host.Run();
