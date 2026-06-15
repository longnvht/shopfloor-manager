using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.Production;

// ── Bulk Import: Job + Part + Routing + OP (theo 03_job_management.md §3-7) ──

public record ImportJobBatchRow(
    string PartNumber, string? PartDescription, string? Revision,
    string JobNumber, string? PoNumber, string? PoLineNumber,
    int? RunQty, DateOnly? ShipBy,
    string OpNumber, string? OpTypeCode, string? OpDescription,
    decimal? SetupTime, decimal? ProdTime);

public record GlobalImportResultDto(
    int PartsCreated, int PartRevsCreated,
    int OpsCreated, int OpsUpdated,
    int JobsCreated, int JobsUpdated, int ProductsCreated,
    List<ImportRowError> Errors);

public record ImportJobBatchCommand(List<ImportJobBatchRow> Rows, int? RequesterId)
    : IRequest<Result<GlobalImportResultDto>>;

public class ImportJobBatchCommandValidator : AbstractValidator<ImportJobBatchCommand>
{
    public ImportJobBatchCommandValidator()
    {
        RuleFor(x => x.Rows).NotEmpty();
    }
}

public class ImportJobBatchCommandHandler(IShopfloorDbContext db)
    : IRequestHandler<ImportJobBatchCommand, Result<GlobalImportResultDto>>
{
    public async Task<Result<GlobalImportResultDto>> Handle(ImportJobBatchCommand req, CancellationToken ct)
    {
        var opTypes = await db.OpTypes.ToListAsync(ct);
        var errors = new List<ImportRowError>();
        int partsCreated = 0, partRevsCreated = 0, opsCreated = 0, opsUpdated = 0,
            jobsCreated = 0, jobsUpdated = 0, productsCreated = 0;

        var groups = req.Rows
            .Select((row, idx) => (Row: row, RowNumber: idx + 2)) // dòng 1 là header
            .GroupBy(x => x.Row.JobNumber)
            .ToList();

        foreach (var group in groups)
        {
            var rows = group.ToList();

            if (string.IsNullOrWhiteSpace(group.Key))
            {
                foreach (var (_, rowNumber) in rows)
                    errors.Add(new ImportRowError(rowNumber, "Thiếu JobNumber."));
                continue;
            }

            var first = rows[0].Row;
            if (string.IsNullOrWhiteSpace(first.PartNumber))
            {
                foreach (var (_, rowNumber) in rows)
                    errors.Add(new ImportRowError(rowNumber, $"Job '{group.Key}': thiếu PartNumber."));
                continue;
            }

            try
            {
                // a. Resolve/tạo Part
                var part = await db.Parts.FirstOrDefaultAsync(p => p.PartNumber == first.PartNumber, ct);
                if (part is null)
                {
                    part = new Part { PartNumber = first.PartNumber, Description = first.PartDescription ?? "", CreatedBy = req.RequesterId };
                    db.Parts.Add(part);
                    partsCreated++;
                }

                // b. Resolve/tạo PartRev + c. RoutingRev active
                var revCode = string.IsNullOrWhiteSpace(first.Revision) ? "A" : first.Revision;
                var partRev = part.Id != 0
                    ? await db.PartRevs.FirstOrDefaultAsync(r => r.PartId == part.Id && r.RevCode == revCode, ct)
                    : null;

                RoutingRev routingRev;
                if (partRev is null)
                {
                    // PartRev mới → IsActive=true + deactivate các PartRev khác cùng Part
                    if (part.Id != 0)
                    {
                        var activeRevs = await db.PartRevs.Where(r => r.PartId == part.Id && r.IsActive).ToListAsync(ct);
                        foreach (var r in activeRevs) r.IsActive = false;
                    }

                    partRev = new PartRev { Part = part, RevCode = revCode, IsActive = true, CreatedBy = req.RequesterId };
                    db.PartRevs.Add(partRev);
                    partRevsCreated++;

                    var routing = new Routing { PartRev = partRev, Name = "Standard", CreatedBy = req.RequesterId };
                    db.Routings.Add(routing);

                    routingRev = new RoutingRev { Routing = routing, RevCode = "R1", IsActive = true, CreatedBy = req.RequesterId };
                    db.RoutingRevs.Add(routingRev);
                }
                else
                {
                    var existingRoutingRev = await db.RoutingRevs
                        .FirstOrDefaultAsync(rr => rr.IsActive && rr.Routing.IsActive && rr.Routing.PartRevId == partRev.Id, ct);

                    if (existingRoutingRev is null)
                    {
                        var routing = new Routing { PartRev = partRev, Name = "Standard", CreatedBy = req.RequesterId };
                        db.Routings.Add(routing);
                        routingRev = new RoutingRev { Routing = routing, RevCode = "R1", IsActive = true, CreatedBy = req.RequesterId };
                        db.RoutingRevs.Add(routingRev);
                    }
                    else
                    {
                        routingRev = existingRoutingRev;
                    }
                }

                // d. Upsert PartOps theo OpNumber — dedupe trong cùng nhóm (dòng cuối thắng)
                var opRowsByNumber = new Dictionary<string, (ImportJobBatchRow Row, int RowNumber)>();
                foreach (var (row, rowNumber) in rows)
                {
                    if (string.IsNullOrWhiteSpace(row.OpNumber))
                    {
                        errors.Add(new ImportRowError(rowNumber, $"Job '{group.Key}': thiếu OpNumber."));
                        continue;
                    }
                    if (opRowsByNumber.ContainsKey(row.OpNumber))
                        errors.Add(new ImportRowError(rowNumber, $"Job '{group.Key}': OpNumber '{row.OpNumber}' trùng — dùng dòng cuối."));
                    opRowsByNumber[row.OpNumber] = (row, rowNumber);
                }

                var existingOps = routingRev.Id != 0
                    ? await db.PartOps.Where(o => o.RoutingRevId == routingRev.Id).ToDictionaryAsync(o => o.OpNumber, ct)
                    : new Dictionary<string, PartOp>();

                foreach (var (opNumber, (row, rowNumber)) in opRowsByNumber)
                {
                    int? opTypeId = null;
                    if (!string.IsNullOrWhiteSpace(row.OpTypeCode))
                    {
                        var opType = opTypes.FirstOrDefault(t => string.Equals(t.Code, row.OpTypeCode, StringComparison.OrdinalIgnoreCase));
                        if (opType is null)
                            errors.Add(new ImportRowError(rowNumber, $"Job '{group.Key}': không tìm thấy OpType '{row.OpTypeCode}'."));
                        else
                            opTypeId = opType.Id;
                    }

                    if (existingOps.TryGetValue(opNumber, out var existing))
                    {
                        existing.Description = row.OpDescription;
                        existing.OpTypeId = opTypeId;
                        existing.SetupTime = row.SetupTime;
                        existing.ProdTime = row.ProdTime;
                        opsUpdated++;
                    }
                    else
                    {
                        decimal.TryParse(opNumber, out var sort);
                        var newOp = new PartOp
                        {
                            RoutingRev = routingRev,
                            OpNumber = opNumber, OpNumberSort = sort,
                            OpTypeId = opTypeId,
                            Description = row.OpDescription,
                            SetupTime = row.SetupTime, ProdTime = row.ProdTime,
                            CreatedBy = req.RequesterId
                        };
                        db.PartOps.Add(newOp);
                        existingOps[opNumber] = newOp;
                        opsCreated++;
                    }
                }

                // e. Resolve/tạo PoLine
                PoLine? poLine = null;
                if (!string.IsNullOrWhiteSpace(first.PoNumber))
                {
                    poLine = await db.PoLines.FirstOrDefaultAsync(
                        p => p.PoNumber == first.PoNumber && p.PoLineNumber == first.PoLineNumber, ct);
                    if (poLine is null)
                    {
                        poLine = new PoLine { PoNumber = first.PoNumber, PoLineNumber = first.PoLineNumber };
                        db.PoLines.Add(poLine);
                    }
                }

                // f. Job upsert
                var job = await db.Jobs.FirstOrDefaultAsync(j => j.JobNumber == group.Key, ct);
                if (job is null)
                {
                    job = new Job
                    {
                        JobNumber = group.Key,
                        PartRev = partRev,
                        RoutingRev = routingRev,
                        PoLine = poLine,
                        RunQty = first.RunQty, ShipBy = first.ShipBy,
                        CreatedBy = req.RequesterId
                    };
                    db.Jobs.Add(job);
                    jobsCreated++;

                    if (first.RunQty.HasValue && first.RunQty.Value > 0)
                    {
                        var products = Enumerable.Range(1, first.RunQty.Value).Select(i => new Product
                        {
                            Job = job, SerialNumber = i.ToString("D2"), SortOrder = i
                        });
                        db.Products.AddRange(products);
                        productsCreated += first.RunQty.Value;
                    }
                }
                else
                {
                    var changed = false;

                    if (first.RunQty.HasValue && job.RunQty != first.RunQty.Value)
                    {
                        var oldQty = job.RunQty ?? 0;
                        var newQty = first.RunQty.Value;
                        job.RunQty = newQty;
                        changed = true;

                        if (newQty > oldQty)
                        {
                            var existingCount = await db.Products.CountAsync(p => p.JobId == job.Id, ct);
                            var toAdd = newQty - existingCount;
                            if (toAdd > 0)
                            {
                                var products = Enumerable.Range(existingCount + 1, toAdd).Select(i => new Product
                                {
                                    Job = job, SerialNumber = i.ToString("D2"), SortOrder = i
                                });
                                db.Products.AddRange(products);
                                productsCreated += toAdd;
                            }
                        }
                        else if (newQty < oldQty)
                        {
                            errors.Add(new ImportRowError(rows[0].RowNumber,
                                $"Job '{group.Key}': RunQty giảm từ {oldQty} xuống {newQty} — không xoá Product hiện có."));
                        }
                    }

                    if (first.ShipBy.HasValue && job.ShipBy != first.ShipBy)
                    {
                        job.ShipBy = first.ShipBy;
                        changed = true;
                    }

                    if (changed) jobsUpdated++;
                }

                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                db.ChangeTracker.Clear();
                foreach (var (_, rowNumber) in rows)
                    errors.Add(new ImportRowError(rowNumber, $"Job '{group.Key}': lỗi xử lý — {ex.Message}"));
            }
        }

        return Result.Ok(new GlobalImportResultDto(
            partsCreated, partRevsCreated, opsCreated, opsUpdated,
            jobsCreated, jobsUpdated, productsCreated, errors));
    }
}
