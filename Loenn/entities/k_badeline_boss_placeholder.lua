-- Loenn plugin for KirbyHelperMechanics/K_BadelineBossPlaceholder.
-- Placeholder visual only, per request: dark silhouette rectangle with a cyan flame
-- outline. Swap the sprite function for a drawableSprite once real art/AI exists;
-- no placement data has to change.

local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")

local badelineBoss = {}

badelineBoss.name = "KirbyHelperMechanics/K_BadelineBossPlaceholder"
badelineBoss.depth = -8500
badelineBoss.justification = {0.5, 1.0}

badelineBoss.placements = {
    name = "k_badeline_boss_placeholder",
    data = {
        health = 10,
    }
}

badelineBoss.fieldInformation = {
    health = {
        fieldType = "integer",
        minimumValue = 1,
    }
}

local fillColor = {0.17, 0.10, 0.24, 1.0}
local lineColor = {0.42, 0.88, 1.0, 1.0}

function badelineBoss.sprite(room, entity)
    local x, y = (entity.x or 0) - 8, (entity.y or 0) - 20

    return {
        drawableRectangle.fromRectangle("fill", x, y, 16, 20, fillColor),
        drawableRectangle.fromRectangle("line", x, y, 16, 20, lineColor),
    }
end

function badelineBoss.selection(room, entity)
    return utils.rectangle((entity.x or 0) - 8, (entity.y or 0) - 20, 16, 20)
end

return badelineBoss
