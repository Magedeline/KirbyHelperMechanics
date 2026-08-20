-- Loenn plugin for KirbyHelperMechanics/K_SeekerChaser.
-- Placeholder visual: teal spiked circle. Swap the sprite function for a drawableSprite
-- once real art exists; no placement data has to change.

local drawableFunction = require("structs.drawable_function")
local drawing = require("utils.drawing")
local utils = require("utils")

local seekerChaser = {}

seekerChaser.name = "KirbyHelperMechanics/K_SeekerChaser"
seekerChaser.depth = 0

seekerChaser.placements = {
    name = "k_seeker_chaser",
    data = {
        health = 2,
    }
}

seekerChaser.fieldInformation = {
    health = {
        fieldType = "integer",
        minimumValue = 1,
    }
}

local bodyColor = {0.18, 0.72, 0.65, 1.0}
local spikeColor = {0.07, 0.37, 0.33, 1.0}

local function drawChaser(x, y)
    drawing.callKeepOriginalColor(function()
        love.graphics.setColor(bodyColor)
        love.graphics.circle("fill", x, y, 8)
        love.graphics.setColor(spikeColor)
        love.graphics.circle("fill", x, y, 4)
    end)
end

function seekerChaser.sprite(room, entity)
    return drawableFunction.fromFunction(drawChaser, entity.x or 0, entity.y or 0)
end

function seekerChaser.selection(room, entity)
    return utils.rectangle((entity.x or 0) - 8, (entity.y or 0) - 8, 16, 16)
end

return seekerChaser
