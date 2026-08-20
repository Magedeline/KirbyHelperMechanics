-- Loenn plugin for KirbyHelperMechanics/K_CrumbleBlock.
-- Placeholder visual: brown rectangle. Swap the sprite function for a drawableSprite
-- once real art exists; no placement data has to change.

local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")

local crumbleBlock = {}

crumbleBlock.name = "KirbyHelperMechanics/K_CrumbleBlock"
crumbleBlock.depth = 0
crumbleBlock.minimumSize = {8, 8}

crumbleBlock.placements = {
    name = "k_crumble_block",
    data = {
        width = 16,
        height = 8,
    }
}

local fillColor = {0.79, 0.48, 0.24, 0.6}
local lineColor = {0.42, 0.25, 0.11, 1.0}

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

function crumbleBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 8
    local sprites = {}

    pushRect(sprites, x, y, width, height, fillColor, lineColor)

    return sprites
end

function crumbleBlock.selection(room, entity)
    return utils.rectangle(entity.x or 0, entity.y or 0, entity.width or 16, entity.height or 8)
end

return crumbleBlock
