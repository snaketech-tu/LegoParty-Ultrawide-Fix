using System.Collections.Immutable; using System.Reflection.Metadata; using System.Reflection.PortableExecutable;
// args: plugin.dll interopDir. Verifies every member/type the plugin references in stubbed assemblies exists in the real ones.
var stubbed = new HashSet<string>{"Il2Cppmscorlib","UnityEngine.CoreModule","UnityEngine.UI","Koala.Core","Koala.UI","Koala.Minigames"};
var pe = new PEReader(File.OpenRead(args[0])); var md = pe.GetMetadataReader(); var prov = new P();
var real = new Dictionary<string,MetadataReader>();
MetadataReader R(string a){ if(!real.TryGetValue(a,out var r)){ r=new PEReader(File.OpenRead(Path.Combine(args[1],a+".dll"))).GetMetadataReader(); real[a]=r;} return r; }
string Scope(TypeReferenceHandle h){ var t=md.GetTypeReference(h); return t.ResolutionScope.Kind==HandleKind.TypeReference? Scope((TypeReferenceHandle)t.ResolutionScope): md.GetString(md.GetAssemblyReference((AssemblyReferenceHandle)t.ResolutionScope).Name); }
string Name(MetadataReader m, TypeReferenceHandle h){ var t=m.GetTypeReference(h); return (t.ResolutionScope.Kind==HandleKind.TypeReference? Name(m,(TypeReferenceHandle)t.ResolutionScope)+"/":(m.GetString(t.Namespace)+".")) + m.GetString(t.Name); }
TypeDefinition? Find(MetadataReader r, string full){ foreach(var h in r.TypeDefinitions){ var t=r.GetTypeDefinition(h); string n=r.GetString(t.Namespace)+"."+r.GetString(t.Name); if(!t.GetDeclaringType().IsNil){ var d=r.GetTypeDefinition(t.GetDeclaringType()); n=r.GetString(d.Namespace)+"."+r.GetString(d.Name)+"/"+r.GetString(t.Name);} if(n==full) return t;} return null; }
int bad=0, ok=0;
foreach (var th in md.TypeReferences){ var a=Scope(th); if(!stubbed.Contains(a)) continue; var n=Name(md,th); if(Find(R(a),n)==null){Console.WriteLine($"MISSING TYPE {a}:{n}");bad++;} else ok++; }
foreach (var mh in md.MemberReferences){ var m=md.GetMemberReference(mh); TypeReferenceHandle th;
  if(m.Parent.Kind==HandleKind.TypeReference) th=(TypeReferenceHandle)m.Parent;
  else if(m.Parent.Kind==HandleKind.TypeSpecification) continue; else continue;
  var a=Scope(th); if(!stubbed.Contains(a)) continue; var tn=Name(md,th); var name=md.GetString(m.Name);
  var r=R(a); var td=Find(r,tn); if(td==null) continue;
  string sig = m.GetKind()==MemberReferenceKind.Method ? Sig(md,m.DecodeMethodSignature(prov,null)) : m.DecodeFieldSignature(prov,null);
  bool found=false; var cands=new List<string>();
  foreach(var h in td.Value.GetMethods()){ var d=r.GetMethodDefinition(h); if(r.GetString(d.Name)!=name) continue; var s=Sig(r,d.DecodeSignature(prov,null)); cands.Add(s); if(s==sig) found=true; }
  foreach(var h in td.Value.GetFields()){ var d=r.GetFieldDefinition(h); if(r.GetString(d.Name)!=name) continue; var s=d.DecodeSignature(prov,null); cands.Add(s); if(s==sig) found=true; }
  if(found) ok++; else { bad++; Console.WriteLine($"MISMATCH {tn}::{name} want {sig} have [{string.Join(" | ",cands)}]"); } }
Console.WriteLine($"ok={ok} bad={bad}"); return bad==0?0:1;
static string Sig(MetadataReader r, MethodSignature<string> s)=> (s.Header.IsInstance?"inst ":"static ")+s.ReturnType+"("+string.Join(",",s.ParameterTypes)+")"+(s.GenericParameterCount>0?"`"+s.GenericParameterCount:"");
class P : ISignatureTypeProvider<string,object> {
 public string GetArrayType(string e, ArrayShape s)=>e+"[,]"; public string GetByReferenceType(string e)=>e+"&"; public string GetFunctionPointerType(MethodSignature<string> s)=>"fnptr";
 public string GetGenericInstantiation(string g, ImmutableArray<string> a)=>g+"<"+string.Join(",",a)+">"; public string GetGenericMethodParameter(object c,int i)=>"!!"+i; public string GetGenericTypeParameter(object c,int i)=>"!"+i;
 public string GetModifiedType(string m,string u,bool req)=>u; public string GetPinnedType(string e)=>e; public string GetPointerType(string e)=>e+"*"; public string GetPrimitiveType(PrimitiveTypeCode t)=>t.ToString();
 public string GetSZArrayType(string e)=>e+"[]";
 public string GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte k){ var t=r.GetTypeDefinition(h); return r.GetString(t.Namespace)+"."+r.GetString(t.Name); }
 public string GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte k){ var t=r.GetTypeReference(h); return r.GetString(t.Namespace)+"."+r.GetString(t.Name); }
 public string GetTypeFromSpecification(MetadataReader r, object c, TypeSpecificationHandle h, byte k)=> r.GetTypeSpecification(h).DecodeSignature(this,c);
}
