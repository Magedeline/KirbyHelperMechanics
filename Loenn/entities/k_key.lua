-- Loenn plugin for KirbyHelperMechanics/K_Key.
-- Placeholder visual: yellow diamond. Swap the sprite function for a drawableSprite
-- once real art exists; no placement data has to change.

local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")

local key = {}

key.name = "KirbyHelperMechanics/K_Key"
key.depth = -100

key.placements = {
    name = "k_key",
    data = {
        id = "",
    }
}

local fillColor = {1.0, 0.91, 0.40, 1.0}
local lineColor = {0.0, 0.0, 0.0, 0.6}

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

function key.sprite(room, entity)
    local x, y = (entity.x or 0) - 4, (entity.y or 0) - 4
    local sprites = {}

    pushRect(sprites, x, y, 8, 8, fillColor, lineColor)

    return sprites
end

function key.selection(room, entity)
    return utils.rectangle((entity.x or 0) - 4, (entity.y or 0) - 4, 8, 8)
end

return key
