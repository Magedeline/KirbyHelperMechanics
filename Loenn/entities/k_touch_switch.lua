-- Loenn plugin for KirbyHelperMechanics/K_TouchSwitch.
-- Placeholder visual: grey circle. Swap the sprite function for a drawableSprite
-- once real art exists; no placement data has to change.

local drawableFunction = require("structs.drawable_function")
local drawing = require("utils.drawing")
local utils = require("utils")

local touchSwitch = {}

touchSwitch.name = "KirbyHelperMechanics/K_TouchSwitch"
touchSwitch.depth = -2000

touchSwitch.placements = {
    name = "k_touch_switch",
    data = {
        flag = "touch_switch",
    }
}

local bodyColor = {0.42, 0.42, 0.42, 1.0}

local function drawSwitch(x, y)
    drawing.callKeepOriginalColor(function()
        love.graphics.setColor(bodyColor)
        love.graphics.circle("fill", x, y, 7)
    end)
end

function touchSwitch.sprite(room, entity)
    return drawableFunction.fromFunction(drawSwitch, entity.x or 0, entity.y or 0)
end

function touchSwitch.selection(room, entity)
    return utils.rectangle((entity.x or 0) - 7, (entity.y or 0) - 7, 14, 14)
end

return touchSwitch
