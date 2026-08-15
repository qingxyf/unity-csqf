# Roguelike Card v0.2 Design

## Goal
Make every map node playable with a coherent first-pass roguelike card loop: retained hands, regenerating energy, a permanent fallback attack, multi-pick node rewards, camp choices, direct event costs, collectibles, and a more useful shop.

## Combat Rules
- Player starts combat with 6 energy.
- Player max energy is 10.
- Each new player turn restores 2 energy, capped at 10.
- Combat starts by drawing 5 cards.
- Each new player turn keeps the old hand and draws 2 cards.
- Hand size is capped at 10.
- A permanent basic attack action is available in combat:
  - costs 1 mana
  - deals the player's current base attack damage to a target
  - immediately ends the player turn after a successful hit
- Cards that say they end the turn must request the same full turn transition as the end-turn button.

## Node Rules
- Battle victory shows 3 reward cards and lets the player add up to 2 cards.
- Elite victory shows 4 reward cards, lets the player add up to 2 cards, and grants a collectible.
- Boss victory can complete the node for now; a dedicated win screen can be added later.
- Treasure offers 3 card choices and a skip option.
- Camp offers several mutually exclusive services instead of always granting every benefit.
- Events use direct health payment for negative health choices; shields and damage reduction must not absorb event costs.
- Shop stocks cards, collectibles, and services. It should support buying cards, buying a collectible, removing a card, healing, refreshing stock, and leaving.

## Economy
- v0.2 uses gold as the shop currency.
- Player starts with enough gold to buy at least one low-cost card.
- Card prices scale by mana cost.
- Remove-card service gets more expensive each time it is used.
- Battle and elite nodes grant gold on victory.

## Collectibles
- Collectibles are run-level bonuses.
- v0.2 includes max health, max energy, combat-start shield, elemental damage, first-card elemental discount, and shop discount effects.
- Elite combat grants a collectible.
- Some event choices can grant a collectible; loaded event assets get a fallback collectible path if none is configured.
- Shop offers a collectible for gold.

## Manual Unity Work
- If runtime UI is acceptable, no new scene button placement is required.
- For polished presentation, assign final prefabs to `NodeContentManager` fields in the forth scene.
- If custom art is preferred for camp/shop/reward panels, add prefabs under `Assets/Prefabs/NodeContent/` and bind them on `NodeContentManager`.
