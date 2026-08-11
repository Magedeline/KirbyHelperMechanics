local kPlayerRefill = {}

kPlayerRefill.name = "KirbyHelperMechanics/K_PlayerRefill"
kPlayerRefill.depth = -100
kPlayerRefill.justification = {0.5, 0.5}
kPlayerRefill.texture = "objects/refill/idle00"
kPlayerRefill.placements = {
    name = "k_player_refill",
    data = {
        oneUse = false,
        refillDash = true,
        refillStamina = true,
        refillHealth = true,
        respawnTime = 2.5,
    }
}

return kPlayerRefill
