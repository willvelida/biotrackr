using System;
using System.Reflection;

class Inspector
{
    static void Main()
    {
        try
        {
            var asm = Assembly.LoadFrom(@"Biotrackr.Reporting.Api\bin\Debug\net10.0\GitHub.Copilot.SDK.dll");
            foreach (var t in asm.GetExportedTypes())
            {
                if (t.Name.Contains("Permission") || t.Name.Contains("Invocation"))
                {
                    Console.WriteLine("=== " + t.FullName + " ===");
                    foreach (var p in t.GetProperties())
                        Console.WriteLine("  Prop: " + p.Name + " (" + p.PropertyType + ")");
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            foreach (var t in ex.Types)
            {
                if (t == null) continue;
                if (t.Name.Contains("Permission") || t.Name.Contains("Invocation"))
                {
                    Console.WriteLine("=== " + t.FullName + " ===");
                    try { foreach (var p in t.GetProperties()) Console.WriteLine("  Prop: " + p.Name + " (" + p.PropertyType + ")"); } catch {}
                }
            }
        }
        catch (Exception ex) { Console.WriteLine("Error: " + ex.GetType().Name + " " + ex.Message); }
    }
}
