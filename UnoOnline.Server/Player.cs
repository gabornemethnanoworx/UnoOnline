﻿using UnoOnline.Shared; // Needed for Card
using System.Collections.Generic;

namespace UnoOnline.Server
{
    public class Player
    {
        /// A unique identifier for the player.
        /// Later, this might be linked to their connection ID or user ID.
        public string Id { get; private set; }

        /// The player's display name.
        public string Name { get; private set; } // Or make it settable if needed

        /// The list of cards currently in the player's hand.
        public List<Card> Hand { get; private set; }

        /// Constructor to create a new player.
        public Player(string id, string name)
        {
            Id = id;
            Name = name;
            Hand = new List<Card>();
        }

        public void AddCardToHand(Card card)
        {
            if (card != null)
            {
                Hand.Add(card);
            }
        }

        public bool RemoveCardFromHand(Card cardToRemove)
        {
            return Hand.Remove(cardToRemove);
        }

        /// Finds a card in the player's hand by its unique ID.
        public Card? FindCardById(Guid cardId)
        {
            return Hand.FirstOrDefault(card => card.Id == cardId);
        }

        // We can add more player-specific logic here later (e.g., HasUnoStatus)
    }
}