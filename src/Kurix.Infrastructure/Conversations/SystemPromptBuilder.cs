using System.Text;
using Kurix.Core.Knowledge;
using Kurix.Core.MultiTenancy;

namespace Kurix.Infrastructure.Conversations;

/// <summary>
/// Assembles the system prompt for a turn from three parts: the tenant's persona,
/// the retrieved RAG context, and fixed behaviour/escalation instructions.
/// </summary>
internal static class SystemPromptBuilder
{
    public static string Build(TenantSettings settings, IReadOnlyList<KnowledgeSearchResult> context)
    {
        var sb = new StringBuilder();

        sb.AppendLine(settings.Persona.Trim());
        sb.AppendLine();

        sb.AppendLine("# Contexto de la base de conocimiento");
        if (context.Count == 0)
        {
            sb.AppendLine("(No se encontró información relevante para esta consulta.)");
        }
        else
        {
            sb.AppendLine("Usá la siguiente información del negocio para responder. " +
                          "Si la respuesta no está acá ni en el historial, no la inventes.");
            sb.AppendLine();
            for (var i = 0; i < context.Count; i++)
            {
                sb.AppendLine($"[Fragmento {i + 1} — {context[i].SourceDocument}]");
                sb.AppendLine(context[i].Content.Trim());
                sb.AppendLine();
            }
        }

        sb.AppendLine("# Instrucciones de comportamiento");
        sb.AppendLine("- Respondé siempre en español, de forma clara y concisa.");
        sb.AppendLine("- Basá tus respuestas en el contexto y el historial; no inventes datos.");
        sb.AppendLine("- Usá las herramientas disponibles cuando la consulta requiera una acción " +
                      "(consultar agenda, reservar, buscar inventario).");
        sb.AppendLine("- Si no podés resolver la consulta con confianza, si el cliente lo pide, " +
                      "o si es un reclamo o caso sensible, usá la herramienta 'escalate_to_human'.");
        sb.AppendLine("- No prometas acciones que no podés ejecutar con las herramientas disponibles.");

        return sb.ToString().TrimEnd();
    }
}
