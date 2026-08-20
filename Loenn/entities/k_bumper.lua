-- Loenn plugin for KirbyHelperMechanics/K_Bumper.
-- Placeholder visual: orange/red circle. Swap the sprite function for a drawableSprite
-- once real art exists; no placement data has to change.

local drawableFunction = require("structs.drawable_function")
local drawing = require("utils.drawing")
local utils = require("utils")

local bumper = {}

bumper.name = "KirbyHelperMechanics/K_Bumper"
bumper.depth = -8500

bumper.placements = {
    {
        name = "k_bumper",
        data = { hot = false }
    },
    {
        name = "k_bumper_hot",
        data = { hot = true }
    },
}

local function drawBumper(x, y, hot)
    local body = hot and {1.0, 0.23, 0.23, 1.0} or {1.0, 0.7, 0.24, 1.0}

    drawing.callKeepOriginalColor(function()
        love.graphics.setColor(body)
        love.graphics.circle("fill", x, y, 12)
        love.graphics.setColor(1, 1, 1, 0.7)
        love.graphics.circle("fill", x, y, 6)
    end)
end

function bumper.sprite(room, entity)
    return drawableFunction.fromFunction(drawBumper, entity.x or 0, entity.y or 0, entity.hot or false)
end

function bumper.selection(room, entity)
    return utils.rectangle((entity.x or 0) - 12, (entity.y or 0) - 12, 24, 24)
end

return bumper
