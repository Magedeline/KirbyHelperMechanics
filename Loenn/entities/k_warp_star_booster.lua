-- Loenn plugin for KirbyHelperMechanics/K_WarpStarBooster.
-- Placeholder visual: yellow/orange star-ish circle. Swap the sprite function for a
-- drawableSprite once real art exists; no placement data has to change.

local drawableFunction = require("structs.drawable_function")
local drawing = require("utils.drawing")
local utils = require("utils")

local warpStar = {}

warpStar.name = "KirbyHelperMechanics/K_WarpStarBooster"
warpStar.depth = -8500
warpStar.nodeLimits = {1, 1}
warpStar.nodeLineRenderType = "line"
warpStar.nodeVisibility = "always"

warpStar.placements = {
    name = "k_warp_star_booster",
    data = {}
}

local bodyColor = {1.0, 0.91, 0.40, 1.0}
local trailColor = {1.0, 0.60, 0.24, 1.0}

local function drawStar(x, y)
    drawing.callKeepOriginalColor(function()
        love.graphics.setColor(bodyColor)
        love.graphics.circle("fill", x, y, 9)
        love.graphics.setColor(trailColor)
        love.graphics.circle("fill", x, y, 4)
    end)
end

function warpStar.sprite(room, entity)
    return drawableFunction.fromFunction(drawStar, entity.x or 0, entity.y or 0)
end

function warpStar.selection(room, entity)
    return utils.rectangle((entity.x or 0) - 9, (entity.y or 0) - 9, 18, 18)
end

return warpStar
