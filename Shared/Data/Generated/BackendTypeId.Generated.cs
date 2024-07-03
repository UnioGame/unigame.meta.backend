namespace MetaService.Shared.Data
{
    public partial struct BackendTypeId
    {
        public static BackendTypeId Nakama => new BackendTypeId { value = 0 };
        public static BackendTypeId Mock => new BackendTypeId { value = 1 };
    }
}
