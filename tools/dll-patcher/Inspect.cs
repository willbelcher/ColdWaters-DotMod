using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

class Inspect {
  static void Main(string[] a) {
    var asm = AssemblyDefinition.ReadAssembly(a[0]);
    foreach (var spec in a.Skip(1)) {
      var parts = spec.Split(new[]{"::"}, StringSplitOptions.None);
      string tn = parts[0], mn = parts.Length>1?parts[1]:null;
      var type = asm.MainModule.GetTypes().FirstOrDefault(t => t.FullName==tn || t.Name==tn);
      if (type==null) { Console.WriteLine("TYPE NOT FOUND: "+tn); continue; }
      foreach (var m in type.Methods.Where(m => mn==null || m.Name==mn)) {
        Console.WriteLine("\n==== "+type.FullName+"::"+m.Name+"  ("+m.Parameters.Count+" params, ret "+m.ReturnType.Name+") ====");
        if (!m.HasBody) { Console.WriteLine("  (no body)"); continue; }
        foreach (var v in m.Body.Variables) Console.WriteLine("  loc "+v.Index+": "+v.VariableType.Name);
        foreach (var i in m.Body.Instructions) Console.WriteLine("  "+i);
      }
    }
  }
}
