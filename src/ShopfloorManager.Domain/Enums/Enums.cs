namespace ShopfloorManager.Domain.Enums;

public enum FileStatus { Pending = 0, Approved = 1, Rejected = 2 }
public enum NcrAction  { Pending = 0, Approve = 1, Rework = 2, Reject = 3 }
public enum NcrStatus  { Open = 0, Closed = 1 }
public enum MeasureResult { Pass = 1, Fail = 2 }
public enum BorrowStatus  { Active = 0, Returned = 1, Cancelled = 2 }
public enum CalibRequestStatus { Pending = 0, Approved = 1, Completed = 2, Cancelled = 3 }

/// <summary>
/// Giai đoạn đo kiểm — mỗi giai đoạn độc lập, không overwrite nhau.
/// (DimensionId, ProductId, MeasureStage) unique per stage.
/// </summary>
public enum MeasureStage
{
    InprocessFAI = 0,  // Operator đo sau khi kết thúc gia công tại OP
    QCInline     = 1,  // QC Inspector kiểm tra ngẫu nhiên trên chuyền
    QCFinal      = 2,  // QC Inspector kiểm tra toàn bộ trước khi xuất xưởng
}
