using UnoOnline.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnoOnline.Server
{
    public class GameManager
    {
        // ... (Meglévő konstansok és privát mezők) ...
        private const int InitialHandSize = 7;
        private readonly List<Player> _players;
        private Deck _deck; // Removed readonly to allow re-initialization
        private readonly List<Card> _discardPile;
        private int _currentPlayerIndex;
        private bool _isGameRunning;
        private bool _clockwiseTurnOrder;
        private bool _mustDrawInsteadOfPlaying; // Flag indicating player drew unplayable card LAST turn

        // --- ÚJ ÁLLAPOTOK ---
        private CardColor? _chosenWildColor = null; // Stores the chosen color after a Wild is played
        private bool _awaitingColorChoice = false; // Is the game waiting for the current player to choose a color?
        private int _pendingDrawAmount = 0; // How many cards must the next player draw (stacking)

        // --- PUBLIKUS PROPERTY-K (meglévők + újak) ---
        public Card? CurrentCard => _discardPile.LastOrDefault();
        public Player? CurrentPlayer => _isGameRunning && _players.Count > 0 ? _players[_currentPlayerIndex] : null;
        public bool IsGameRunning => _isGameRunning;
        public string? GameMessage { get; private set; }
        public IReadOnlyList<Player> Players => _players.AsReadOnly();
        public int DrawPileCount => _deck.CardsRemaining;

        // --- ÚJ PUBLIKUS PROPERTY-K ---
        public CardColor? ChosenWildColor => _chosenWildColor; // Expose chosen color
        public bool IsAwaitingColorChoice => _awaitingColorChoice; // Expose waiting state
        public int PendingDrawAmount => _pendingDrawAmount; // Expose pending draw count


        public GameManager()
        {
            _players = new List<Player>();
            _deck = new Deck();
            _discardPile = new List<Card>();
            InitializeGameDefaults(); // Use a helper for defaults
        }

        private void InitializeGameDefaults()
        {
            _currentPlayerIndex = 0;
            _isGameRunning = false;
            _clockwiseTurnOrder = true;
            _mustDrawInsteadOfPlaying = false;
            _chosenWildColor = null;
            _awaitingColorChoice = false;
            _pendingDrawAmount = 0;
            GameMessage = "Game not started. Add players and press Start Game.";
            // Do NOT clear _players list here, only game state
        }


        public bool AddPlayer(Player player)
        {
            // Prevent adding players mid-game more strictly
            if (_isGameRunning)
            {
                Console.WriteLine($"Game Manager: Cannot add player '{player?.Name}'. Game is running.");
                GameMessage = "Cannot add players while game is running.";
                return false;
            }
            if (player == null) return false; // Basic check

            if (_players.Any(p => p.Id == player.Id))
            {
                Console.WriteLine($"Game Manager: Player with ID '{player.Id}' already exists.");
                GameMessage = $"Player {player.Name} already in game.";
                return false;
            }
            if (_players.Count >= 10)
            { // Optional: Max player limit
                GameMessage = "Maximum number of players reached.";
                return false;
            }

            _players.Add(player);
            Console.WriteLine($"Game Manager: Player '{player.Name}' added. Total players: {_players.Count}");
            GameMessage = $"Player {player.Name} joined. Total: {_players.Count}";
            return true;
        }

        /// <summary>
        /// Starts a new game or restarts the current one.
        /// Clears hands, resets state, shuffles, deals, and starts the first turn.
        /// </summary>
        /// <summary>
        /// Starts a new game or restarts the current one.
        /// Clears hands, resets state, shuffles, deals, and starts the first turn,
        /// applying effects of the first card correctly.
        /// </summary>
        public bool StartGame()
        {
            // Validation (minimum players)
            if (_players.Count < 2)
            {
                Console.WriteLine($"Game Manager: Cannot start game. Need at least 2 players, currently have {_players.Count}.");
                GameMessage = $"Need at least 2 players to start (currently {_players.Count}).";
                return false;
            }

            Console.WriteLine("Game Manager: Starting/Restarting game...");

            // 1. Reset Game State
            InitializeGameDefaults();
            _deck = new Deck();
            _deck.Shuffle();
            _discardPile.Clear();
            foreach (var player in _players) { player.Hand.Clear(); }
            Console.WriteLine("Game Manager: State reset, deck shuffled, hands cleared.");

            // 2. Deal initial hands
            Console.WriteLine($"Game Manager: Dealing {InitialHandSize} cards to each of the {_players.Count} players.");
            for (int i = 0; i < InitialHandSize; i++)
            {
                foreach (var player in _players)
                {
                    Card? drawnCard = _deck.DrawCard();
                    if (drawnCard != null) { player.AddCardToHand(drawnCard); }
                    else
                    {
                        Console.WriteLine("Game Manager Warning: Deck ran out during initial deal!");
                        GameMessage = "Error: Deck ran out during initial deal!";
                        return false;
                    }
                }
            }

            // 3. Place the first card
            Console.WriteLine("Game Manager: Placing first card on discard pile...");
            Card? firstCard;
            do
            {
                if (_deck.CardsRemaining == 0 && _discardPile.Count > 0)
                {
                    if (!ReshuffleDiscardPile())
                    {
                        GameMessage = "Error: Not enough cards to start game after reshuffle.";
                        return false;
                    }
                }
                else if (_deck.CardsRemaining == 0)
                {
                    GameMessage = "Error: Deck empty when drawing first card.";
                    return false;
                }
                firstCard = _deck.DrawCard();
                if (firstCard.Value == CardValue.WildDrawFour)
                {
                    Console.WriteLine($"Game Manager: First card was {firstCard}. Adding it back and reshuffling.");
                    _deck.AddCardBackAndShuffle(firstCard);
                    firstCard = null;
                }
            } while (firstCard == null);
            _discardPile.Add(firstCard);
            Console.WriteLine($"Game Manager: First card is {CurrentCard}.");


            // 4. Set Running State & Handle First Card Effects
            _isGameRunning = true;
            _currentPlayerIndex = 0; // Player 0 would normally start

            // --- Apply first card effects ---
            Card topCard = CurrentCard!;
            // Temporarily set player 0 as current for messages, even if skipped
            Player startingPlayer = CurrentPlayer!;
            GameMessage = $"Game started! First card: {topCard}. "; // Initial part of message

            switch (topCard.Value)
            {
                case CardValue.Wild:
                    _awaitingColorChoice = true;
                    // Player 0 needs to choose color first. CurrentPlayerIndex remains 0.
                    GameMessage += $"Player {startingPlayer.Name} must choose a color.";
                    Console.WriteLine(GameMessage);
                    break;

                case CardValue.DrawTwo:
                    _pendingDrawAmount = 2;
                    // Player 0 must deal with the Draw Two. CurrentPlayerIndex remains 0.
                    GameMessage += $"Player {startingPlayer.Name}'s turn. Must Draw 2 or play a Draw card.";
                    Console.WriteLine(GameMessage);
                    break;

                case CardValue.Skip:
                    // Player 0 is skipped. The turn advances by ONE position.
                    Console.WriteLine($"Game Manager: First card Skip! {startingPlayer.Name} is skipped.");
                    AdvanceTurn(); // <<< JAVÍTÁS: skipNextPlayer: false (default)
                    GameMessage += $"{startingPlayer.Name} is skipped. Turn: {CurrentPlayer!.Name}.";
                    Console.WriteLine($"Game Manager: Turn advanced. Current player is now {CurrentPlayer.Name}");
                    break;

                case CardValue.Reverse:
                    if (_players.Count > 2)
                    {
                        // Change direction, then advance ONE step in the new direction.
                        _clockwiseTurnOrder = !_clockwiseTurnOrder;
                        Console.WriteLine($"Game Manager: First card Reverse! Direction reversed (now {(_clockwiseTurnOrder ? "Clockwise" : "Counter-Clockwise")}).");
                        AdvanceTurn(); // Advance 1 step in the new direction
                        GameMessage += $"Direction reversed. Turn: {CurrentPlayer!.Name}.";
                        Console.WriteLine($"Game Manager: Turn advanced. Current player is now {CurrentPlayer.Name}");
                    }
                    else
                    { // Acts as Skip in 2P
                        // Player 0 skips Player 1. Player 0 gets to play immediately.
                        // DO NOT advance the turn index. _currentPlayerIndex remains 0.
                        Console.WriteLine($"Game Manager: First card Reverse (2P)! {startingPlayer.Name} skips opponent.");
                        // Find opponent name for message clarity
                        Player opponent = _players.FirstOrDefault(p => p.Id != startingPlayer.Id)!;
                        GameMessage += $"{opponent.Name} is skipped. Turn: {startingPlayer.Name}.";
                        Console.WriteLine($"Game Manager: Opponent skipped. Current player is still {CurrentPlayer!.Name}");
                        // <<< JAVÍTÁS: Nincs AdvanceTurn() hívás itt >>>
                    }
                    break;

                default: // Number card - game proceeds normally
                    // Player 0 starts. CurrentPlayerIndex remains 0.
                    GameMessage += $"Turn: {startingPlayer.Name}.";
                    Console.WriteLine($"Game Manager: Game started normally. Turn: {CurrentPlayer!.Name}. Top card: {CurrentCard}");
                    break;
            }

            // Ensure the final GameMessage reflects the actual starting player after effects
            // This might override parts of the messages set within the switch, refine if needed.
            // GameMessage = $"Game started. Turn: {CurrentPlayer!.Name}. Top Card: {CurrentCard}. {GameMessage}"; // Example refinement

            return true;
        }


        public bool PlayCard(string playerId, Guid cardId)
        {
            // --- Basic Checks ---
            if (!CanPlayerAct(playerId, out Player? player)) return false;
            if (player == null) return false; // Should be covered by CanPlayerAct

            if (_awaitingColorChoice)
            {
                GameMessage = $"Waiting for {player.Name} to choose a color for the Wild card.";
                return false;
            }

            var cardToPlay = player.FindCardById(cardId);
            if (cardToPlay == null)
            {
                GameMessage = "Card not found in player's hand.";
                return false;
            }

            // --- Check Playability ---
            if (!IsCardPlayable(cardToPlay))
            {
                GameMessage = $"{cardToPlay} cannot be played on {CurrentCard} (Current color: {_chosenWildColor ?? CurrentCard?.Color}, Pending Draw: {_pendingDrawAmount}).";
                return false;
            }


            // --- Execute Play ---
            Console.WriteLine($"Game Manager: {player.Name} attempts to play {cardToPlay}.");
            player.RemoveCardFromHand(cardToPlay);
            _discardPile.Add(cardToPlay);
            _chosenWildColor = null; // Reset chosen color unless the played card is Wild
            _mustDrawInsteadOfPlaying = false; // Player successfully played

            GameMessage = $"{player.Name} played {cardToPlay}.";

            // --- Handle Card Effects & State Changes ---
            bool turnAdvanced = false; // Track if effect handles turn advancement

            switch (cardToPlay.Value)
            {
                case CardValue.Wild:
                case CardValue.WildDrawFour:
                    _awaitingColorChoice = true;
                    // Do NOT apply draw penalty for WD4 yet (wait for color choice)
                    GameMessage += $" {player.Name} must choose a color.";
                    Console.WriteLine(GameMessage);
                    // Turn does NOT advance yet
                    break; // Handled further by SetWildColor

                case CardValue.DrawTwo:
                    _pendingDrawAmount += 2;
                    GameMessage += $" Next player must draw {_pendingDrawAmount} or play a Draw card.";
                    Console.WriteLine($"Game Manager: Draw Two played. Pending draw is now {_pendingDrawAmount}.");
                    AdvanceTurn();
                    turnAdvanced = true;
                    break;

                case CardValue.Skip:
                    GameMessage += " Next player skipped!";
                    Console.WriteLine("Game Manager: Skip played.");
                    AdvanceTurn(skipNextPlayer: true);
                    turnAdvanced = true;
                    break;

                case CardValue.Reverse:
                    if (_players.Count > 2)
                    {
                        _clockwiseTurnOrder = !_clockwiseTurnOrder;
                        GameMessage += " Direction reversed!";
                        Console.WriteLine("Game Manager: Reverse played. Direction reversed.");
                        // Advance turn normally based on new direction
                    }
                    else
                    { // Acts as Skip in 2P
                        GameMessage += " Skipped opponent (Reverse in 2P).";
                        Console.WriteLine("Game Manager: Reverse in 2P played (acts as Skip).");
                        AdvanceTurn(skipNextPlayer: true);
                        turnAdvanced = true;
                    }
                    break;

                default: // Number card
                         // If there was a pending draw, playing a number card means the previous player failed to stack.
                         // This case *shouldn't* be reachable if IsCardPlayable is correct. Log if it happens.
                    if (_pendingDrawAmount > 0)
                    {
                        Console.WriteLine($"WARNING: Player {player.Name} played {cardToPlay} while PendingDraw was {_pendingDrawAmount}. Check IsCardPlayable logic.");
                        _pendingDrawAmount = 0; // Clear penalty as it wasn't enforced? Or should this be an error? Let's clear for now.
                    }
                    break;
            }

            // --- Check Win Condition ---
            if (player.Hand.Count == 0)
            {
                _isGameRunning = false;
                GameMessage = $"{player.Name} wins!";
                Console.WriteLine($"Game Manager: {player.Name} has won the game!");
                return true; // Game Over
            }

            // --- Check Uno State ---
            if (player.Hand.Count == 1)
            {
                GameMessage += $" {player.Name} has UNO!";
                Console.WriteLine($"Game Manager: {player.Name} has UNO!");
                // TODO: Implement UNO calling mechanic
            }

            // --- Advance Turn (if not handled by effect) ---
            if (!_awaitingColorChoice && !turnAdvanced)
            {
                AdvanceTurn();
            }

            return true;
        }

        /// <summary>
        /// Sets the chosen color after a Wild card is played.
        /// </summary>
        public bool SetWildColor(string playerId, CardColor color)
        {
            if (!CanPlayerAct(playerId, out Player? player)) return false;
            if (player == null) return false;


            if (!_awaitingColorChoice)
            {
                GameMessage = "Not currently waiting for a color choice.";
                return false;
            }
            if (color == CardColor.Wild)
            {
                GameMessage = "Cannot choose Wild as the color.";
                return false;
            }

            _chosenWildColor = color;
            _awaitingColorChoice = false;
            Card lastPlayedCard = _discardPile.Last(); // The Wild/WD4 card

            GameMessage = $"{player.Name} chose {color}.";
            Console.WriteLine($"Game Manager: {player.Name} chose color {color} for {lastPlayedCard}.");

            // --- Apply WD4 effect AFTER color is chosen ---
            bool turnAdvanced = false;
            if (lastPlayedCard.Value == CardValue.WildDrawFour)
            {
                _pendingDrawAmount += 4;
                GameMessage += $" Next player must draw {_pendingDrawAmount} or play a Draw card.";
                Console.WriteLine($"Game Manager: Wild Draw Four effect applied. Pending draw is now {_pendingDrawAmount}.");
                AdvanceTurn(skipNextPlayer: true); // Skip the next player
                turnAdvanced = true;
            }

            // Advance turn if not already advanced by WD4 effect
            if (!turnAdvanced)
            {
                AdvanceTurn();
            }

            return true;
        }


        /// <summary>
        /// Checks if a card is playable based on the current game state (top card, chosen wild color, pending draws).
        /// </summary>
        private bool IsCardPlayable(Card cardToPlay)
        {
            Card? topCard = CurrentCard;
            if (topCard == null) return true; // Should only happen before first card dealt

            // --- Rule 1: Handling Pending Draws (Stacking) ---
            if (_pendingDrawAmount > 0)
            {
                // Must play a DrawTwo or WildDrawFour
                if (cardToPlay.Value == CardValue.DrawTwo)
                {
                    // Can play D2 on D2 or WD4
                    return topCard.Value == CardValue.DrawTwo || topCard.Value == CardValue.WildDrawFour;
                }
                if (cardToPlay.Value == CardValue.WildDrawFour)
                {
                    // WD4 can be played on D2 or WD4 (common rule)
                    // TODO: Add official WD4 challenge rule later? For now, always allow if stacking.
                    return topCard.Value == CardValue.DrawTwo || topCard.Value == CardValue.WildDrawFour;
                }
                // Cannot play any other card if draw amount is pending
                return false;
            }

            // --- Rule 2: Normal Play (No Pending Draws) ---
            // Wild cards are always playable (WD4 legality check could be added here if desired)
            if (cardToPlay.Color == CardColor.Wild)
            {
                return true;
            }

            // Check against chosen wild color if applicable
            if (_chosenWildColor.HasValue)
            {
                return cardToPlay.Color == _chosenWildColor.Value;
            }

            // Match color or value of the top card
            if (cardToPlay.Color == topCard.Color || cardToPlay.Value == topCard.Value)
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Player draws card(s). Handles regular draw and pending draw penalties.
        /// </summary>
        public bool DrawCard(string playerId)
        {
            if (!CanPlayerAct(playerId, out Player? player)) return false;
            if (player == null) return false;

            if (_awaitingColorChoice)
            {
                GameMessage = $"Waiting for {player.Name} to choose a color.";
                return false;
            }

            // --- Handle Pending Draw Penalty ---
            if (_pendingDrawAmount > 0)
            {
                Console.WriteLine($"Game Manager: {player.Name} must draw {_pendingDrawAmount} cards.");
                int cardsDrawn = 0;
                for (int i = 0; i < _pendingDrawAmount; i++)
                {
                    // Ensure deck has cards, reshuffle if needed
                    if (_deck.CardsRemaining == 0)
                    {
                        if (!ReshuffleDiscardPile())
                        {
                            GameMessage = $"Error: Cannot draw required cards. Deck empty after reshuffle attempt.";
                            Console.WriteLine("Game Manager Error: Deck empty during forced draw, reshuffle failed.");
                            // Game might be stuck here?
                            _pendingDrawAmount = 0; // Clear penalty anyway?
                            return false;
                        }
                    }
                    Card? drawn = _deck.DrawCard();
                    if (drawn != null)
                    {
                        player.AddCardToHand(drawn);
                        cardsDrawn++;
                    }
                    else
                    {
                        // Should not happen after reshuffle check
                        Console.WriteLine("Game Manager Error: DrawCard returned null unexpectedly during penalty draw.");
                        break; // Stop drawing if error occurs
                    }
                }

                GameMessage = $"{player.Name} drew {cardsDrawn} cards due to penalty. Turn skipped.";
                Console.WriteLine(GameMessage);

                // Reset penalty and wild color state
                int amountDrawn = _pendingDrawAmount; // Store before resetting
                _pendingDrawAmount = 0;
                _chosenWildColor = null;

                AdvanceTurn(); // Skip the player who just drew the penalty

                return true; // Indicate successful penalty draw
            }

            // --- Handle Regular Draw (Draw 1 Card) ---
            Console.WriteLine($"Game Manager: {player.Name} draws a card.");
            if (_deck.CardsRemaining == 0)
            {
                if (!ReshuffleDiscardPile())
                {
                    GameMessage = "Draw pile empty, cannot reshuffle discard pile.";
                    Console.WriteLine("Game Manager: Deck empty, cannot draw or reshuffle.");
                    return false;
                }
            }

            Card? drawnCard = _deck.DrawCard();
            if (drawnCard != null)
            {
                player.AddCardToHand(drawnCard);
                Console.WriteLine($"Game Manager: {player.Name} drew {drawnCard}.");

                // Simplified Rule: After a player draws a card (not as penalty), their turn ends.
                // We remove the option to immediately play the drawn card for simplicity for now.
                if (IsCardPlayable(drawnCard))
                {
                    GameMessage = $"{player.Name} drew {drawnCard}. Turn ends.";
                }
                else
                {
                    GameMessage = $"{player.Name} drew {drawnCard} (unplayable). Turn ends.";
                }

                // ALWAYS advance the turn after a successful regular draw
                AdvanceTurn();

                return true;
            }
            else
            {
                GameMessage = "Error: Failed to draw card.";
                Console.WriteLine("Game Manager Error: DrawCard returned null unexpectedly.");
                return false;
            }
        }

        /// <summary>
        /// Helper to check if a player can perform an action (is it their turn, is game running?).
        /// Sets GameMessage on failure.
        /// </summary>
        private bool CanPlayerAct(string playerId, out Player? player)
        {
            player = null;
            if (!_isGameRunning)
            {
                GameMessage = "Game is not running.";
                return false;
            }
            player = _players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
            {
                GameMessage = "Player not found.";
                return false;
            }
            if (player != CurrentPlayer)
            {
                GameMessage = $"It's not {player.Name}'s turn (it's {CurrentPlayer?.Name}'s turn).";
                return false;
            }
            return true;
        }


        /// <summary>
        /// Moves to the next player's turn, optionally skipping them.
        /// Clears turn-specific states like _mustDrawInsteadOfPlaying.
        /// Does NOT clear _pendingDrawAmount or _chosenWildColor here (those persist until resolved).
        /// </summary>
        private void AdvanceTurn(bool skipNextPlayer = false)
        {
            if (!_isGameRunning || _players.Count == 0) return;

            int direction = _clockwiseTurnOrder ? 1 : -1;
            int playersToAdvance = skipNextPlayer ? 2 : 1;

            _currentPlayerIndex = (_currentPlayerIndex + (direction * playersToAdvance) + _players.Count * playersToAdvance) % _players.Count; // Ensure positive index

            // Reset states for the *new* player's turn
            _mustDrawInsteadOfPlaying = false;
            // Do NOT reset _pendingDrawAmount or _chosenWildColor here
            // Do NOT reset _awaitingColorChoice here (it's cleared by SetWildColor)

            // Update game message for the new turn
            GameMessage = $"Turn: {CurrentPlayer?.Name}. Top card: {CurrentCard}";
            if (_pendingDrawAmount > 0)
            {
                GameMessage += $" Must DRAW {_pendingDrawAmount} or play matching Draw card!";
            }
            else if (_chosenWildColor.HasValue)
            {
                GameMessage += $" Color is {_chosenWildColor.Value}.";
            }
            Console.WriteLine($"Game Manager: Advanced turn. Current player is now {CurrentPlayer?.Name}. Message: {GameMessage}");
        }

        /// <summary>
        /// Reshuffles the discard pile (except the top card) back into the draw pile.
        /// </summary>
        /// <returns>True if reshuffle happened, false if not possible.</returns>
        private bool ReshuffleDiscardPile()
        {
            Console.WriteLine("Game Manager: Draw pile empty. Reshuffling discard pile...");
            if (_discardPile.Count <= 1)
            {
                Console.WriteLine("Game Manager Warning: Not enough cards in discard pile to reshuffle.");
                return false; // Cannot reshuffle if only 0 or 1 card is there
            }

            Card topCard = _discardPile.Last();
            _discardPile.RemoveAt(_discardPile.Count - 1);

            List<Card> cardsToReshuffle = new List<Card>(_discardPile);
            _discardPile.Clear();
            _discardPile.Add(topCard); // Put the top card back

            // Add cards back to deck THEN shuffle once (more efficient)
            _deck.AddCardsBack(cardsToReshuffle); // Need to add this method to Deck.cs
            _deck.Shuffle();

            Console.WriteLine($"Game Manager: Reshuffled {cardsToReshuffle.Count} cards into deck. Deck now has {_deck.CardsRemaining} cards.");
            GameMessage = "Reshuffled discard pile into draw pile.";
            return true;
        }

        // TODO: Add method PassTurn(string playerId) - Needed if player draws playable card but chooses not to play
        // TODO: Add WD4 Challenge logic?
    }
}