using System;
using Celeste;

namespace Celeste.Mod.KirbyHelperMechanics
{
    /// <summary>
    /// Marks a class or method as hot-reloadable during development.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class HotReloadableAttribute : Attribute
    {
        public HotReloadableAttribute()
        {
        }
    }
}
