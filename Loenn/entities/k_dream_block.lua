-- Loenn plugin for KirbyHelperMechanics/K_DreamBlock.
-- Placeholder visual: deep blue rectangle. Swap the sprite function for a drawableSprite
-- once real art exists; no placement data has to change.

local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")

local dreamBlock = {}

dreamBlock.name = "KirbyHelperMechanics/K_DreamBlock"
dreamBlock.depth = -11000
dreamBlock.minimumSize = {8, 8}

dreamBlock.placements = {
    name = "k_dream_block",
    data = {
        width = 16,
        height = 16,
    }
}

local fillColor = {0.035, 0.10, 0.24, 0.85}
local lineColor = {0.42, 0.88, 1.0, 0.9}

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

local function pushRect(sprites, x, y, width, height, fill, line)
    push(sprites, drawableRectangle.fromRectangle("fill", x, y, width, height, fill))
    push(sprites, drawableRectangle.fromRectangle("line", x, y, width, height, line))
end

function dreamBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local sprites = {}

    pushRect(sprites, x, y, width, height, fillColor, lineColor)

    return sprites
end

function dreamBlock.selection(room, entity)
    return utils.rectangle(entity.x or 0, entity.y or 0, entity.width or 16, entity.height or 16)
end

return dreamBlock
