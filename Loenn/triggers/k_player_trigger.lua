local kPlayerTrigger = {}

kPlayerTrigger.name = "KirbyHelperMechanics/K_PlayerTrigger"
kPlayerTrigger.depth = 0
kPlayerTrigger.placements = {
    name = "k_player_trigger",
    data = {
        targetPlayer = "Kirby",
        spawnKPlayer = true,
        revertOnLeave = false,
        flag = "",
        clearFlagOnLeave = true,
        onlyOnce = false,
    }
}

kPlayerTrigger.fieldInformation = {
    targetPlayer = {
        options = {"Kirby", "Madeline"},
        editable = false
    }
}

return kPlayerTrigger
