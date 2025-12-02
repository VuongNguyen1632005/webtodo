namespace WebApplication1.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    
    [Table("ThanhVienBang")]
    public partial class ThanhVienBang
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int MaBang { get; set; }
        
        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int MaTaiKhoan { get; set; }
        
        [StringLength(20)]
        public string VaiTro { get; set; } // "owner", "editor", "viewer"
        
        public DateTime? NgayThamGia { get; set; }
        
        public virtual Bang Bang { get; set; }
        public virtual TaiKhoan TaiKhoan { get; set; }
    }
}
