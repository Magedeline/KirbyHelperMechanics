-- Loenn plugin for KirbyHelperMechanics/K_TempleGateDoor.
-- Placeholder visual: brown rectangle. Swap the sprite function for a drawableSprite
-- once real art exists; no placement data has to change.

local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")

local templeGateDoor = {}

templeGateDoor.name = "KirbyHelperMechanics/K_TempleGateDoor"
templeGateDoor.depth = -9998
templeGateDoor.minimumSize = {8, 8}

templeGateDoor.placements = {
    name = "k_temple_gate_door",
    data = {
        width = 16,
        height = 32,
        flag = "temple_presser",
    }
}

local fillColor = {0.35, 0.27, 0.20, 0.9}
local lineColor = {0.79, 0.64, 0.42, 1.0}

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

function templeGateDoor.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 32
    local sprites = {}

    pushRect(sprites, x, y, width, height, fillColor, lineColor)

    return sprites
end

function templeGateDoor.selection(room, entity)
    return utils.rectangle(entity.x or 0, entity.y or 0, entity.width or 16, entity.height or 32)
end

return templeGateDoor
