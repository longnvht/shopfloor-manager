namespace ShopfloorManager.Domain.Enums;

public enum FileStatus { Pending = 0, Approved = 1, Rejected = 2 }
public enum NcrAction  { Pending = 0, Approve = 1, Rework = 2, Reject = 3 }
public enum NcrStatus  { Open = 0, Closed = 1 }
public enum MeasureResult { Pass = 1, Fail = 2 }
public enum BorrowStatus  { Active = 0, Returned = 1, Cancelled = 2 }
public enum CalibRequestStatus { Pending = 0, Approved = 1, Completed = 2, Cancelled = 3 }
