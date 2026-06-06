using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

// Resolver that returns synthesized stub assemblies for refs we can't find on disk,
// delegating everything else to a normal disk resolver.
class StubResolver : IAssemblyResolver {
  readonly DefaultAssemblyResolver disk = new DefaultAssemblyResolver();
  readonly Dictionary<string, AssemblyDefinition> stubs = new Dictionary<string, AssemblyDefinition>();
  public StubResolver(string dir){ disk.AddSearchDirectory(dir); }
  public void AddStub(string name, AssemblyDefinition def){ stubs[name] = def; }
  public AssemblyDefinition Resolve(AssemblyNameReference name){
    if (stubs.TryGetValue(name.Name, out var s)) return s;
    return disk.Resolve(name);
  }
  public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters p){
    if (stubs.TryGetValue(name.Name, out var s)) return s;
    return disk.Resolve(name, p);
  }
  public void Dispose(){ disk.Dispose(); foreach(var s in stubs.Values) s.Dispose(); }
}

class Patch {
  static readonly string[] STUBBED = { "UnityEngine", "UnityEngine.UI", "Assembly-CSharp-firstpass" };

  static void Main(string[] args){
    string path = args[0];
    string dir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path));
    var resolver = new StubResolver(dir);

    var rp = new ReaderParameters { ReadWrite = true, AssemblyResolver = resolver };
    var asm = AssemblyDefinition.ReadAssembly(path, rp);
    var module = asm.MainModule;

    // For each stubbed assembly, build a stub module with enum types for every
    // type the main module references into it (so write-time constant/type
    // resolution succeeds). Stubs are transient and never emitted to the output.
    foreach (var anr in module.AssemblyReferences.Where(r => STUBBED.Contains(r.Name)).GroupBy(r=>r.Name).Select(g=>g.First())) {
      var stubAsm = AssemblyDefinition.CreateAssembly(
        new AssemblyNameDefinition(anr.Name, anr.Version ?? new Version(0,0,0,0)),
        anr.Name, ModuleKind.Dll);
      var sm = stubAsm.MainModule;
      var enumBase = new TypeReference("System","Enum", sm, sm.TypeSystem.CoreLibrary);
      var byFullName = new Dictionary<string, TypeDefinition>();

      foreach (var tr in module.GetTypeReferences()) {
        if (ScopeName(tr) != anr.Name) continue;
        EnsureStub(tr, sm, enumBase, byFullName);
      }
      resolver.AddStub(anr.Name, stubAsm);
      Console.WriteLine($"stub {anr.Name}: {byFullName.Count} types");
    }

    // ---- The actual patch: Application.runInBackground = true in LevelLoadManager.Awake ----
    var unityRef = module.AssemblyReferences.First(r => r.Name == "UnityEngine");
    var appType = new TypeReference("UnityEngine","Application", module, unityRef);
    var setter = new MethodReference("set_runInBackground", module.TypeSystem.Void, appType);
    setter.Parameters.Add(new ParameterDefinition(module.TypeSystem.Boolean));
    var setterRef = module.ImportReference(setter);

    var type = module.GetTypes().First(t => t.Name == "LevelLoadManager");
    var m = type.Methods.First(x => x.Name == "Awake" && x.HasBody);
    if (m.Body.Instructions.Any(i => i.Operand is MethodReference r && r.Name == "set_runInBackground")) {
      Console.WriteLine("Already patched."); return;
    }
    var il = m.Body.GetILProcessor();
    var first = m.Body.Instructions[0];
    il.InsertBefore(first, il.Create(OpCodes.Ldc_I4_1));
    il.InsertBefore(first, il.Create(OpCodes.Call, setterRef));

    asm.Write();
    Console.WriteLine("WROTE patched assembly.");
  }

  static string ScopeName(TypeReference tr){
    var s = tr.Scope;
    if (s is AssemblyNameReference a) return a.Name;
    if (s is ModuleReference) return tr.Module.Assembly.Name.Name;
    return tr.Module.Assembly.Name.Name;
  }

  // Create a stub enum TypeDefinition (with int value__ field) for tr, recursing for nested decls.
  static TypeDefinition EnsureStub(TypeReference tr, ModuleDefinition sm, TypeReference enumBase, Dictionary<string,TypeDefinition> map){
    string key = tr.FullName;
    if (map.TryGetValue(key, out var existing)) return existing;
    var attrs = TypeAttributes.Public | TypeAttributes.Sealed;
    TypeDefinition td;
    if (tr.IsNested) {
      var decl = EnsureStub(tr.DeclaringType, sm, enumBase, map);
      td = new TypeDefinition(null, tr.Name, TypeAttributes.NestedPublic | TypeAttributes.Sealed){ BaseType = enumBase };
      decl.NestedTypes.Add(td);
    } else {
      td = new TypeDefinition(tr.Namespace, tr.Name, attrs){ BaseType = enumBase };
      sm.Types.Add(td);
    }
    td.Fields.Add(new FieldDefinition("value__", FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName, sm.TypeSystem.Int32));
    map[key] = td;
    return td;
  }
}
