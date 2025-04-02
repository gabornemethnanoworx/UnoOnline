using UnoOnline.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnoOnline.Server
{
    /// Manages the state and logic of a single Uno game instance.
    public class GameManager
    {
        private const int InitialHandSize = 7; // Standard Uno starting hand size

        private readonly List<Player> _players;
        private readonly Deck _deck;
        private readonly List<Card> _discardPile;
        private int _currentPlayerIndex;
        private bool _isGameRunning;
        private bool _clockwiseTurnOrder; // True for clockwise, false for counter-clockwise

        /// The card currently on top of the discard pile that players must match.
        /// Can be null if the game hasn't started or the discard pile is empty (shouldn't happen in normal play after start).
        public Card? CurrentCard => _discardPile.LastOrDefault();

        /// The player whose turn it currently is.
        /// Can be null if the game is not running.
        public Player? CurrentPlayer => _isGameRunning && _players.Count > 0 ? _players[_currentPlayerIndex] : null;

        /// Indicates if the game is currently in progress.
        public bool IsGameRunning => _isGameRunning;

        /// Constructor for the GameManager.
        public GameManager()
        {
            _players = new List<Player>();
            _deck = new Deck(); // Creates a new, full deck
            _discardPile = new List<Card>();
            _currentPlayerIndex = 0;
            _isGameRunning = false;
            _clockwiseTurnOrder = true; // Default direction
        }

        /// Adds a player to the game before it starts.
        public bool AddPlayer(Player player)
        {
            if (_isGameRunning || player == null)
            {
                Console.WriteLine($"Game Manager: Cannot add player '{player?.Name}'. Game running: {_isGameRunning}");
                return false;
            }
            // Optional: Check if player with same ID already exists
            if (_players.Any(p => p.Id == player.Id))
            {
                Console.WriteLine($"Game Manager: Player with ID '{player.Id}' already exists.");
                return false;
            }

            _players.Add(player);
            Console.WriteLine($"Game Manager: Player '{player.Name}' added. Total players: {_players.Count}");
            return true;
        }

        // --- We will add more methods here (StartGame, PlayCard, DrawCard, etc.) ---

        /// <summary>
        /// Starts the Uno game. Requires at least 2 players.
        /// Shuffles the deck, deals initial hands, and places the first card.
        /// </summary>
        /// <returns>True if the game started successfully, false otherwise.</returns>
        public bool StartGame()
        {
            // Basic validation
            if (_isGameRunning)
            {
                Console.WriteLine("Game Manager: Game is already running.");
                return false;
            }
            if (_players.Count < 2) // Need at least 2 players for Uno
            {
                Console.WriteLine($"Game Manager: Cannot start game. Need at least 2 players, currently have {_players.Count}.");
                return false;
            }

            Console.WriteLine("Game Manager: Starting game...");

            // 1. Shuffle the deck
            _deck.Shuffle();
            Console.WriteLine("Game Manager: Deck shuffled.");

            // 2. Deal initial hands
            Console.WriteLine($"Game Manager: Dealing {InitialHandSize} cards to each of the {_players.Count} players.");
            for (int i = 0; i < InitialHandSize; i++)
            {
                foreach (var player in _players)
                {
                    Card? drawnCard = _deck.DrawCard();
                    if (drawnCard != null)
                    {
                        player.AddCardToHand(drawnCard);
                    }
                    else
                    {
                        // Should not happen with a standard deck and player count, but good to handle
                        Console.WriteLine("Game Manager Warning: Deck ran out during initial deal!");
                        // Decide how to handle this - maybe throw an exception or return false
                        return false;
                    }
                }
            }

            // Print hands for verification (temporary)
            foreach (var player in _players)
            {
                Console.WriteLine($"--- {player.Name}'s Hand ({player.Hand.Count} cards):");
                foreach (var card in player.Hand.OrderBy(c => c.Color).ThenBy(c => c.Value)) // Sort for readability
                {
                    Console.WriteLine($"   - {card}");
                }
            }


            // 3. Place the first card on the discard pile
            Console.WriteLine("Game Manager: Placing first card on discard pile...");
            Card? firstCard = null;
            do
            {
                firstCard = _deck.DrawCard();
                if (firstCard == null)
                {
                    // Extremely unlikely scenario: deck is empty after dealing?
                    // Might need to reshuffle discard pile back into deck, but let's ignore for now.
                    Console.WriteLine("Game Manager Error: Deck empty when trying to draw the first card!");
                    return false;
                }

                // Rule: The first card cannot be a Wild Draw Four.
                // If it is, put it back in the middle of the deck and draw another.
                if (firstCard.Value == CardValue.WildDrawFour)
                {
                    Console.WriteLine($"Game Manager: First card was {firstCard}. Adding it back to deck and reshuffling.");

                    _deck.AddCardBackAndShuffle(firstCard);

                    firstCard = null; // Force redraw
                }

            } while (firstCard == null);

            _discardPile.Add(firstCard);
            Console.WriteLine($"Game Manager: First card is {CurrentCard}.");

            // Handle first card effects (Skip, Reverse, Draw Two, Wild) - Simplified for now
            // TODO: Implement first card action logic (e.g., if Skip, first player is skipped; if Wild, first player chooses color)
            Console.WriteLine("Game Manager: (TODO: Implement first card action effects)");


            // 4. Set game state
            _isGameRunning = true;
            _currentPlayerIndex = 0; // First player in the list starts
            _clockwiseTurnOrder = true;

            Console.WriteLine($"Game Manager: Game started! Current turn: {_players[_currentPlayerIndex].Name}. Top card: {CurrentCard}");

            return true;
        }

    }
}