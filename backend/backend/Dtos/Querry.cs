namespace backend.Dtos
{
    public class Querry
    {
        public string StringQ { get; set; } = "";
        public string Ordering { get; set; } = "";
        public bool IsDeleted { get; set; } = false;
        public string Status { get; set; } = "";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}
