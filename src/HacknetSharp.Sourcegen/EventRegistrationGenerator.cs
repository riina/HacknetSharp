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
            }

            sb.Append(@"
        }
    }
}");
            context.AddSource("Util_EventRegistration.cs", sb.ToString());
        }

        private class EventSyntaxReceiver : ISyntaxReceiver
        {
            public record EventItem(TypeDeclarationSyntax Type, AttributeSyntax Attribute, string Command);

            public List<EventItem> Events { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is TypeDeclarationSyntax tds)
                    foreach (AttributeSyntax x in tds.AttributeLists.SelectMany(v => v.Attributes))
                        if (x.Name.ToString() == "EventCommand" && x.ArgumentList?.Arguments[0].ToString() is { } command)
                        {
                            Events.Add(new EventItem(tds, x, command));
                            break;
                        }
            }
        }
    }
}
