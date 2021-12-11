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
    public class BlackListExcel {
        public string? Name { set; get; }
        public string? IdCard { set; get; }
        public string? Address { set; get; }
    }
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
        /// <summary>
        /// 通过idCard查重
        /// </summary>
        /// <param name="idCard"></param>
        /// <returns></returns>
        private async Task<BlackList> SearchRepeat(string idCard)
        {
            return await context.BlackList.Where(_ => _.IdCard == idCard).FirstOrDefaultAsync();
        }
        /// <summary>
        /// 获取黑名单列表 
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="limit">每页数量</param>
        /// <returns></returns>
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
        /// <summary>
        /// 通过关键字搜索
        /// </summary>
        /// <param name="keyword">关键字</param>
        /// <param name="page">页码</param>
        /// <param name="limit">每页数量</param>
        /// <returns></returns>
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
        /// <summary>
        /// 处理Excel文件
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
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
                    var data = map.Take<BlackListExcel>("sheet1").Select(_ => _.Value).ToList();

                    for (int i = 0; i < data.Count(); i++)
                    {
                        next:
                        var item = data[i];
                        // 判断excel重复
                        for (int j = 0; j < i; j++)
                        {
                            var _item = data[j];
                            if (_item.IdCard == item.IdCard)
                            {
                                i++;
                                goto next;
                            }
                        }
                        var repeat = await SearchRepeat(item.IdCard);
                        // 判断数据库重复, 有则更新 无则添加
                        if (repeat==null)
                        {
                            var black = new BlackList() {IdCard = item.IdCard, Name = item.Name, Address = item.Address };
                            context.BlackList.Add(black);
                        }
                        // 如果数据库中有重复 , 且数据有变化 , 则执行更新操作
                        else if (repeat.IdCard != item.IdCard || repeat.Name != item.Name || repeat.Address != item.Address)
                        {
                            repeat.IdCard = item.IdCard;
                            repeat.Name = item.Name;
                            repeat.Address = item.Address;
                            context.SaveChanges();
                        }
                    }
                    context.SaveChanges();
                }
                return new { code = 200, message = "导入成功" };
            }catch(Exception e)
            {
                return new { code = 500, message = e.Message };
            } 
        }
        /// <summary>
        /// 更新 / 新增 数据(取决于有没有id  或者说id是否为0)
        /// </summary>
        /// <param name="blackList"></param>
        /// <returns></returns>
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
                    
                    if (await SearchRepeat(idCard)!=null)
                    {
                        return new { code = 400, message = "证件号重复!" }; 
                    }
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
        /// <summary>
        /// 通过id删除对象
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 精确查找
        /// </summary>
        /// <param name="obj">对象中 cardId, name必填</param>
        /// <returns></returns>
        [HttpPost("ExactSearch"), EnableCors("getData")] 
        public async Task<object> ExactSearch(BlackList obj)
        { 
            try 
            {
                var data = context.BlackList.Where(_ => _.IdCard == obj.IdCard && _.Name == obj.Name).FirstOrDefault();
                return new { code = 200, data, message = "查询成功" };
            }
            catch (Exception e)
            {
                return new { code = 500, message = e.Message };
            }
             
        }

    }
}
