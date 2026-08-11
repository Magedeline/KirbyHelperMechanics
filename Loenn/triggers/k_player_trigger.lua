local kPlayerTrigger = {}

kPlayerTrigger.name = "KirbyHelperMechanics/K_PlayerTrigger"
kPlayerTrigger.depth = 0
kPlayerTrigger.placements = {
    name = "k_player_trigger",
    data = {
        spawnKPlayer = true,
        flag = "",
        clearFlagOnLeave = true,
        onlyOnce = false,
    }
}

return kPlayerTrigger
