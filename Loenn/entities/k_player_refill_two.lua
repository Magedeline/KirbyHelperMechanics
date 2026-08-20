local kPlayerRefillTwo = {}

kPlayerRefillTwo.name = "KirbyHelperMechanics/K_PlayerRefillTwo"
kPlayerRefillTwo.depth = -100
kPlayerRefillTwo.justification = {0.5, 0.5}
kPlayerRefillTwo.texture = "objects/refillTwo/idle00"
kPlayerRefillTwo.placements = {
    name = "k_player_refill_two",
    data = {
        oneUse = false,
        refillHealth = true,
        respawnTime = 2.5,
    }
}

return kPlayerRefillTwo
