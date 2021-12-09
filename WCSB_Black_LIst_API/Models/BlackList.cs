using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WCSB_Black_LIst_API.Models
{
    public class BlackList
    {
        [Key] public int Id { set; get; }
        [DisplayName("姓名")] public string Name { set; get ; }
        [DisplayName("身份证号")] public string IdCard { set; get ; }
        [DisplayName("户籍地")] public string? Address { set; get ; }
    }
}
