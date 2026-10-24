using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using SmartTicketing.Domain;

var root=Path.GetFullPath(args.FirstOrDefault()??"Assignment1");
var output=Path.Combine(root,"engineering");Directory.CreateDirectory(output);
var model=JsonSerializer.Deserialize<Dictionary<string,Dictionary<string,string>>>(File.ReadAllText(Path.Combine(root,"tools/ModelEngineering/domain-model.json")))!;
var code=new StringBuilder("// Forward generated from domain-model.json. Review before using.\nnamespace GeneratedModel;\n");
foreach(var (type,properties) in model){code.AppendLine($"public class {type}\n{{");foreach(var (name,value) in properties)code.AppendLine($"    public {value} {name} {{ get; set; }}");code.AppendLine("}\n");}
File.WriteAllText(Path.Combine(output,"ModelSkeletons.cs"),code.ToString());
var types=typeof(TicketTransaction).Assembly.GetExportedTypes();
var reflected=types.Select(t=>new{name=t.FullName,kind=t.IsInterface?"interface":t.IsEnum?"enum":"class/record",properties=t.GetProperties(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly).Select(p=>new{name=p.Name,type=p.PropertyType.Name}),methods=t.GetMethods(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly).Where(m=>!m.IsSpecialName).Select(m=>m.Name).Distinct()});
File.WriteAllText(Path.Combine(output,"CodeToModel.json"),JsonSerializer.Serialize(reflected,new JsonSerializerOptions{WriteIndented=true}));
XNamespace ns="http://schemas.microsoft.com/vs/2009/dgml";
var links=types.SelectMany(t=>t.GetProperties().Select(p=>(Source:t,Target:p.PropertyType)).Where(p=>types.Contains(p.Target))).Distinct();
var graph=new XElement(ns+"DirectedGraph",new XAttribute("GraphDirection","LeftToRight"),new XElement(ns+"Nodes",types.Select(t=>new XElement(ns+"Node",new XAttribute("Id",t.FullName!),new XAttribute("Label",t.Name)))),new XElement(ns+"Links",links.Select(l=>new XElement(ns+"Link",new XAttribute("Source",l.Source.FullName!),new XAttribute("Target",l.Target.FullName!),new XAttribute("Label","property type")))));
graph.Save(Path.Combine(output,"DomainCodeMap.dgml"));
Console.WriteLine($"Forward: {model.Count} model classes -> ModelSkeletons.cs");Console.WriteLine($"Backward: {types.Length} compiled types -> CodeToModel.json");Console.WriteLine($"Code map: {links.Count()} reflected property dependencies -> DomainCodeMap.dgml");
