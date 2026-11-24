local kPlayerHealthTrigger = {}

kPlayerHealthTrigger.name = "KirbyHelperMechanics/K_PlayerHealthTrigger"
kPlayerHealthTrigger.depth = 0
kPlayerHealthTrigger.placements = {
    name = "k_player_health_trigger",
    data = {
        enableHealth = true,
        maxHealth = 6,
        healAmount = 0,
        fullHeal = false,
        setRespawnPoint = false,
        onlyOnce = true,
    }
}

kPlayerHealthTrigger.fieldInformation = {
    maxHealth = {
        fieldType = "integer",
        minimumValue = 1
    },
    healAmount = {
        fieldType = "integer",
        minimumValue = 0
    }
}

return kPlayerHealthTrigger
