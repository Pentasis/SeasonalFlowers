# Seasonal Flowers
Tired of seeing vibrant flowers bloom year-round in Vintage Story, breaking your immersion? Seasonal Flowers changes that! This mod introduces realistic seasonal life cycles to vanilla flowers, making them grow, flower, wither, and hibernate according to the in-game calendar.

Flowers now grow, bloom, wither and hibernate. I tried to follow each type of flowers' real-life cycle. To get some colour in winter I assumed heather to be the winter-variant (Erica).

Changes only occur at night so as not to break immersion when they transition suddenly.

## Performance
Performance impact should be minimal if at all. There is an extra game tick on the server that runs only once every minute and re-rendering the flowers is done using the game's own re-tesselation loop.

## Important Note for Mod Developers
This mod makes fundamental changes to vanilla flower block definitions.
It adds a new phase variant to all vanilla flower blocks (e.g., flower-wilddaisy-free-flowering). This alters the expected variant structure of `game:blocktypes/plant/flower.json`.
If your mod patches this file you need to provide a compatibility patch for this mod as well.
(this also applies to flower-lupine.json but NOT rafflesia.json)

