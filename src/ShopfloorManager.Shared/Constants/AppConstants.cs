namespace ShopfloorManager.Shared.Constants;

public static class AppConstants
{
    public static class Roles
    {
        public const string Admin        = "Administrator";
        public const string Manager      = "Manager";
        public const string LeadEngineer = "Lead Engineer";  // Kỹ sư trưởng — approve docs + dimensions
        public const string Leader       = "Leader";          // Tổ trưởng — quản lý ca, force-finish session
        public const string Engineer     = "Engineer";
        public const string QC           = "QC Inspector";
        public const string Operator     = "Operator";
        public const string Planner      = "Planner";

        // Roles có quyền Approve/Reject TechDocuments và Dimensions
        public const string Approvers = $"{Admin},{Manager},{LeadEngineer}";
    }

    public static class Mqtt
    {
        public const string TopicCnc     = "factory/cnc/#";
        public const string TopicMachine = "factory/cnc/{0}/status";  // {0} = machineCode
    }

    public static class Minio
    {
        public const string DefaultBucket = "shopfloor-storage";
    }

    public static class Cache
    {
        public const int DefaultExpiryMinutes = 5;
    }
}
