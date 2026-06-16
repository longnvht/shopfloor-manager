using ShopfloorManager.Application.Common.Interfaces;

namespace ShopfloorManager.Infrastructure.Erp;

public class ErpConnectorFactory : IErpConnectorFactory
{
    public IErpConnector Create(string erpType, string baseUrl, string? company, string? username, string? password) =>
        erpType.ToUpperInvariant() switch
        {
            "EPICOR" => new EpicorConnector(baseUrl, company, username, password),
            "MOCK"   => new MockErpConnector(),
            _        => throw new NotSupportedException($"ErpType không được hỗ trợ: {erpType}")
        };
}
