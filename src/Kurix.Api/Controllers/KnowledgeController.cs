using Kurix.Api.Auth;
using Kurix.Api.Contracts;
using Kurix.Core.Enums;
using Kurix.Core.Entities;
using Kurix.Core.Knowledge;
using Kurix.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kurix.Api.Controllers;

/// <summary>Knowledge-base management for the dashboard (JWT). Scoped to the caller's tenant.</summary>
[ApiController]
[Route("api/knowledge/documents")]
[Authorize]
public class KnowledgeController(
    KurixDbContext db,
    IKnowledgeService knowledge,
    ILogger<KnowledgeController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<DocumentResponse>> Ingest(
        [FromBody] IngestDocumentRequest request, CancellationToken ct)
    {
        var tenantId = User.GetTenantId();

        var document = new KnowledgeDocument
        {
            TenantId = tenantId,
            FileName = request.FileName,
            Status = KnowledgeDocumentStatus.Processing
        };
        db.KnowledgeDocuments.Add(document);
        await db.SaveChangesAsync(ct);

        try
        {
            var result = await knowledge.IngestAsync(
                tenantId, new KnowledgeDocumentInput(request.FileName, request.Content, request.Metadata), ct);

            document.Status = KnowledgeDocumentStatus.Ingested;
            document.ChunkCount = result.ChunkCount;
            document.IngestedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ingestion failed for tenant {TenantId}, document {FileName}.",
                tenantId, request.FileName);
            document.Status = KnowledgeDocumentStatus.Failed;
            document.ErrorMessage = ex.Message;
            await db.SaveChangesAsync(ct);
            return StatusCode(StatusCodes.Status502BadGateway,
                new { error = "La ingesta del documento falló." });
        }

        return Ok(Map(document));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentResponse>>> List(CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var documents = await db.KnowledgeDocuments
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId)
            .OrderByDescending(d => d.IngestedAt)
            .ToListAsync(ct);

        return Ok(documents.Select(Map).ToList());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var document = await db.KnowledgeDocuments
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, ct);

        if (document is null)
            return NotFound();

        await knowledge.DeleteDocumentAsync(tenantId, document.FileName, ct);
        db.KnowledgeDocuments.Remove(document);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    private static DocumentResponse Map(KnowledgeDocument d) =>
        new(d.Id, d.FileName, d.Status.ToString(), d.ChunkCount, d.IngestedAt, d.ErrorMessage);
}
