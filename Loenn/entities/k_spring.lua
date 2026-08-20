-- Loenn plugin for KirbyHelperMechanics/K_Spring.
-- Placeholder visual: yellow coil circle. Swap the sprite function for a drawableSprite
-- once real art exists; no placement data has to change.

local drawableFunction = require("structs.drawable_function")
local drawing = require("utils.drawing")
local utils = require("utils")

local spring = {}

spring.name = "KirbyHelperMechanics/K_Spring"
spring.depth = -8501

spring.fieldInformation = {
    orientation = {
        options = {"Up", "Left", "Right"},
        editable = false,
    }
}

spring.placements = {
    name = "k_spring",
    data = {
        orientation = "Up",
    }
}

local coilColor = {0.85, 0.56, 0.0, 1.0}
local bodyColor = {1.0, 0.82, 0.25, 1.0}

local function drawSpring(x, y)
    drawing.callKeepOriginalColor(function()
        love.graphics.setColor(coilColor)
        love.graphics.circle("fill", x, y, 8)
        love.graphics.setColor(bodyColor)
        love.graphics.circle("fill", x, y, 6)
    end)
end

function spring.sprite(room, entity)
    return drawableFunction.fromFunction(drawSpring, entity.x or 0, entity.y or 0)
end

function spring.selection(room, entity)
    return utils.rectangle((entity.x or 0) - 8, (entity.y or 0) - 8, 16, 16)
end

return spring
