using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ShopfloorManager.API.Common;
using ShopfloorManager.Infrastructure.Data;
using ShopfloorManager.Infrastructure.Mqtt;

namespace ShopfloorManager.API.Controllers;

[ApiController]
[Route("api/v1/machines")]
[Authorize]
public class MachineStatusController(ShopfloorDbContext db, IMemoryCache cache) : ControllerBase
{
    /// <summary>Trạng thái real-time tất cả máy (từ MemoryCache, cập nhật qua MQTT)</summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetAllStatus()
    {
        var machines = await db.Machines
            .Where(m => m.IsActive && m.IsCnc)
            .OrderBy(m => m.Code)
            .Select(m => new { m.Id, m.Code, m.Name, m.MachineType })
            .ToListAsync();

        var result = machines.Select(m =>
        {
            var status = cache.Get<CncPayload>($"machine_status_{m.Code}");
            return new MachineStatusDto(
                m.Id, m.Code, m.Name,
                status?.RunMode, status?.TmMode, status?.AlarmMessage,
                status?.SpindleSpeed, status?.SpindleLoad, status?.Feedrate,
                status?.XPosition, status?.YPosition, status?.ZPosition,
                status?.Program, status?.PartCount,
                status?.Timestamp ?? DateTimeOffset.MinValue);
        });

        return Ok(ApiResponse<object>.Ok(result));
    }

    /// <summary>Trạng thái real-time 1 máy</summary>
    [HttpGet("{machineCode}/status-live")]
    public async Task<IActionResult> GetStatus(string machineCode)
    {
        var machine = await db.Machines
            .Where(m => m.Code == machineCode)
            .Select(m => new { m.Id, m.Code, m.Name })
            .FirstOrDefaultAsync();

        if (machine is null) return NotFound(ApiResponse<object>.Fail("Máy không tồn tại."));

        var status = cache.Get<CncPayload>($"machine_status_{machineCode}");
        var dto = new MachineStatusDto(
            machine.Id, machine.Code, machine.Name,
            status?.RunMode, status?.TmMode, status?.AlarmMessage,
            status?.SpindleSpeed, status?.SpindleLoad, status?.Feedrate,
            status?.XPosition, status?.YPosition, status?.ZPosition,
            status?.Program, status?.PartCount,
            status?.Timestamp ?? DateTimeOffset.MinValue);

        return Ok(ApiResponse<MachineStatusDto>.Ok(dto));
    }

    /// <summary>Lịch sử events của 1 máy theo ngày</summary>
    [HttpGet("{machineCode}/events")]
    public async Task<IActionResult> GetEvents(string machineCode, [FromQuery] DateOnly? date = null)
    {
        var d = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var startUtc = d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc   = startUtc.AddDays(1);

        var machine = await db.Machines.FirstOrDefaultAsync(m => m.Code == machineCode);
        if (machine is null) return NotFound(ApiResponse<object>.Fail("Máy không tồn tại."));

        var events = await db.MachineEvents
            .Where(e => e.MachineId == machine.Id
                     && e.CreatedAt >= startUtc
                     && e.CreatedAt <  endUtc)
            .OrderBy(e => e.CreatedAt)
            .Select(e => new {
                e.Id, e.CreatedAt,
                e.TmMode, e.AtMode, e.RunMode,
                e.Alarm, e.AlarmMessage
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(events));
    }
}
