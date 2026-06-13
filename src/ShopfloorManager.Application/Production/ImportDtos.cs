namespace ShopfloorManager.Application.Production;

public record ImportRowError(int RowNumber, string Message);

public record ImportResultDto(int Created, int Updated, int Skipped, List<ImportRowError> Errors);
