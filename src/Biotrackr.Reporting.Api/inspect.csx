using System;
using System.Reflection;

try
{
    // Load assembly from the build output
    var asm = Assembly.LoadFrom(@"Biotrackr.Reporting.Api\bin\Debug\net10.0\GitHub.Copilot.SDK.dll");
    foreach (var t in asm.GetExportedTypes())
    {
        if (t.Name.Contains("Permission") || t.Name.Contains("Invocation"))
        {
            Console.WriteLine($"\n=== {t.FullName} ===");
            foreach (var p in t.GetProperties())
                Console.WriteLine($"  Prop: {p.Name} ({p.PropertyType})");
            foreach (var f in t.GetFields())
                Console.WriteLine($"  Field: {f.Name} ({f.FieldType})");
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
            Console.WriteLine($"\n=== {t.FullName} ===");
            foreach (var p in t.GetProperties())
                Console.WriteLine($"  Prop: {p.Name} ({p.PropertyType})");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
