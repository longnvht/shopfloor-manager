namespace ShopfloorManager.Domain.Entities;

/// <summary>
/// Cấu hình kết nối ERP (Epicor, Odoo, ERPNext...).
/// Credentials lưu thẳng vào DB — hệ thống chạy trên intranet nội bộ, không public internet.
/// </summary>
public class ErpConnection : BaseEntity
{
    public string Name { get; set; } = "";
    /// <summary>"Epicor" | "Mock" | "Odoo" | "ErpNext"</summary>
    public string ErpType { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    /// <summary>Epicor: company code; Odoo: database name</summary>
    public string? Company { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool IsActive { get; set; } = true;
}
