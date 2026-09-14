-- Loenn plugin for KirbyHelperMechanics/K_Key.
-- Placeholder visual: yellow diamond. Swap the sprite function for a drawableSprite
-- once real art exists; no placement data has to change.

local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")

local key = {}

key.name = "KirbyHelperMechanics/K_Key"
key.depth = -100

key.placements = {
    name = "k_key",
    data = {
        id = "",
    }
}

local fillColor = {1.0, 0.91, 0.40, 1.0}
local lineColor = {0.0, 0.0, 0.0, 0.6}

function key.sprite(room, entity)
    local x, y = (entity.x or 0) - 4, (entity.y or 0) - 4

    return {
        drawableRectangle.fromRectangle("fill", x, y, 8, 8, fillColor),
        drawableRectangle.fromRectangle("line", x, y, 8, 8, lineColor),
    }
end

function key.selection(room, entity)
    return utils.rectangle((entity.x or 0) - 4, (entity.y or 0) - 4, 8, 8)
end

return key
