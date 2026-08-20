-- Loenn plugin for KirbyHelperMechanics/K_ArrowBlock.
-- Placeholder visual: purple rectangle with a direction arrow line. Swap the sprite
-- function for a drawableSprite once real art exists; no placement data has to change.

local drawableRectangle = require("structs.drawable_rectangle")
local drawableLine = require("structs.drawable_line")
local utils = require("utils")

local arrowBlock = {}

arrowBlock.name = "KirbyHelperMechanics/K_ArrowBlock"
arrowBlock.depth = -1
arrowBlock.minimumSize = {16, 16}

arrowBlock.fieldInformation = {
    direction = {
        options = {"Left", "Right", "Up", "Down"},
        editable = false,
    }
}

arrowBlock.placements = {
    name = "k_arrow_block",
    data = {
        width = 16,
        height = 16,
        direction = "Right",
    }
}

local fillColor = {0.28, 0.25, 0.44, 0.9}
local lineColor = {0.19, 0.17, 0.29, 1.0}
local arrowColor = {1.0, 1.0, 1.0, 1.0}

local directionOffsets = {
    Left = {-1, 0},
    Right = {1, 0},
    Up = {0, -1},
    Down = {0, 1},
}

local function push(sprites, rectangle)
    local result = rectangle:getDrawableSprite()

    if result[1] ~= nil then
        for _, sprite in ipairs(result) do
            table.insert(sprites, sprite)
        end
    else
        table.insert(sprites, result)
    end
end

function arrowBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local sprites = {}

    push(sprites, drawableRectangle.fromRectangle("fill", x, y, width, height, fillColor))
    push(sprites, drawableRectangle.fromRectangle("line", x, y, width, height, lineColor))

    local dir = directionOffsets[entity.direction or "Right"] or directionOffsets.Right
    local cx, cy = x + width / 2, y + height / 2
    local len = math.min(width, height) / 2 - 2

    push(sprites, drawableLine.fromPoints({cx, cy, cx + dir[1] * len, cy + dir[2] * len}, arrowColor, 2))

    return sprites
end

function arrowBlock.selection(room, entity)
    return utils.rectangle(entity.x or 0, entity.y or 0, entity.width or 16, entity.height or 16)
end

return arrowBlock
