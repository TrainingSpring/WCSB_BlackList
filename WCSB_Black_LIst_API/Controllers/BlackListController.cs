using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;
using Microsoft.AspNetCore.Mvc;
using WCSB_Black_LIst_API.Models;
using Npoi.Mapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WCSB_Black_LIst_API.Controllers
{
    // [Route("api/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json", "multipart/form-data")]//此处为新增 接受form表单数据
    [Route("api/")]
    [ApiController]
    public class BlackListController : ControllerBase
    {
        private readonly BlackListContext context;
        public BlackListController(BlackListContext context)
        {
            this.context = context;
        }
        // GET: api/<BlackList>
        [HttpGet("GetBlackLists"), EnableCors("getData")]
        public async Task<object> GetBlackLists(int page = 1, int limit = 20)
        {
            try
            {
                var bl = context.BlackList;
                // 查数量
                var total = bl.Select(_ => _.Id).Distinct().Count();
                // 获取全部数据 
                // bl.ToListAsync();
                // 分页查询
                var res =  bl.Skip(limit * (page-1)).Take(limit);
                return new { code = 200,data = new { list = res , total} ,message="查询成功"};
            }
            catch (Exception e)
            {
                return new {code = 400,message=e.Message};
            }
        }

        // GET api/<BlackList>/5
        [HttpGet("Search")]
        public async Task<object> Search(string keyword,int page = 1, int limit = 20)
        {
            try
            {
                var s = context.BlackList.Where(_ => _.Name.Contains(keyword) || _.IdCard.Contains(keyword) || _.Address.Contains(keyword));
                var total = s.Select(_ => _.Id).Distinct().Count();
                var res = s.Skip(limit * (page-1)).Take(limit);
                return new {code = 200 , data =new {
                    list = res,
                    total
                }, message="查询成功"};
            }
            catch (Exception e)
            {
                return new {code = 400,message=e.Message};  
            }
        }
        private class BlackListExcel {
            public string? Name { set; get; }
            public string? IdCard { set; get; }
            public string? Address { set; get; }
        }
        // POST api/<BlackList>
        [HttpPost("UploadExcel")]
        public async Task<object> UploadExcel(IFormCollection files) 
        {
            try {
                foreach (var file in files.Files)
                {
                    string fileName = file.FileName;
                    string[] suffixArr = fileName.Split(".");
                    string suffix = suffixArr[suffixArr.Length-1];
                    if(suffix != "xlsx" && suffix != "xls" && suffix != "xlsm"&& suffix != "xltx"&& suffix != "et"&& suffix != "ett")
                    {
                        throw new Exception($"[Error]文件格式不正确: .{suffix}不是有效的Excel文件格式!");
                    }
                    var mapper = new Mapper(file.OpenReadStream());
                    var map = mapper.Map<BlackListExcel>("姓名", o => o.Name).Map<BlackListExcel>("证件号码", o => o.IdCard).Map<BlackListExcel>("户籍地", o => o.Address);
                    var data = map.Take<BlackListExcel>("sheet1").Select(_ => _.Value);

                    foreach (var item in data)
                    {
                        if(item.Address == null)
                        {
                            Console.WriteLine(item);
                        }
                        var black = new BlackList() {IdCard = item.IdCard, Name = item.Name, Address = item.Address };
                        context.BlackList.Add(black);
                    }
                    context.SaveChanges();
                }
                return new { code = 200, message = "导入成功" };
            }catch(Exception e)
            {
                return new { code = 500, message = e.Message };
            } 
        }
         
        [HttpPost("UpdatePeople")]
        public async Task<object> UpdatePeople(BlackList blackList )
         {
            try
            {
                int id = blackList.Id;
                string name = blackList.Name;
                string? address = blackList.Address;
                string idCard = blackList.IdCard;
                if (id == 0)
                {
                    var data = new BlackList() { IdCard = idCard, Name = name, Address = address };
                    context.BlackList.Add(data);
                }
                else
                {
                    var data = context.BlackList.Where(_ => _.Id == id).FirstOrDefault();
                    data.Name = name;
                    data.Address = address;
                    data.IdCard = idCard;
                }
                context.SaveChanges();
                return new { code = 200, message = (id == 0 ? "新增" : "修改") + "成功" };
            }
            catch (Exception e)
            {
                 return new { code = 500, message = e.Message };
            }
        }
        [HttpPost("DeletePeople")]
        public async Task<object> DeletePeople(object obj)
        {
            try
            {
                JObject res = (JObject)JsonConvert.DeserializeObject(obj.ToString());
                string s_id = res["id"].ToString();
                int id = int.Parse(s_id);
                var data = context.BlackList.Where(_ => _.Id == id).FirstOrDefault();
                context.BlackList.Remove(data);
                context.SaveChanges();
                return new { code = 200, message = "删除成功" };
            }
            catch (Exception e)
            {
                return new { code = 500, message = e.Message };
            }
        }
        
        [HttpGet("ExactSearch"), EnableCors("getData")] 
        public async Task<object> ExactSearch(string name , string idCard)
        { 
            try 
            {
                var data = context.BlackList.Where(_ => _.IdCard == idCard && _.Name == name).FirstOrDefault();
                return new { code = 200, data, message = "查询成功" };
            }
            catch (Exception e)
            {
                return new { code = 500, message = e.Message };
            }
             
        }

    }
}
