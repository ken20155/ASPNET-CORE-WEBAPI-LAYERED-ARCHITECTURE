using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApiInterviewStatus.Models;
using Microsoft.AspNetCore.Authorization;

namespace WebApiInterviewStatus.Controllers.V1.Modules.Users
{

    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class UsersListController(MainModel mainModel) : ControllerBase
    {
        private readonly MainModel _mainModel = mainModel;


        [HttpGet("list")]
        public async Task<IActionResult> FetchUsers()
        {
            var sql = "SELECT * FROM [tbl_users] WHERE is_deleted = @is_deleted ";

            var allRows = await _mainModel.GetAllAsync(
                sql,
                new { is_deleted = 0 },
                dbSet: DbSetType.Db2
            );

            return Ok(new
            {
                success = true,
                message = "Fetched data21 users",
                data = allRows
            });
        }
    }
}
