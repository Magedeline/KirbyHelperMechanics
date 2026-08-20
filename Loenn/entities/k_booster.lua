-- Loenn plugin for KirbyHelperMechanics/K_Booster.
-- Placeholder visual: green/red coil circle. Swap the sprite function for a drawableSprite
-- once real art exists; no placement data has to change.

local drawableFunction = require("structs.drawable_function")
local drawing = require("utils.drawing")
local utils = require("utils")

local booster = {}

booster.name = "KirbyHelperMechanics/K_Booster"
booster.depth = -8500

booster.placements = {
    {
        name = "k_booster_green",
        data = { red = false }
    },
    {
        name = "k_booster_red",
        data = { red = true }
    },
}

local function drawBooster(x, y, red)
    local body = red and {1.0, 0.23, 0.23, 1.0} or {0.23, 1.0, 0.42, 1.0}

    drawing.callKeepOriginalColor(function()
        love.graphics.setColor(body)
        love.graphics.circle("fill", x, y, 10)
        love.graphics.setColor(1, 1, 1, 0.6)
        love.graphics.circle("fill", x, y, 6)
    end)
end

function booster.sprite(room, entity)
    return drawableFunction.fromFunction(drawBooster, entity.x or 0, entity.y or 0, entity.red or false)
end

function booster.selection(room, entity)
    return utils.rectangle((entity.x or 0) - 10, (entity.y or 0) - 10, 20, 20)
end

return booster
