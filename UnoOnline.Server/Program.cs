using UnoOnline.Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


// -------- NEW TEMPORARY TEST CODE - GAME MANAGER --------
Console.WriteLine("*****************************************");
Console.WriteLine("***** UNO GAME MANAGER TEST - SERVER START ******");

// 1. Create a GameManager instance
var gameManager = new GameManager(); // Assumes 'using UnoOnline.Server;' is present

// 2. Create some dummy players
var player1 = new Player("p1", "Alice");
var player2 = new Player("p2", "Bob");
var player3 = new Player("p3", "Charlie"); // Optional 3rd player

// 3. Add players to the game
gameManager.AddPlayer(player1);
gameManager.AddPlayer(player2);
gameManager.AddPlayer(player3); // Add Charlie too

// 4. Try to start the game
bool gameStarted = gameManager.StartGame();

if (gameStarted)
{
    Console.WriteLine("\n--- Game Successfully Started ---");
    Console.WriteLine($"Game Running: {gameManager.IsGameRunning}");
    Console.WriteLine($"Current Player: {gameManager.CurrentPlayer?.Name}"); // Use ?. for safety
    Console.WriteLine($"Top Card: {gameManager.CurrentCard}");
}
else
{
    Console.WriteLine("\n--- Failed to Start Game ---");
}


Console.WriteLine("***** END OF UNO GAME MANAGER TEST ************");
Console.WriteLine("*****************************************");
// -------- END OF NEW TEMPORARY TEST CODE --------



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();