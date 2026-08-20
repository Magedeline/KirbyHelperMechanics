-- Loenn plugin for KirbyHelperMechanics/K_Jellyfish.
-- Placeholder visual: violet circle. Swap the sprite function for a drawableSprite
-- once real art exists; no placement data has to change.

local drawableFunction = require("structs.drawable_function")
local drawing = require("utils.drawing")
local utils = require("utils")

local jellyfish = {}

jellyfish.name = "KirbyHelperMechanics/K_Jellyfish"
jellyfish.depth = 0

jellyfish.placements = {
    name = "k_jellyfish",
    data = {}
}

local bodyColor = {0.85, 0.56, 1.0, 1.0}

local function drawJellyfish(x, y)
    drawing.callKeepOriginalColor(function()
        love.graphics.setColor(bodyColor)
        love.graphics.circle("fill", x, y, 8)
        love.graphics.setColor(1, 1, 1, 0.7)
        love.graphics.circle("fill", x, y - 2, 3)
    end)
end

function jellyfish.sprite(room, entity)
    return drawableFunction.fromFunction(drawJellyfish, entity.x or 0, entity.y or 0)
end

function jellyfish.selection(room, entity)
    return utils.rectangle((entity.x or 0) - 8, (entity.y or 0) - 8, 16, 16)
end

return jellyfish
