// Build step: UnderrailFix.dll is compiled against FNA (Wine Mono's WineMono.FNA). This points each of its XNA type
// references at the XNA 4.0 assembly the game references that type from, so the DLL binds the way underrail.exe
// does: through Wine Mono's XNA facades on Linux, or to Microsoft XNA on Windows.
// Usage: mono Retarget.exe <UnderrailFix.dll> <underrail.exe>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

static class Retarget
{
    static int Main(string[] args)
    {
        var game = ModuleDefinition.ReadModule(args[1]);
        var scopes = new Dictionary<string, AssemblyNameReference>();
        foreach (var t in game.GetTypeReferences())
            if (t.DeclaringType == null && t.Scope is AssemblyNameReference a && a.Name.StartsWith("Microsoft.Xna.Framework"))
                scopes[t.FullName] = a;
        var xna = game.AssemblyReferences.Where(a => a.Name.StartsWith("Microsoft.Xna.Framework")).ToDictionary(a => a.Name);

        var dll = ModuleDefinition.ReadModule(new MemoryStream(File.ReadAllBytes(args[0])));
        var missing = new List<string>();
        foreach (var t in dll.GetTypeReferences().ToList())
        {
            if (t.DeclaringType != null || !(t.Scope is AssemblyNameReference s) || !s.Name.StartsWith("WineMono.FNA")) continue;
            AssemblyNameReference target;
            if (!scopes.TryGetValue(t.FullName, out target))
            {
                // Not used by the game itself: XNA's own layout (Graphics types in .Graphics, the rest in the core assembly).
                string name = t.Namespace.StartsWith("Microsoft.Xna.Framework.Graphics") ? "Microsoft.Xna.Framework.Graphics" : "Microsoft.Xna.Framework";
                if (!xna.TryGetValue(name, out target)) { missing.Add(t.FullName); continue; }
            }
            var reference = dll.AssemblyReferences.FirstOrDefault(r => r.FullName == target.FullName);
            if (reference == null)
            {
                reference = new AssemblyNameReference(target.Name, target.Version) { PublicKeyToken = target.PublicKeyToken, Culture = target.Culture };
                dll.AssemblyReferences.Add(reference);
            }
            t.Scope = reference;
        }
        if (missing.Count > 0) { Console.Error.WriteLine("no XNA assembly for: " + string.Join(", ", missing)); return 1; }
        foreach (var r in dll.AssemblyReferences.Where(r => r.Name.StartsWith("WineMono.FNA")).ToList()) dll.AssemblyReferences.Remove(r);
        dll.Write(args[0]);
        Console.WriteLine("XNA references: " + string.Join(", ", dll.AssemblyReferences.Where(r => r.Name.StartsWith("Microsoft.Xna")).Select(r => r.Name)));
        return 0;
    }
}
