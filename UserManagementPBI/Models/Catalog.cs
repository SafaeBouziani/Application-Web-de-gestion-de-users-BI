namespace UserManagementPBI.Models
{
    public class Catalog
    {
        public Guid ItemID { get; set; }
        public string Path { get; set; } = null!;
        public string Name { get; set; } = null!;
        public Guid? ParentID { get; set; }
        public int Type { get; set; }
        public byte[]? Content { get; set; }
        public Guid? Intermediate { get; set; }
        public Guid? SnapshotDataID { get; set; }
        public Guid? LinkSourceID { get; set; }
        public string? Property { get; set; }
        public string? Description { get; set; }
        public bool? Hidden { get; set; }
        public Guid CreatedByID { get; set; }
        public DateTime CreationDate { get; set; }
        public Guid ModifiedByID { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string? MimeType { get; set; }
        public int? SnapshotLimit { get; set; }
        public string? Parameter { get; set; }
        public Guid PolicyID { get; set; }
        public bool PolicyRoot { get; set; }
        public int ExecutionFlag { get; set; }
        public DateTime? ExecutionTime { get; set; }
        public string? SubType { get; set; }
        public Guid? ComponentID { get; set; }
        public long? ContentSize { get; set; }
    }

}
