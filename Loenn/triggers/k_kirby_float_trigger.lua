local kKirbyFloatTrigger = {}

kKirbyFloatTrigger.name = "KirbyHelperMechanics/K_KirbyFloatTrigger"
kKirbyFloatTrigger.depth = 0
kKirbyFloatTrigger.placements = {
    name = "k_kirby_float_trigger",
    data = {
        floatEnabled = true,
        maxDuration = 1.5,
        maxFlaps = 0,
        resetOnLeave = false,
    }
}

kKirbyFloatTrigger.fieldInformation = {
    maxDuration = {
        fieldType = "number",
        minimumValue = 0
    },
    maxFlaps = {
        fieldType = "integer",
        minimumValue = 0
    }
}

return kKirbyFloatTrigger
