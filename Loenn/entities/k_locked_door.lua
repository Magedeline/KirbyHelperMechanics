-- Loenn plugin for KirbyHelperMechanics/K_LockedDoor.
-- Placeholder visual: dark rectangle with a yellow lock dot. Swap the sprite function for
-- a drawableSprite once real art exists; no placement data has to change.

local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")

local lockedDoor = {}

lockedDoor.name = "KirbyHelperMechanics/K_LockedDoor"
lockedDoor.depth = -9997
lockedDoor.minimumSize = {8, 8}

lockedDoor.placements = {
    name = "k_locked_door",
    data = {
        width = 16,
        height = 32,
        keyId = "",
    }
}

local fillColor = {0.23, 0.18, 0.13, 0.95}
local lockColor = {1.0, 0.91, 0.40, 1.0}

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

function lockedDoor.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 32
    local sprites = {}

    pushRect(sprites, x, y, width, height, fillColor, {0.05, 0.05, 0.05, 1.0})
    pushRect(sprites, x + width / 2 - 2, y + height / 2 - 2, 4, 4, lockColor, lockColor)

    return sprites
end

function lockedDoor.selection(room, entity)
    return utils.rectangle(entity.x or 0, entity.y or 0, entity.width or 16, entity.height or 32)
end

return lockedDoor
