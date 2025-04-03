using UnoOnline.Server; // Access GameManager
using UnoOnline.Shared; // Access Card, Player, Enums, etc.

namespace UnoOnline.Web.Services
{
    /// <summary>
    /// Manages the singleton GameManager instance for the Blazor application.
    /// Provides an interface for UI components to interact with the game logic.
    /// </summary>
    public class GameManagerService
    {
        private GameManager _gameManager;

        // Provides access to the raw GameManager if needed, but prefer using service methods.
        // Use with caution, direct manipulation bypasses service logic.
        public GameManager RawGameManager => _gameManager;

        public GameManagerService()
        {
            _gameManager = new GameManager();
            // Players are now added via the UI/AddPlayer method for the demo
            Console.WriteLine("GameManagerService initialized.");
        }

        /// <summary>
        /// Adds a new player to the game if the game is not running.
        /// </summary>
        /// <param name="id">Unique player ID.</param>
        /// <param name="name">Player display name.</param>
        /// <returns>True if player was added successfully, false otherwise.</returns>
        public bool AddPlayer(string id, string name)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("GameManagerService: AddPlayer failed - invalid id or name.");
                return false;
            }
            // Pass validated data to the GameManager
            return _gameManager.AddPlayer(new Player(id, name));
        }

        /// <summary>
        /// Starts a new game or restarts the current one using the existing players.
        /// </summary>
        /// <returns>True if the game started/restarted successfully, false otherwise.</returns>
        public bool StartGame()
        {
            // StartGame in GameManager now handles resets.
            return _gameManager.StartGame();
        }

        /// <summary>
        /// Retrieves the current state of the game, adapted for UI display.
        /// </summary>
        /// <returns>A GameState object containing UI-relevant information.</returns>
        public GameState GetGameState()
        {
            // Creates a snapshot of the current game state for the UI
            return new GameState
            {
                IsGameRunning = _gameManager.IsGameRunning,
                CurrentCard = _gameManager.CurrentCard,
                CurrentPlayerId = _gameManager.CurrentPlayer?.Id, // Add Current Player ID
                CurrentPlayerName = _gameManager.CurrentPlayer?.Name ?? "N/A",
                Players = _gameManager.Players.Select(p => new PlayerInfo
                {
                    Id = p.Id,
                    Name = p.Name,
                    CardCount = p.Hand.Count,
                    // SECURITY/INFO HIDING: In a real multi-user app, this needs filtering.
                    // Only the requesting user should receive their full hand details.
                    Hand = p.Hand.ToList() // WARNING: Sending all hands for demo purposes ONLY!
                }).ToList(),
                DrawPileCount = _gameManager.DrawPileCount,
                GameMessage = _gameManager.GameMessage ?? "",
                // --- NEW STATE FIELDS ---
                IsAwaitingColorChoice = _gameManager.IsAwaitingColorChoice,
                ChosenWildColor = _gameManager.ChosenWildColor,
                PendingDrawAmount = _gameManager.PendingDrawAmount
            };
        }

        /// <summary>
        /// Retrieves detailed state for a specific player (primarily their hand).
        /// </summary>
        /// <param name="playerId">The ID of the player whose state is requested.</param>
        /// <returns>PlayerInfo for the specified player, or null if not found.</returns>
        public PlayerInfo? GetPlayerState(string playerId)
        {
            var player = _gameManager.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null) return null;

            // In a real app, you'd likely call this only for the authenticated user.
            return new PlayerInfo
            {
                Id = player.Id,
                Name = player.Name,
                CardCount = player.Hand.Count,
                Hand = player.Hand.ToList() // Return the actual hand for this specific player
            };
        }

        /// <summary>
        /// Attempts to play a card for the specified player.
        /// </summary>
        /// <param name="playerId">ID of the player playing.</param>
        /// <param name="cardId">ID of the card being played.</param>
        /// <returns>True if the card was played successfully, false otherwise.</returns>
        public bool AttemptPlayCard(string playerId, Guid cardId)
        {
            return _gameManager.PlayCard(playerId, cardId);
        }

        /// <summary>
        /// Attempts to make the specified player draw card(s).
        /// Handles both regular draws and pending draw penalties.
        /// </summary>
        /// <param name="playerId">ID of the player drawing.</param>
        /// <returns>True if drawing occurred, false otherwise.</returns>
        public bool AttemptDrawCard(string playerId)
        {
            return _gameManager.DrawCard(playerId);
        }

        /// <summary>
        /// Attempts to set the chosen color after a Wild card was played.
        /// </summary>
        /// <param name="playerId">ID of the player choosing the color.</param>
        /// <param name="color">The chosen color (must not be Wild).</param>
        /// <returns>True if the color was set successfully, false otherwise.</returns>
        public bool AttemptSetWildColor(string playerId, CardColor color)
        {
            return _gameManager.SetWildColor(playerId, color);
        }

        // Potential future methods:
        // public bool AttemptPassTurn(string playerId) { ... }
        // public bool AttemptChallengeWildDrawFour(string challengerId) { ... }
        // public bool CallUno(string playerId) { ... }
    }

    // Data Transfer Object (DTO) for sending game state to the Blazor UI
    public class GameState
    {
        public bool IsGameRunning { get; set; }
        public Card? CurrentCard { get; set; }
        public string? CurrentPlayerId { get; set; } // Added ID for more reliable checks
        public string CurrentPlayerName { get; set; } = "N/A";
        public List<PlayerInfo> Players { get; set; } = new();
        public int DrawPileCount { get; set; }
        public string GameMessage { get; set; } = "";
        // --- NEW FIELDS ---
        public bool IsAwaitingColorChoice { get; set; }
        public CardColor? ChosenWildColor { get; set; }
        public int PendingDrawAmount { get; set; }
    }

    // DTO for player information (includes hand for demo, hide in real app)
    public class PlayerInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int CardCount { get; set; }
        // WARNING: Exposing everyone's hand is only for the current single-browser demo!
        public List<Card> Hand { get; set; } = new();
    }
}