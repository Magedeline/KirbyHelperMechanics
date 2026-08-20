-- Loenn plugin for KirbyHelperMechanics/K_ZipMover.
-- Placeholder visual: violet rectangle. Swap the sprite function for a drawableSprite
-- once real art exists; no placement data has to change.

local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")

local zipMover = {}

zipMover.name = "KirbyHelperMechanics/K_ZipMover"
zipMover.depth = -9000
zipMover.minimumSize = {16, 16}
zipMover.nodeLimits = {1, 1}
zipMover.nodeLineRenderType = "line"
zipMover.nodeVisibility = "always"

zipMover.placements = {
    name = "k_zip_mover",
    data = {
        width = 16,
        height = 16,
    }
}

local fillColor = {0.70, 0.24, 1.0, 0.5}
local lineColor = {0.42, 0.12, 0.64, 1.0}

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

function zipMover.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local sprites = {}

    pushRect(sprites, x, y, width, height, fillColor, lineColor)

    return sprites
end

function zipMover.selection(room, entity)
    return utils.rectangle(entity.x or 0, entity.y or 0, entity.width or 16, entity.height or 16)
end

return zipMover
