// Minimal global usings for Kirby Helper Mechanics. DZ's own GlobalUsings.cs
// (Source/Core/GlobalUsings.cs) pulls in many DZ-only namespaces (Celeste.Helpers,
// Celeste.Utils, Celeste.Extensions.Core, DZModule/IngesteModule aliases, etc.)
// that don't exist in this trimmed repo -- copying it wholesale would trade
// today's compile errors for new ones. This covers only what K_Player.cs and
// friends actually reference unqualified:
//   - Celeste.Mod.Entities: CustomEntityAttribute
//   - Celeste.Extensions: KirbyMode
//   - CelesteGame: DZ's alias for the main Celeste.Celeste class (TimeRate, Freeze)
global using Celeste.Mod;
global using Celeste.Mod.Entities;
global using Celeste.Extensions;
global using CelesteGame = Celeste.Celeste;
