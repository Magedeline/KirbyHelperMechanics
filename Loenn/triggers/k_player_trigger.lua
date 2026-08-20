local kPlayerTrigger = {}

kPlayerTrigger.name = "KirbyHelperMechanics/K_PlayerTrigger"
kPlayerTrigger.depth = 0
kPlayerTrigger.placements = {
    name = "k_player_trigger",
    data = {
        targetPlayer = "Kirby",
        spawnKPlayer = true,
        revertOnLeave = "Default",
        flag = "",
        clearFlagOnLeave = true,
        onlyOnce = false,
    }
}

kPlayerTrigger.fieldInformation = {
    targetPlayer = {
        options = {"Kirby", "Madeline"},
        editable = false
    },
    -- "Default" defers to the mod's "Default Revert Player On Trigger Leave"
    -- setting in Mod Options rather than hardcoding true/false per placement.
    revertOnLeave = {
        options = {"Default", "True", "False"},
        editable = false
    }
}

return kPlayerTrigger
