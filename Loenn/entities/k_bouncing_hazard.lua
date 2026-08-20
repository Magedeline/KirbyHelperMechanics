-- Loenn plugin for KirbyHelperMechanics/K_BouncingHazard.
-- Placeholder visual: fire/ice circle. Swap the sprite function for a drawableSprite
-- once real art exists; no placement data has to change.

local drawableFunction = require("structs.drawable_function")
local drawing = require("utils.drawing")
local utils = require("utils")

local bouncingHazard = {}

bouncingHazard.name = "KirbyHelperMechanics/K_BouncingHazard"
bouncingHazard.depth = 0

bouncingHazard.fieldInformation = {
    kind = {
        options = {"fire", "ice"},
        editable = false,
    },
    angle = {
        fieldType = "number",
    }
}

bouncingHazard.placements = {
    {
        name = "k_bouncing_hazard_fire",
        data = { kind = "fire", angle = 45.0 }
    },
    {
        name = "k_bouncing_hazard_ice",
        data = { kind = "ice", angle = 45.0 }
    },
}

local function drawHazard(x, y, kind)
    local body = kind == "ice" and {0.42, 0.88, 1.0, 1.0} or {1.0, 0.42, 0.12, 1.0}

    drawing.callKeepOriginalColor(function()
        love.graphics.setColor(body)
        love.graphics.circle("fill", x, y, 6)
    end)
end

function bouncingHazard.sprite(room, entity)
    return drawableFunction.fromFunction(drawHazard, entity.x or 0, entity.y or 0, entity.kind or "fire")
end

function bouncingHazard.selection(room, entity)
    return utils.rectangle((entity.x or 0) - 6, (entity.y or 0) - 6, 12, 12)
end

return bouncingHazard
