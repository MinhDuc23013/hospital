using FakePaymentProvider;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<PendingTransactionStore>();
builder.Services.AddHttpClient();

var app = builder.Build();
app.MapControllers();
app.Run();
