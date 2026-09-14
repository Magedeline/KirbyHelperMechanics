-- Loenn plugin for KirbyHelperMechanics/K_CrushBlock.
-- Placeholder visual: dark rectangle with a red face dot. Swap the sprite function for a
-- drawableSprite once real art exists; no placement data has to change.

local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")

local crushBlock = {}

crushBlock.name = "KirbyHelperMechanics/K_CrushBlock"
crushBlock.depth = -9000
crushBlock.minimumSize = {16, 16}

crushBlock.fieldInformation = {
    axis = {
        options = {"Horizontal", "Vertical"},
        editable = false,
    }
}

crushBlock.placements = {
    name = "k_crush_block",
    data = {
        width = 24,
        height = 24,
        axis = "Horizontal",
    }
}

local fillColor = {0.17, 0.17, 0.17, 0.9}
local lineColor = {1.0, 0.23, 0.23, 1.0}

function crushBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 24, entity.height or 24
    local cx, cy = x + width / 2 - 3, y + height / 2 - 3

    return {
        drawableRectangle.fromRectangle("fill", x, y, width, height, fillColor),
        drawableRectangle.fromRectangle("line", x, y, width, height, {0.06, 0.06, 0.06, 1.0}),
        drawableRectangle.fromRectangle("fill", cx, cy, 6, 6, lineColor),
        drawableRectangle.fromRectangle("line", cx, cy, 6, 6, lineColor),
    }
end

function crushBlock.selection(room, entity)
    return utils.rectangle(entity.x or 0, entity.y or 0, entity.width or 24, entity.height or 24)
end

return crushBlock
