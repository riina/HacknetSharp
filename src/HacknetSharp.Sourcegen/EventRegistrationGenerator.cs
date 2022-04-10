using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HacknetSharp.Sourcegen
{
    [Generator]
    public class EventRegistrationGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new EventSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.Compilation.AssemblyName != "HacknetSharp") return;
            EventSyntaxReceiver? syntaxReceiver = (EventSyntaxReceiver?)context.SyntaxReceiver;
            if (syntaxReceiver == null) return;
            var sb = new StringBuilder(@"
namespace HacknetSharp
{
    public static partial class Util
    {
        static partial void RegisterCommands()
        {");
            foreach (var item in syntaxReceiver.Events)
            {
                var sem = context.Compilation.GetSemanticModel(item.Attribute.SyntaxTree);
                if (sem.GetSymbolInfo(item.Attribute).Symbol?.ContainingSymbol.ToString() !=
                    "HacknetSharp.EventCommandAttribute") continue;
                string name = item.Type.GetFullyQualifiedName();
                sb.Append(@$"
            RegisterCommand<{name}>({item.Command});");
                if (item.AzuraSerializing == null ||
                    sem.GetSymbolInfo(item.AzuraSerializing).Symbol?.ContainingSymbol.ToString() !=
                    "Azura.AzuraAttribute") continue;
                string id = item.Type.Identifier.ToString();
                string? namespaceName = item.Type.GetParentName();
                var sb2 = new StringBuilder(@"
#pragma warning disable 1591
using System.IO;");
                if (namespaceName != null)
                    sb2.Append($@"
namespace {namespaceName}
{{");
                sb2.Append($@"
    public partial class {id}
    {{

        /// <inheritdoc />
        public override void Serialize(Stream stream) => {name}Serialization.Serialize(this, stream);

        /// <inheritdoc />
        public override Event Deserialize(Stream stream) => {name}Serialization.Deserialize(stream);
    }}");
                if (namespaceName != null)
                    sb2.Append(@"
}");
                context.AddSource($"{name}_Serialization.cs", sb2.ToString());
            }

            sb.Append(@"
        }
    }
}");
            context.AddSource("Util_EventRegistration.cs", sb.ToString());
        }

        private class EventSyntaxReceiver : ISyntaxReceiver
        {
            public record EventItem(TypeDeclarationSyntax Type, AttributeSyntax Attribute, string Command,
                AttributeSyntax? AzuraSerializing);

            public List<EventItem> Events { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is TypeDeclarationSyntax tds)
                {
                    var list = new List<AttributeSyntax>(tds.AttributeLists.SelectMany(v => v.Attributes));
                    foreach (AttributeSyntax x in list)
                        if (x.Name.ToString() == "EventCommand" && x.ArgumentList?.Arguments[0].ToString() is
                                { } command)
                        {
                            Events.Add(new EventItem(tds, x, command,
                                list.FirstOrDefault(a => a.Name.ToString() == "Azura")));
                            break;
                        }
                }
            }
        }
    }
}
