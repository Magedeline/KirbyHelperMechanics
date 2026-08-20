-- Loenn plugin for KirbyHelperMechanics/K_TemplePresser.
-- Placeholder visual: grey plate rectangle. Swap the sprite function for a drawableSprite
-- once real art exists; no placement data has to change.

local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")

local templePresser = {}

templePresser.name = "KirbyHelperMechanics/K_TemplePresser"
templePresser.depth = -50

templePresser.placements = {
    name = "k_temple_presser",
    data = {
        flag = "temple_presser",
    }
}

local fillColor = {0.42, 0.42, 0.42, 1.0}

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

function templePresser.sprite(room, entity)
    local x, y = (entity.x or 0) - 8, (entity.y or 0) - 4
    local sprites = {}

    push(sprites, drawableRectangle.fromRectangle("fill", x, y, 16, 4, fillColor))

    return sprites
end

function templePresser.selection(room, entity)
    return utils.rectangle((entity.x or 0) - 8, (entity.y or 0) - 4, 16, 4)
end

return templePresser
