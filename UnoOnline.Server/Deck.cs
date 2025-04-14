using UnoOnline.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnoOnline.Server
{
    public class Deck
    {
        private List<Card> _cards;
        private Random _random;

        public int CardsRemaining => _cards.Count;

        public Deck()
        {
            // Use a shared Random instance if created frequently, but for a single deck, this is fine.
            _random = new Random();
            InitializeDeck(); // Initialize on creation
        }

        /// <summary>
        /// Initializes or resets the deck to a full, standard 108-card Uno deck.
        /// </summary>
        public void InitializeDeck()
        {
            _cards = new List<Card>(108); // Pre-allocate capacity for efficiency
            var colors = new[] { CardColor.Red, CardColor.Yellow, CardColor.Green, CardColor.Blue };

            foreach (var color in colors)
            {
                // 1x Zero card per color
                _cards.Add(new Card(color, CardValue.Zero));

                // 2x 1-9 cards per color
                for (int i = 1; i <= 9; i++)
                {
                    _cards.Add(new Card(color, (CardValue)i));
                    _cards.Add(new Card(color, (CardValue)i));
                }

                // 2x Skip, Reverse, DrawTwo cards per color
                _cards.Add(new Card(color, CardValue.Skip));
                _cards.Add(new Card(color, CardValue.Skip));
                _cards.Add(new Card(color, CardValue.Reverse));
                _cards.Add(new Card(color, CardValue.Reverse));
                _cards.Add(new Card(color, CardValue.DrawTwo));
                _cards.Add(new Card(color, CardValue.DrawTwo));
            }

            // 4x Wild cards
            for (int i = 0; i < 4; i++)
            {
                _cards.Add(new Card(CardColor.Wild, CardValue.Wild));
            }

            // 4x Wild Draw Four cards
            for (int i = 0; i < 4; i++)
            {
                _cards.Add(new Card(CardColor.Wild, CardValue.WildDrawFour));
            }
            // Total: 4 * (1 + 2*9 + 2*3) + 4 + 4 = 108 cards
            Console.WriteLine($"--- Deck Initialized ({_cards.Count} cards) ---");
        }

        /// <summary>
        /// Shuffles the deck using the Fisher-Yates (inside-out) algorithm.
        /// </summary>
        public void Shuffle()
        {
            int n = _cards.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                // Swap cards[k] and cards[n]
                Card value = _cards[k];
                _cards[k] = _cards[n];
                _cards[n] = value;
            }
            Console.WriteLine($"--- Deck Shuffled ({_cards.Count} cards) ---");
        }

        /// <summary>
        /// Draws a single card from the top of the deck.
        /// </summary>
        /// <returns>The drawn card, or null if the deck is empty.</returns>
        public Card? DrawCard()
        {
            if (CardsRemaining > 0)
            {
                // Draw from the "end" of the list (like taking the top card)
                Card drawnCard = _cards[CardsRemaining - 1];
                _cards.RemoveAt(CardsRemaining - 1);
                return drawnCard;
            }
            else
            {
                Console.WriteLine("--- Deck is empty! Cannot draw. ---");
                return null;
            }
        }

        /// <summary>
        /// Prints the current state of the deck to the console (for debugging).
        /// </summary>
        public void PrintDeckToConsole()
        {
            Console.WriteLine($"--- Current Deck ({CardsRemaining} cards) ---");
            // Print in reverse to see "top" card first if desired
            // for (int i = _cards.Count - 1; i >= 0; i--) { Console.WriteLine(_cards[i].ToString()); }
            foreach (var card in _cards) { Console.WriteLine(card.ToString()); }
            Console.WriteLine("------------------------------------");
        }

        /// <summary>
        /// Adds a single card back into the deck. Does NOT shuffle.
        /// </summary>
        public void AddCardBack(Card card)
        {
            if (card != null)
            {
                _cards.Add(card);
                // No shuffle here, intended for bulk adds before a single shuffle
            }
        }

        /// <summary>
        /// Adds a collection of cards back into the deck. Does NOT shuffle automatically.
        /// Primarily used for reshuffling the discard pile.
        /// </summary>
        public void AddCardsBack(IEnumerable<Card> cardsToAdd)
        {
            if (cardsToAdd != null)
            {
                _cards.AddRange(cardsToAdd);
                Console.WriteLine($"Deck: Added {cardsToAdd.Count()} cards back. Total now: {_cards.Count}");
            }
        }

        /// <summary>
        /// Adds a single card back into the deck AND immediately reshuffles.
        /// Useful for specific rules like putting back a Wild Draw Four during setup.
        /// Less efficient than AddCardsBack + Shuffle for multiple cards.
        /// </summary>
        public void AddCardBackAndShuffle(Card card)
        {
            if (card != null)
            {
                _cards.Add(card);
                Shuffle(); // Reshuffle immediately
                Console.WriteLine($"Deck: Added {card} back and reshuffled.");
            }
        }
    }
}